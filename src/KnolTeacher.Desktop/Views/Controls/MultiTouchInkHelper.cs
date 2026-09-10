using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace KnolTeacher.Desktop.Views.Controls;

/// <summary>
/// Enables true multi-touch simultaneous writing and erasing on a WPF InkCanvas.
/// Allows multiple students/teachers to write or erase on interactive displays at the same time.
/// </summary>
public class MultiTouchInkHelper
{
    private readonly InkCanvas _inkCanvas;
    private readonly Dictionary<int, Stroke> _activeTouchStrokes = new();
    private Stroke? _mouseStroke;
    private bool _isEraserMode;

    public event Action<Stroke>? StrokeCollected;
    public event Action<StrokeCollection>? StrokesErased;

    public bool IsEraserMode
    {
        get => _isEraserMode;
        set => _isEraserMode = value;
    }

    public MultiTouchInkHelper(InkCanvas inkCanvas)
    {
        _inkCanvas = inkCanvas ?? throw new ArgumentNullException(nameof(inkCanvas));

        // Disable standard single-touch/mouse processing so we can handle multi-touch directly
        _inkCanvas.EditingMode = InkCanvasEditingMode.None;

        _inkCanvas.TouchDown += OnTouchDown;
        _inkCanvas.TouchMove += OnTouchMove;
        _inkCanvas.TouchUp += OnTouchUp;
        _inkCanvas.TouchLeave += OnTouchUp;

        _inkCanvas.MouseDown += OnMouseDown;
        _inkCanvas.MouseMove += OnMouseMove;
        _inkCanvas.MouseUp += OnMouseUp;
        _inkCanvas.MouseLeave += OnMouseLeave;
    }

    #region Touch Handlers (Multi-Touch Simultaneous)

    private void OnTouchDown(object? sender, TouchEventArgs e)
    {
        var touch = e.TouchDevice;
        var point = touch.GetTouchPoint(_inkCanvas);
        var stylusPoint = new StylusPoint(point.Position.X, point.Position.Y);

        if (_isEraserMode)
        {
            EraseAtPoint(stylusPoint);
        }
        else
        {
            var attributes = _inkCanvas.DefaultDrawingAttributes.Clone();
            var stroke = new Stroke(new StylusPointCollection(new[] { stylusPoint }), attributes);

            _activeTouchStrokes[touch.Id] = stroke;
            _inkCanvas.Strokes.Add(stroke);
        }

        e.Handled = true;
    }

    private void OnTouchMove(object? sender, TouchEventArgs e)
    {
        var touch = e.TouchDevice;
        var point = touch.GetTouchPoint(_inkCanvas);
        var stylusPoint = new StylusPoint(point.Position.X, point.Position.Y);

        if (_isEraserMode)
        {
            EraseAtPoint(stylusPoint);
        }
        else
        {
            if (_activeTouchStrokes.TryGetValue(touch.Id, out var stroke))
            {
                stroke.StylusPoints.Add(stylusPoint);
            }
        }

        e.Handled = true;
    }

    private void OnTouchUp(object? sender, TouchEventArgs e)
    {
        var touch = e.TouchDevice;
        if (_activeTouchStrokes.TryGetValue(touch.Id, out var stroke))
        {
            _activeTouchStrokes.Remove(touch.Id);
            StrokeCollected?.Invoke(stroke);
        }

        e.Handled = true;
    }

    #endregion

    #region Mouse Handlers (Fallback for PC/Mouse use)

    private void OnMouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(_inkCanvas);
        var stylusPoint = new StylusPoint(pos.X, pos.Y);

        if (_isEraserMode)
        {
            EraseAtPoint(stylusPoint);
        }
        else
        {
            var attributes = _inkCanvas.DefaultDrawingAttributes.Clone();
            _mouseStroke = new Stroke(new StylusPointCollection(new[] { stylusPoint }), attributes);
            _inkCanvas.Strokes.Add(_mouseStroke);
        }

        _inkCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.StylusDevice != null) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(_inkCanvas);
        var stylusPoint = new StylusPoint(pos.X, pos.Y);

        if (_isEraserMode)
        {
            EraseAtPoint(stylusPoint);
        }
        else if (_mouseStroke != null)
        {
            _mouseStroke.StylusPoints.Add(stylusPoint);
        }

        e.Handled = true;
    }

    private void OnMouseUp(object? sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null) return;

        _inkCanvas.ReleaseMouseCapture();
        if (_mouseStroke != null)
        {
            StrokeCollected?.Invoke(_mouseStroke);
            _mouseStroke = null;
        }

        e.Handled = true;
    }

    private void OnMouseLeave(object? sender, MouseEventArgs e)
    {
        if (e.StylusDevice != null) return;

        if (_mouseStroke != null)
        {
            _inkCanvas.ReleaseMouseCapture();
            StrokeCollected?.Invoke(_mouseStroke);
            _mouseStroke = null;
        }
    }

    #endregion

    #region Erasing Logic

    private void EraseAtPoint(StylusPoint pt)
    {
        double radius = Math.Max(18, _inkCanvas.DefaultDrawingAttributes.Width * 3.5);
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
