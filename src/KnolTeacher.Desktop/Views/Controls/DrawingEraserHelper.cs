using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KnolTeacher.Desktop.Views.Controls;

/// <summary>
/// 판서 도구 및 지우개 모드 정의.
/// </summary>
public enum EraserToolMode
{
    Pen,            // 일반 펜
    Highlighter,    // 형광펜
    Point,          // 부분 지우개 (궤적 지우기)
    Stroke,         // 획 지우개 (선 단위 지우기)
    Box,            // 구역 지우개 (직사각형 박스 드래그 일괄 삭제)
    Lasso           // 영역 지우개 (올가미 자유선 드래그 일괄 삭제)
}

/// <summary>
/// 구역 지우개(사각 드래그), 영역 지우개(올가미 드래그), 부분/획 지우개 상호작용 및
/// 실시간 프리뷰, HitTest 기반 획 삭제, Undo 연동을 총괄 관리하는 헬퍼 클래스.
/// </summary>
public class DrawingEraserHelper
{
    private readonly InkCanvas _inkCanvas;
    private readonly Canvas _previewCanvas;
    private readonly DrawingUndoManager _undoManager;

    private EraserToolMode _currentMode = EraserToolMode.Pen;
    private bool _isInteracting;
    private bool _isExecutingUndo;
    private Point _startPoint;
    private readonly List<Point> _lassoPoints = new();

    // 프리뷰 시각 요소
    private Rectangle? _boxPreview;
    private Path? _lassoPreview;
    private PathGeometry? _lassoGeometry;
    private PathFigure? _lassoFigure;

    public EraserToolMode CurrentMode => _currentMode;

    public event Action<EraserToolMode>? ModeChanged;
    public event Action? StrokesModified;

