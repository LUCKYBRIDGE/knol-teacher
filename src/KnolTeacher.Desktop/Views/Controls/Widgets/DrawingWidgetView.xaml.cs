using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace KnolTeacher.Desktop.Views.Controls.Widgets;

public partial class DrawingWidgetView : UserControl
{
    // Same undo rules as the screen overlay and the board ink layer: undo reverts the last
    // stroke or eraser gesture (including partial-eraser splits) instead of deleting the top stroke.
    private readonly DrawingUndoManager _undoManager = new();
    private bool _suppressUndoRecording;
    private bool _isReady = false;

    public DrawingWidgetView()
    {
        InitializeComponent();
        MiniInkCanvas.DefaultDrawingAttributes = new DrawingAttributes
        {
            Color = Colors.White,
            Width = 3,
            Height = 3,
            FitToCurve = true
        };

        // Same touch behaviour as the other ink surfaces: no press-and-hold ring, no flick gestures.
        Stylus.SetIsPressAndHoldEnabled(MiniInkCanvas, false);
        Stylus.SetIsFlicksEnabled(MiniInkCanvas, false);
        MiniInkCanvas.EraserShape = new EllipseStylusShape(DrawingEraserHelper.PointEraserDiameter, DrawingEraserHelper.PointEraserDiameter);

        MiniInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        MiniInkCanvas.Cursor = Cursors.Pen;
        MiniInkCanvas.StrokeCollected += MiniInkCanvas_StrokeCollected;
        MiniInkCanvas.Strokes.StrokesChanged += MiniInkStrokes_StrokesChanged;
        MiniInkCanvas.PreviewStylusDown += (s, e) => BeginEraseGestureIfNeeded();
        MiniInkCanvas.PreviewMouseDown += (s, e) =>
        {
            // Pen/touch input is promoted to mouse events as well; the stylus handler already ran.
            if (e.StylusDevice == null) BeginEraseGestureIfNeeded();
        };
        _isReady = true;
    }

    private void MiniInkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        if (_suppressUndoRecording) return;
        _undoManager.PushAdded(e.Stroke);
    }

    private void MiniInkStrokes_StrokesChanged(object? sender, StrokeCollectionChangedEventArgs e)
    {
        // New pen strokes are recorded via StrokeCollected; erasing reports Removed (+ fragments).
        if (_suppressUndoRecording || e.Removed.Count == 0) return;
        _undoManager.PushReplaced(e.Removed, e.Added);
    }

    private void BeginEraseGestureIfNeeded()
    {
        if (MiniInkCanvas.EditingMode is InkCanvasEditingMode.EraseByPoint or InkCanvasEditingMode.EraseByStroke)
        {
            // One eraser swipe becomes one undo step.
            _undoManager.BeginGroup();
        }
    }

    private void BtnModeGreen_Click(object sender, RoutedEventArgs e)
    {
        InkBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B4332"));
        MiniInkCanvas.DefaultDrawingAttributes.Color = Colors.White;
    }

    private void BtnModeWhite_Click(object sender, RoutedEventArgs e)
    {
        InkBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
        MiniInkCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString("#0F172A");
    }

    private void RbPen_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        _undoManager.EndGroup();
        MiniInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        MiniInkCanvas.Cursor = Cursors.Pen;
    }

    private void RbEraser_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        _undoManager.EndGroup();
        MiniInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
        MiniInkCanvas.Cursor = Cursors.Cross;
    }

    private void BtnColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            MiniInkCanvas.DefaultDrawingAttributes.Color = color;
            if (RbEraser.IsChecked == true)
            {
                RbPen.IsChecked = true;
            }
        }
    }

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        _suppressUndoRecording = true;
        try
        {
            _undoManager.Undo(MiniInkCanvas);
        }
        finally
        {
            _suppressUndoRecording = false;
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        if (MiniInkCanvas.Strokes.Count == 0) return;

        // Clearing is one undo step so a mis-tap on the board can be recovered.
        _undoManager.EndGroup();
        var snapshot = MiniInkCanvas.Strokes.ToList();
        _suppressUndoRecording = true;
        try
        {
            MiniInkCanvas.Strokes.Clear();
        }
        finally
        {
            _suppressUndoRecording = false;
        }
        _undoManager.PushRemoved(snapshot);
    }
}
