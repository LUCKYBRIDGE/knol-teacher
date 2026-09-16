using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.Views.Controls;

namespace KnolTeacher.Desktop.Views.Controls.Widgets;

public partial class WheelWidgetView : UserControl, IWidgetLifecycle
{
    private static readonly Color[] SliceColors = new[]
    {
        Color.FromRgb(0x25, 0x63, 0xEB), // Blue
        Color.FromRgb(0x05, 0x96, 0x69), // Emerald
        Color.FromRgb(0xD9, 0x77, 0x06), // Amber
        Color.FromRgb(0xDB, 0x27, 0x77), // Pink
        Color.FromRgb(0x7C, 0x3A, 0xED), // Purple
        Color.FromRgb(0x08, 0x91, 0xB2), // Cyan
        Color.FromRgb(0xDC, 0x26, 0x26), // Red
        Color.FromRgb(0x0D, 0x94, 0x88), // Teal
        Color.FromRgb(0xEA, 0x58, 0x0C), // Orange
        Color.FromRgb(0x4F, 0x46, 0xE5)  // Indigo
    };

    private readonly ISoundService? _soundService;
    private List<string> _activeItems = new();
    private CancellationTokenSource? _spinCts;
    private bool _isSpinning = false;
    private bool _isReady = false;
    private bool _isActive = false;
    private bool _disposed = false;

    public WheelWidgetView(ISoundService? soundService = null)
    {
        _soundService = soundService;
        InitializeComponent();
        _isReady = true;
        LoadPreset(0);
    }

    public void Activate()
    {
        if (_disposed) return;
        _isActive = true;
    }

    public void Deactivate()
    {
        if (_disposed || !_isActive) return;
        _isActive = false;
        CancelSpin();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _isActive = false;
        CancelSpin();
        _disposed = true;
    }

    private void CancelSpin()
    {
        var cts = _spinCts;
        _spinCts = null;
        if (cts != null)
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }

        if (WheelRotateTransform != null)
        {
            WheelRotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
        }

