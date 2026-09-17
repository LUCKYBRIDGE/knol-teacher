using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace KnolTeacher.Desktop.Views.Controls;

/// <summary>
/// 전자칠판 멀티터치 동시 필기, 디지타이저 스타일러스 펜, 교탁 PC 마우스 판서를
/// 완벽하게 지원하는 고성능 잉크 헬퍼 클래스.
/// </summary>
public class MultiTouchInkHelper
{
    private readonly InkCanvas _inkCanvas;
    private readonly Dictionary<int, Stroke> _activeTouchStrokes = new();
    private readonly Dictionary<int, Stroke> _activeStylusStrokes = new();
    private readonly Dictionary<int, Point> _lastTouchEraserPoints = new();
    private readonly Dictionary<int, Point> _lastStylusEraserPoints = new();
    private Point? _lastMouseEraserPoint;
    private Stroke? _mouseStroke;
    private bool _isEraserMode;

    public event Action<Stroke>? StrokeCollected;
    public event Action<StrokeCollection>? StrokesErased;

    public bool IsEraserMode
    {
        get => _isEraserMode;
        set
        {
            _isEraserMode = value;
            _lastMouseEraserPoint = null;
            _lastTouchEraserPoints.Clear();
            _lastStylusEraserPoints.Clear();
        }
    }

    public MultiTouchInkHelper(InkCanvas inkCanvas)
    {
        _inkCanvas = inkCanvas ?? throw new ArgumentNullException(nameof(inkCanvas));

        // 기본 단일 포인터 처리를 끄고 멀티터치/펜/마우스를 통합 관리
        _inkCanvas.EditingMode = InkCanvasEditingMode.None;

        // 1. 멀티터치 (전자칠판 손가락 다중 터치)
        _inkCanvas.TouchDown += OnTouchDown;
        _inkCanvas.TouchMove += OnTouchMove;
        _inkCanvas.TouchUp += OnTouchUp;
        _inkCanvas.TouchLeave += OnTouchLeave;

        // 2. 디지타이저 스타일러스 펜 (전자칠판 전용 펜, 서피스/와콤 펜)
        _inkCanvas.StylusDown += OnStylusDown;
        _inkCanvas.StylusMove += OnStylusMove;
        _inkCanvas.StylusUp += OnStylusUp;
        _inkCanvas.StylusLeave += OnStylusLeave;

        // 3. 마우스 판서 (교탁 모니터/노트북 마우스 좌클릭 드래그)
        _inkCanvas.MouseDown += OnMouseDown;
        _inkCanvas.MouseMove += OnMouseMove;
        _inkCanvas.MouseUp += OnMouseUp;
        _inkCanvas.MouseLeave += OnMouseLeave;
    }

    #region Touch Handlers (손가락 멀티터치 독립 트래킹)

    private void OnTouchDown(object? sender, TouchEventArgs e)
    {
        var touch = e.TouchDevice;
        var point = touch.GetTouchPoint(_inkCanvas);
        var pos = point.Position;

        if (_isEraserMode)
        {
            _lastTouchEraserPoints[touch.Id] = pos;
            EraseAtPoint(pos);
        }
        else
        {
            var attributes = _inkCanvas.DefaultDrawingAttributes.Clone();
            var stylusPoint = new StylusPoint(pos.X, pos.Y);
            var stroke = new Stroke(new StylusPointCollection(new[] { stylusPoint }), attributes);

            _activeTouchStrokes[touch.Id] = stroke;
            _inkCanvas.Strokes.Add(stroke);
        }

        e.Handled = true;
    }

    private void OnTouchMove(object? sender, TouchEventArgs e)
    {
        var touch = e.TouchDevice;
        var intermediatePoints = e.GetIntermediateTouchPoints(_inkCanvas);

        if (_isEraserMode)
        {
            _lastTouchEraserPoints.TryGetValue(touch.Id, out var lastPt);
            foreach (var pt in intermediatePoints)
            {
                var curPos = pt.Position;
                if (lastPt != default)
                {
                    EraseAlongSegment(lastPt, curPos);
                }
                else
                {
                    EraseAtPoint(curPos);
                }
                lastPt = curPos;
            }
            if (intermediatePoints.Count > 0)
            {
                _lastTouchEraserPoints[touch.Id] = intermediatePoints[^1].Position;
            }
        }
        else if (_activeTouchStrokes.TryGetValue(touch.Id, out var stroke))
        {
            foreach (var pt in intermediatePoints)
            {
                stroke.StylusPoints.Add(new StylusPoint(pt.Position.X, pt.Position.Y));
            }
        }

        e.Handled = true;
    }

