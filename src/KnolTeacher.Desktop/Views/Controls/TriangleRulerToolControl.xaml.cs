using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KnolTeacher.Desktop.Views.Controls;

public partial class TriangleRulerToolControl : UserControl
{
    private bool _isDragging = false;
    private bool _isRotating = false;
    private double _startHandleAngleOffset;
    private Point _startPoint;
    public event Action? CloseRequested;

    public TriangleRulerToolControl()
    {
        InitializeComponent();
        MouseWheel += (s, e) =>
        {
            double delta = e.Delta > 0 ? 5 : -5;
            SetAngle(TriRotate.Angle + delta);
            e.Handled = true;
        };
    }

    private void SetAngle(double angle)
    {
        angle = (angle % 360 + 360) % 360;
        TriRotate.Angle = Math.Round(angle);
        TxtAngle.Text = $"{Math.Round(angle)}°";
    }

    private void BtnRotateCW_Click(object sender, RoutedEventArgs e) => SetAngle(TriRotate.Angle + 15);
    private void BtnRotateCCW_Click(object sender, RoutedEventArgs e) => SetAngle(TriRotate.Angle - 15);
    private void BtnResetAngle_Click(object sender, RoutedEventArgs e) => SetAngle(0);
    private void BtnClose_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();

    private void RotateHandle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Parent is UIElement canvas)
        {
            _isRotating = true;
            Point centerOnParent = TransformToAncestor(canvas).Transform(new Point(160, 130));
            Point currentOnParent = e.GetPosition(canvas);
            double mouseAngle = Math.Atan2(currentOnParent.Y - centerOnParent.Y, currentOnParent.X - centerOnParent.X) * 180.0 / Math.PI;
            _startHandleAngleOffset = mouseAngle - TriRotate.Angle;
            RotateHandleCorner.CaptureMouse();
            e.Handled = true;
        }
    }

    private void RotateHandle_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isRotating && Parent is UIElement canvas)
        {
            Point centerOnParent = TransformToAncestor(canvas).Transform(new Point(160, 130));
            Point currentOnParent = e.GetPosition(canvas);

            double mouseAngle = Math.Atan2(currentOnParent.Y - centerOnParent.Y, currentOnParent.X - centerOnParent.X) * 180.0 / Math.PI;
            SetAngle(mouseAngle - _startHandleAngleOffset);
            e.Handled = true;
        }
    }

    private void RotateHandle_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isRotating)
        {
            _isRotating = false;
            RotateHandleCorner.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Triangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
        if (e.OriginalSource is DependencyObject dep && (RotateHandleCorner.IsAncestorOf(dep) || ReferenceEquals(dep, RotateHandleCorner))) return;
        _isDragging = true;
        _startPoint = e.GetPosition(Parent as UIElement);
        CaptureMouse();
    }

    private void Triangle_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && Parent is Canvas canvas)
        {
            Point current = e.GetPosition(canvas);
            double dx = current.X - _startPoint.X;
            double dy = current.Y - _startPoint.Y;

            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            Canvas.SetLeft(this, left + dx);
            Canvas.SetTop(this, top + dy);
            _startPoint = current;
        }
    }

    private void Triangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }
}