        _isSpinning = false;
        _soundService?.StopAll();
        if (!_disposed && BtnSpin != null)
        {
            BtnSpin.IsEnabled = true;
        }
    }

    private void LoadPreset(int index)
    {
        string text = index switch
        {
            1 => string.Join(", ", Enumerable.Range(1, 25).Select(i => $"{i}번")),
            2 => "국어, 수학, 사회, 과학, 영어, 음악, 미술, 체육, 도덕, 실과",
            3 => "칠판지우개, 우유당번, 환기반장, 줄서기도우미, 정리정돈, 책장정리",
            4 => string.IsNullOrWhiteSpace(TbCustomItems.Text) ? "사과, 바나나, 포도, 딸기, 오렌지" : TbCustomItems.Text,
            _ => "1모둠, 2모둠, 3모둠, 4모둠, 5모둠, 6모둠"
        };

        TbCustomItems.Text = text;
        UpdateActiveItemsFromText();

        if (index == 4)
        {
            PanelCustomInput.Visibility = Visibility.Visible;
            BtnToggleEdit.Content = "▲ 접기";
        }
    }

    private void CbPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        LoadPreset(CbPresets.SelectedIndex);
    }

    private void BtnToggleEdit_Click(object sender, RoutedEventArgs e)
    {
        if (PanelCustomInput.Visibility == Visibility.Visible)
        {
            PanelCustomInput.Visibility = Visibility.Collapsed;
            BtnToggleEdit.Content = "✏️ 항목 편집";
        }
        else
        {
            PanelCustomInput.Visibility = Visibility.Visible;
            BtnToggleEdit.Content = "▲ 접기";
        }
    }

    private void TbCustomItems_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isReady) return;
        UpdateActiveItemsFromText();
    }

    private void UpdateActiveItemsFromText()
    {
        if (TbCustomItems == null) return;
        string raw = TbCustomItems.Text;
        var parts = raw.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToList();

        _activeItems = parts.Count > 0 ? parts : new List<string> { "항목 없음" };
        if (TxtItemCount != null)
        {
            TxtItemCount.Text = $"총 {_activeItems.Count}개 항목 등록됨";
        }

        RenderWheel();
    }

    private void RenderWheel()
    {
        if (WheelCanvas == null) return;
        WheelCanvas.Children.Clear();

        int count = _activeItems.Count;
        if (count == 0) return;

        double cx = 160;
        double cy = 160;
        double radius = 158;

        if (count == 1)
        {
            var singleCircle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(SliceColors[0]),
                Stroke = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(singleCircle, cx - radius);
            Canvas.SetTop(singleCircle, cy - radius);
            WheelCanvas.Children.Add(singleCircle);

            var textBlock = new TextBlock
            {
                Text = _activeItems[0],
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 180,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(textBlock, cx - textBlock.DesiredSize.Width / 2.0);
            Canvas.SetTop(textBlock, cy - textBlock.DesiredSize.Height / 2.0);
            WheelCanvas.Children.Add(textBlock);
            return;
        }

        double stepDeg = 360.0 / count;
        double fontSize = count switch
        {
            > 20 => 9.5,
            > 12 => 11.0,
            > 8 => 12.0,
            _ => 13.5
        };

        for (int i = 0; i < count; i++)
        {
            double startAngleDeg = i * stepDeg;
            double endAngleDeg = (i + 1) * stepDeg;

            // 12 o'clock is 0 rad, clockwise: x = cx + r*sin(a), y = cy - r*cos(a)
            double startRad = startAngleDeg * Math.PI / 180.0;
            double endRad = endAngleDeg * Math.PI / 180.0;

            Point pStart = new(cx + radius * Math.Sin(startRad), cy - radius * Math.Cos(startRad));
            Point pEnd = new(cx + radius * Math.Sin(endRad), cy - radius * Math.Cos(endRad));

            var figure = new PathFigure
            {
                StartPoint = new Point(cx, cy),
                IsClosed = true,
                IsFilled = true
            };
            figure.Segments.Add(new LineSegment(pStart, true));
            figure.Segments.Add(new ArcSegment(
                pEnd,
                new Size(radius, radius),
                0,
                stepDeg > 180.0,
                SweepDirection.Clockwise,
                true
            ));
            figure.Segments.Add(new LineSegment(new Point(cx, cy), true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            var path = new Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(SliceColors[i % SliceColors.Length]),
                Stroke = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                StrokeThickness = 1.5
            };
            WheelCanvas.Children.Add(path);

            // Radial Text Label positioned at ~65% radius along midAngle
            double midAngleDeg = startAngleDeg + stepDeg / 2.0;
            var textBlock = new TextBlock
            {
                Text = _activeItems[i],
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = fontSize,
                MaxWidth = radius * 0.55,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center
            };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double tw = textBlock.DesiredSize.Width;
            double th = textBlock.DesiredSize.Height;

            double textDistance = radius * 0.65;
            Canvas.SetLeft(textBlock, cx - tw / 2.0);
            Canvas.SetTop(textBlock, cy - textDistance - th / 2.0);

            // Rotate around the center of the wheel (cx, cy)
            textBlock.RenderTransform = new RotateTransform(midAngleDeg, tw / 2.0, textDistance + th / 2.0);
            WheelCanvas.Children.Add(textBlock);
        }
    }

    private void BtnSpin_Click(object sender, RoutedEventArgs e)
    {
        if (_isSpinning || _activeItems.Count == 0 || !_isActive || _disposed) return;

        CancelSpin();
        _spinCts = new CancellationTokenSource();

        _isSpinning = true;
        BtnSpin.IsEnabled = false;
        TxtResult.Text = "빙글빙글 회전 중...";
        TxtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));

        var rng = new Random();
        int winnerIndex = rng.Next(_activeItems.Count);
        string winner = _activeItems[winnerIndex];

        int count = _activeItems.Count;
        double stepDeg = 360.0 / count;
        double midAngleDeg = winnerIndex * stepDeg + stepDeg / 2.0;

        // The top indicator is fixed at 12 o'clock (0 deg).
        // For slice at midAngleDeg to stop at 12 o'clock, the wheel angle must satisfy:
        // (wheelAngle + midAngleDeg) % 360 == 0 => wheelAngle == 360 - midAngleDeg
        double targetStopAngle = (360.0 - midAngleDeg) % 360.0;
        if (targetStopAngle < 0) targetStopAngle += 360.0;

        double currentAngle = WheelRotateTransform.Angle % 360.0;
        if (currentAngle < 0) currentAngle += 360.0;

        double delta = targetStopAngle - currentAngle;
        if (delta < 0) delta += 360.0;

        // 5 to 7 full rotations plus delta
        double spins = 360.0 * (5 + rng.Next(3));
        double targetAngle = WheelRotateTransform.Angle + spins + delta;

        _soundService?.PlayDrumroll();

        var animation = new DoubleAnimation
        {
            From = WheelRotateTransform.Angle,
            To = targetAngle,
            Duration = TimeSpan.FromSeconds(3.6),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        animation.Completed += (s, args) =>
        {
            if (_disposed || !_isActive) return;

            WheelRotateTransform.Angle = targetStopAngle;
            TxtResult.Text = $"🎉 {winner}!";
            TxtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            _soundService?.PlayChime();

            _isSpinning = false;
            if (BtnSpin != null)
            {
                BtnSpin.IsEnabled = true;
            }
        };

        WheelRotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
    }
}