    private void OnTouchUp(object? sender, TouchEventArgs e)
    {
        var touch = e.TouchDevice;
        _lastTouchEraserPoints.Remove(touch.Id);

        if (_activeTouchStrokes.TryGetValue(touch.Id, out var stroke))
        {
            _activeTouchStrokes.Remove(touch.Id);
            StrokeCollected?.Invoke(stroke);
        }

        e.Handled = true;
    }

    private void OnTouchLeave(object? sender, TouchEventArgs e)
    {
        OnTouchUp(sender, e);
    }

    #endregion

    #region Stylus Handlers (전자칠판 전용 펜 / 디지타이저 스타일러스)

    private void OnStylusDown(object? sender, StylusDownEventArgs e)
    {
        // 손가락 터치스크린 입력은 TouchDown에서 처리하므로 중복 방지
        if (e.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch)
        {
            return;
        }

        var points = e.GetStylusPoints(_inkCanvas);
        if (points.Count == 0) return;

        bool isEraser = _isEraserMode || e.StylusDevice.Inverted;
        var pos = new Point(points[0].X, points[0].Y);

        if (isEraser)
        {
            _lastStylusEraserPoints[e.StylusDevice.Id] = pos;
            EraseAtPoint(pos);
        }
        else
        {
            var attributes = _inkCanvas.DefaultDrawingAttributes.Clone();
            var stroke = new Stroke(points, attributes);
            _activeStylusStrokes[e.StylusDevice.Id] = stroke;
            _inkCanvas.Strokes.Add(stroke);
        }

        e.Handled = true;
    }

    private void OnStylusMove(object? sender, StylusEventArgs e)
    {
        if (e.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch)
        {
            return;
        }

        var points = e.GetStylusPoints(_inkCanvas);
        if (points.Count == 0) return;

        bool isEraser = _isEraserMode || e.StylusDevice.Inverted;

        if (isEraser)
        {
            _lastStylusEraserPoints.TryGetValue(e.StylusDevice.Id, out var lastPt);
            foreach (var sp in points)
            {
                var curPos = new Point(sp.X, sp.Y);
                if (lastPt != default)
                {
                    EraseAlongSegment(lastPt, curPos);
                }
                else
                {
                    EraseAtPoint(curPos);
                }
                lastPt = curPos;
            }
            if (points.Count > 0)
            {
                _lastStylusEraserPoints[e.StylusDevice.Id] = new Point(points[^1].X, points[^1].Y);
            }
        }
        else if (_activeStylusStrokes.TryGetValue(e.StylusDevice.Id, out var stroke))
        {
            stroke.StylusPoints.Add(points);
        }

        e.Handled = true;
    }

    private void OnStylusUp(object? sender, StylusEventArgs e)
    {
        if (e.StylusDevice.TabletDevice?.Type == TabletDeviceType.Touch)
        {
            return;
        }

        _lastStylusEraserPoints.Remove(e.StylusDevice.Id);

        if (_activeStylusStrokes.TryGetValue(e.StylusDevice.Id, out var stroke))
        {
            _activeStylusStrokes.Remove(e.StylusDevice.Id);
            StrokeCollected?.Invoke(stroke);
        }

        e.Handled = true;
    }

    private void OnStylusLeave(object? sender, StylusEventArgs e)
    {
        // 펜이 잠시 호버 높이로 뜨더라도 버튼을 누르고 있지 않으면 정리
        if (_activeStylusStrokes.TryGetValue(e.StylusDevice.Id, out var stroke))
        {
            _activeStylusStrokes.Remove(e.StylusDevice.Id);
            _lastStylusEraserPoints.Remove(e.StylusDevice.Id);
            StrokeCollected?.Invoke(stroke);
        }
    }

    #endregion