    public DrawingEraserHelper(InkCanvas inkCanvas, Canvas previewCanvas, DrawingUndoManager undoManager)
    {
        _inkCanvas = inkCanvas ?? throw new ArgumentNullException(nameof(inkCanvas));
        _previewCanvas = previewCanvas ?? throw new ArgumentNullException(nameof(previewCanvas));
        _undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));

        // 잉크 수집 이벤트 등록 (펜/형광펜 Undo 기록)
        _inkCanvas.StrokeCollected += OnStrokeCollected;

        // 일반 획 지우개 / 부분 지우개에 의한 삭제 감지 및 Undo 기록
        _inkCanvas.Strokes.StrokesChanged += OnStrokesChanged;

        // 마우스 및 터치/스타일러스 입력 이벤트 등록
        _inkCanvas.PreviewMouseDown += OnPreviewMouseDown;
        _inkCanvas.PreviewMouseMove += OnPreviewMouseMove;
        _inkCanvas.PreviewMouseUp += OnPreviewMouseUp;

        _inkCanvas.PreviewStylusDown += OnPreviewStylusDown;
        _inkCanvas.PreviewStylusMove += OnPreviewStylusMove;
        _inkCanvas.PreviewStylusUp += OnPreviewStylusUp;
    }

    /// <summary>
    /// 지우개 및 펜 도구 모드를 설정합니다.
    /// </summary>
    public void SetToolMode(EraserToolMode mode, double penWidth = 4, bool isHighlighter = false, Color? penColor = null)
    {
        _currentMode = mode;
        CancelInteraction();

        switch (mode)
        {
            case EraserToolMode.Pen:
                _inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                _inkCanvas.Cursor = Cursors.Pen;
                _inkCanvas.DefaultDrawingAttributes.IsHighlighter = false;
                _inkCanvas.DefaultDrawingAttributes.Width = penWidth;
                _inkCanvas.DefaultDrawingAttributes.Height = penWidth;
                if (penColor.HasValue) _inkCanvas.DefaultDrawingAttributes.Color = penColor.Value;
                break;

            case EraserToolMode.Highlighter:
                _inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                _inkCanvas.Cursor = Cursors.Pen;
                _inkCanvas.DefaultDrawingAttributes.IsHighlighter = true;
                _inkCanvas.DefaultDrawingAttributes.Width = 18;
                _inkCanvas.DefaultDrawingAttributes.Height = 28;
                if (penColor.HasValue) _inkCanvas.DefaultDrawingAttributes.Color = penColor.Value;
                break;

            case EraserToolMode.Point:
                _inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                _inkCanvas.Cursor = Cursors.Cross;
                break;

            case EraserToolMode.Stroke:
                _inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                _inkCanvas.Cursor = Cursors.Cross;
                break;

            case EraserToolMode.Box:
            case EraserToolMode.Lasso:
                // 구역 / 영역 지우개는 자체 인터랙션으로 처리하므로 InkCanvas 기본 드로잉 비활성화
                _inkCanvas.EditingMode = InkCanvasEditingMode.None;
                _inkCanvas.Cursor = Cursors.Cross;
                break;
        }

        ModeChanged?.Invoke(mode);
    }

    private void OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        if (_isExecutingUndo) return;
        _undoManager.PushAdded(e.Stroke);
        StrokesModified?.Invoke();
    }

    private void OnStrokesChanged(object? sender, StrokeCollectionChangedEventArgs e)
    {
        if (_isExecutingUndo) return;

        // 구역/영역 지우개 모드에서 직접 삭제한 것은 Up 시점에 PushRemoved하므로 여기서 중복 방지
        if (_currentMode != EraserToolMode.Box && _currentMode != EraserToolMode.Lasso)
        {
            if (e.Removed.Count > 0)
            {
                _undoManager.PushRemoved(e.Removed);
                StrokesModified?.Invoke();
            }
        }
    }

    #region Stylus Event Routing

    private void OnPreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        if (_currentMode == EraserToolMode.Box || _currentMode == EraserToolMode.Lasso)
        {
            var pt = e.GetPosition(_inkCanvas);
            StartDrag(pt);
            e.Handled = true;
        }
    }

    private void OnPreviewStylusMove(object sender, StylusEventArgs e)
    {
        if (_isInteracting && (_currentMode == EraserToolMode.Box || _currentMode == EraserToolMode.Lasso))
        {
            var pt = e.GetPosition(_inkCanvas);
            UpdateDrag(pt);
            e.Handled = true;
        }
    }

    private void OnPreviewStylusUp(object sender, StylusEventArgs e)
    {
        if (_isInteracting && (_currentMode == EraserToolMode.Box || _currentMode == EraserToolMode.Lasso))
        {
            EndDrag();
            e.Handled = true;
        }
    }

    #endregion

    #region Mouse Event Routing

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed &&
            (_currentMode == EraserToolMode.Box || _currentMode == EraserToolMode.Lasso))
        {
            var pt = e.GetPosition(_inkCanvas);
            StartDrag(pt);
            e.Handled = true;
        }
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isInteracting && (_currentMode == EraserToolMode.Box || _currentMode == EraserToolMode.Lasso))
        {
            var pt = e.GetPosition(_inkCanvas);
            UpdateDrag(pt);
            e.Handled = true;
        }
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isInteracting && (_currentMode == EraserToolMode.Box || _currentMode == EraserToolMode.Lasso))
        {
            EndDrag();
            e.Handled = true;
        }
    }

    #endregion

    #region Drag Interaction & HitTest Erasure

    private void StartDrag(Point pt)
    {
        _isInteracting = true;
        _startPoint = pt;
        _inkCanvas.CaptureMouse();

        if (_currentMode == EraserToolMode.Box)
        {
            CreateBoxPreview(pt);
        }
        else if (_currentMode == EraserToolMode.Lasso)
        {
            _lassoPoints.Clear();
            _lassoPoints.Add(pt);
            CreateLassoPreview(pt);
        }
    }

    private void UpdateDrag(Point pt)
    {
        if (!_isInteracting) return;

        if (_currentMode == EraserToolMode.Box)
        {
            UpdateBoxPreview(pt);
        }
        else if (_currentMode == EraserToolMode.Lasso)
        {
            // 지나치게 조밀한 점 수집 방지 (2px 이상 이동 시 추가)
            if (_lassoPoints.Count == 0 || (_lassoPoints[^1] - pt).Length >= 2.0)
            {
                _lassoPoints.Add(pt);
                UpdateLassoPreview();
            }
        }
    }

    private void EndDrag()
    {
        if (!_isInteracting) return;
        _isInteracting = false;
        if (_inkCanvas.IsMouseCaptured)
        {
            _inkCanvas.ReleaseMouseCapture();
        }

        try
        {
            if (_currentMode == EraserToolMode.Box)
            {
                ApplyBoxErasure();
            }
            else if (_currentMode == EraserToolMode.Lasso)
            {
                ApplyLassoErasure();
            }
        }
        finally
        {
            ClearPreview();
        }
    }

    public void CancelInteraction()
    {
        if (_isInteracting)
        {
            _isInteracting = false;
            if (_inkCanvas.IsMouseCaptured)
            {
                _inkCanvas.ReleaseMouseCapture();
            }
        }
        ClearPreview();
    }

    private void CreateBoxPreview(Point start)
    {
        ClearPreview();

        _boxPreview = new Rectangle
        {
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
            RadiusX = 2,
            RadiusY = 2,
            Width = 0,
            Height = 0
        };

        Canvas.SetLeft(_boxPreview, start.X);
        Canvas.SetTop(_boxPreview, start.Y);
        _previewCanvas.Children.Add(_boxPreview);
    }

    private void UpdateBoxPreview(Point current)
    {
        if (_boxPreview == null) return;

        double x = Math.Min(_startPoint.X, current.X);
        double y = Math.Min(_startPoint.Y, current.Y);
        double w = Math.Abs(current.X - _startPoint.X);
        double h = Math.Abs(current.Y - _startPoint.Y);

        Canvas.SetLeft(_boxPreview, x);
        Canvas.SetTop(_boxPreview, y);
        _boxPreview.Width = w;
        _boxPreview.Height = h;
    }

    private void ApplyBoxErasure()
    {
        if (_boxPreview == null) return;

        double x = Canvas.GetLeft(_boxPreview);
        double y = Canvas.GetTop(_boxPreview);
        double w = _boxPreview.Width;
        double h = _boxPreview.Height;

        // 아주 작은 클릭은 실수 방지 (가로 세로 4px 이상일 때만 삭제)
        if (w >= 4 && h >= 4)
        {
            var rect = new Rect(x, y, w, h);
            // 1% 이상 교차/포함된 모든 스트로크를 검출
            var hitStrokes = _inkCanvas.Strokes.HitTest(rect, 1);
            if (hitStrokes != null && hitStrokes.Count > 0)
            {
                _undoManager.PushRemoved(hitStrokes);
                _inkCanvas.Strokes.Remove(hitStrokes);
                StrokesModified?.Invoke();
            }
        }
    }

    private void CreateLassoPreview(Point start)
    {
        ClearPreview();

        _lassoFigure = new PathFigure
        {
            StartPoint = start,
            IsClosed = true,
            IsFilled = true
        };

        _lassoGeometry = new PathGeometry();
        _lassoGeometry.Figures.Add(_lassoFigure);

        _lassoPreview = new Path
        {
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),
            Data = _lassoGeometry
        };

        _previewCanvas.Children.Add(_lassoPreview);
    }

    private void UpdateLassoPreview()
    {
        if (_lassoFigure == null || _lassoPoints.Count < 2) return;

        _lassoFigure.Segments.Clear();
        var polySegment = new PolyLineSegment(_lassoPoints.Skip(1), true);
        _lassoFigure.Segments.Add(polySegment);
    }

    private void ApplyLassoErasure()
    {
        if (_lassoPoints.Count >= 3)
        {
            // 올가미 폐곡선 내부 또는 교차하는 스트로크 검출
            var hitStrokes = _inkCanvas.Strokes.HitTest(_lassoPoints.ToArray(), 1);
            if (hitStrokes != null && hitStrokes.Count > 0)
            {
                _undoManager.PushRemoved(hitStrokes);
                _inkCanvas.Strokes.Remove(hitStrokes);
                StrokesModified?.Invoke();
            }
        }
    }

    private void ClearPreview()
    {
        if (_boxPreview != null)
        {
            _previewCanvas.Children.Remove(_boxPreview);
            _boxPreview = null;
        }

        if (_lassoPreview != null)
        {
            _previewCanvas.Children.Remove(_lassoPreview);
            _lassoPreview = null;
            _lassoGeometry = null;
            _lassoFigure = null;
        }

        _lassoPoints.Clear();
    }

    #endregion

    /// <summary>
    /// 마지막 판서 액션을 되돌립니다.
    /// </summary>
    public bool Undo()
    {
        _isExecutingUndo = true;
        try
        {
            bool success = _undoManager.Undo(_inkCanvas);
            if (success)
            {
                StrokesModified?.Invoke();
            }
            return success;
        }
        finally
        {
            _isExecutingUndo = false;
        }
    }

    /// <summary>
    /// 모든 판서를 지우고 Undo 스택을 비웁니다.
    /// </summary>
    public void ClearAll()
    {
        CancelInteraction();
        _inkCanvas.Strokes.Clear();
        _undoManager.Clear();
        StrokesModified?.Invoke();
    }
}