    #region Mouse Handlers (교탁 PC 및 노트북 마우스 판서 100% 보장)

    private void OnMouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        // 현재 물리 터치나 펜이 직접 드로잉 중이라면 중복 마우스 합성 이벤트는 건너뜀
        if (_activeTouchStrokes.Count > 0 || _activeStylusStrokes.Count > 0)
        {
            return;
        }

        var pos = e.GetPosition(_inkCanvas);

        if (_isEraserMode)
        {
            _lastMouseEraserPoint = pos;
            EraseAtPoint(pos);
            _inkCanvas.CaptureMouse();
        }
        else
        {
            var attributes = _inkCanvas.DefaultDrawingAttributes.Clone();
            var stylusPoint = new StylusPoint(pos.X, pos.Y);
            _mouseStroke = new Stroke(new StylusPointCollection(new[] { stylusPoint }), attributes);
            _inkCanvas.Strokes.Add(_mouseStroke);
            _inkCanvas.CaptureMouse();
        }

        e.Handled = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (_mouseStroke != null || _lastMouseEraserPoint != null)
            {
                EndMouseDrawing();
            }
            return;
        }

        // 터치/펜 입력 진행 중일 때는 마우스 이동 무시
        if (_activeTouchStrokes.Count > 0 || _activeStylusStrokes.Count > 0)
        {
            return;
        }

        var pos = e.GetPosition(_inkCanvas);

        if (_isEraserMode)
        {
            if (_lastMouseEraserPoint.HasValue)
            {
                EraseAlongSegment(_lastMouseEraserPoint.Value, pos);
            }
            else
            {
                EraseAtPoint(pos);
            }
            _lastMouseEraserPoint = pos;
        }
        else if (_mouseStroke != null)
        {
            _mouseStroke.StylusPoints.Add(new StylusPoint(pos.X, pos.Y));
        }

        e.Handled = true;
    }

    private void OnMouseUp(object? sender, MouseButtonEventArgs e)
    {
        EndMouseDrawing();
        e.Handled = true;
    }

    private void OnMouseLeave(object? sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndMouseDrawing();
        }
    }

    private void EndMouseDrawing()
    {
        _lastMouseEraserPoint = null;

        if (_mouseStroke != null)
        {
            var stroke = _mouseStroke;
            _mouseStroke = null;
            try
            {
                if (_inkCanvas.IsMouseCaptured)
                {
                    _inkCanvas.ReleaseMouseCapture();
                }
            }
            catch
            {
                // Ignore capture release edge cases
            }
            StrokeCollected?.Invoke(stroke);
        }
        else if (_inkCanvas.IsMouseCaptured)
        {
            try
            {
                _inkCanvas.ReleaseMouseCapture();
            }
            catch
            {
                // Ignore
            }
        }
    }

    #endregion

    #region Smooth Segment Erasing (고속 문지름 건너뜀 없는 부드러운 지우개)

    private void EraseAlongSegment(Point p1, Point p2)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // 점 간 거리가 작으면 현재 점만 삭제
        if (dist <= 8.0)
        {
            EraseAtPoint(p2);
            return;
        }

        // 고속 문지름 시 궤적 사이를 8px 간격으로 촘촘히 보간하여 깨끗하게 삭제
        int steps = (int)Math.Ceiling(dist / 8.0);
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            Point interp = new Point(p1.X + dx * t, p1.Y + dy * t);
            EraseAtPoint(interp);
        }
    }

    private void EraseAtPoint(Point pt)
    {
        // 4K 전자칠판 및 일반 모니터에서 시원하게 지워지는 최적 반경 설정
        double radius = Math.Max(28.0, _inkCanvas.DefaultDrawingAttributes.Width * 4.0);
        var hitRect = new Rect(pt.X - radius, pt.Y - radius, radius * 2, radius * 2);

        var hitStrokes = _inkCanvas.Strokes.HitTest(hitRect, 8);
        if (hitStrokes != null && hitStrokes.Count > 0)
        {
            foreach (var stroke in hitStrokes)
            {
                _inkCanvas.Strokes.Remove(stroke);
            }
            StrokesErased?.Invoke(hitStrokes);
        }
    }

    #endregion
}
