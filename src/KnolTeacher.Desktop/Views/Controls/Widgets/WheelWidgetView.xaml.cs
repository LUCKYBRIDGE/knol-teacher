using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.Views.Controls;

namespace KnolTeacher.Desktop.Views.Controls.Widgets;

public class WheelItem
{
    public string Name { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;
    public Color Color { get; set; }
}

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
    private readonly List<WheelItem> _items = new();
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
        _items.Clear();
        switch (index)
        {
            case 1:
                foreach (int i in Enumerable.Range(1, 25))
                {
                    _items.Add(new WheelItem { Name = $"{i}번", Weight = 1 });
                }
                break;
            case 2:
                string[] subjects = { "국어", "수학", "사회", "과학", "영어", "음악", "미술", "체육", "도덕", "실과" };
                foreach (string s in subjects) _items.Add(new WheelItem { Name = s, Weight = 1 });
                break;
            case 3:
                string[] roles = { "칠판지우개", "우유당번", "환기반장", "줄서기도우미", "정리정돈", "책장정리" };
                foreach (string r in roles) _items.Add(new WheelItem { Name = r, Weight = 1 });
                break;
            case 4:
                // User custom input mode: default fruits
                string[] custom = { "사과", "바나나", "포도", "딸기", "오렌지" };
                foreach (string c in custom) _items.Add(new WheelItem { Name = c, Weight = 1 });
                PanelCustomInput.Visibility = Visibility.Visible;
                BtnToggleEdit.Content = "▲ 접기";
                break;
            default:
                foreach (int i in Enumerable.Range(1, 6))
                {
                    _items.Add(new WheelItem { Name = $"{i}모둠", Weight = 1 });
                }
                break;
        }

        AssignColors();
        RefreshAll();
    }

    private void AssignColors()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Color = SliceColors[i % SliceColors.Length];
        }
    }

    private void RefreshAll()
    {
        BuildItemUiList();
        RenderWheel();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        int totalWeight = _items.Sum(it => it.Weight);
        if (TxtItemSummary != null)
        {
            TxtItemSummary.Text = $"{_items.Count}개 (총 비율: {totalWeight})";
        }
    }

    private void BuildItemUiList()
    {
        if (PanelItemList == null) return;
        PanelItemList.Children.Clear();

        foreach (var item in _items)
        {
            var rowGrid = new Grid
            {
                Margin = new Thickness(0, 2, 0, 2),
                Background = new SolidColorBrush(Color.FromArgb(0x30, 0x1E, 0x29, 0x3B))
            };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 1. Color Dot
            var dot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(item.Color),
                Margin = new Thickness(4, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dot, 0);
            rowGrid.Children.Add(dot);

            // 2. Name TextBox
            var tbName = new TextBox
            {
                Text = item.Name,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 2, 5, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Height = 26
            };
            tbName.TextChanged += (s, e) =>
            {
                item.Name = tbName.Text;
                RenderWheel();
            };
            Grid.SetColumn(tbName, 1);
            rowGrid.Children.Add(tbName);

            // 3. Weight Stepper StackPanel: [-] [Weight] [+]
            var stepperPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(6, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var btnMinus = new Button
            {
                Content = "−",
                Style = (Style)FindResource("MiniStepperBtn"),
                ToolTip = "비율 1 감소"
            };
            var txtWeight = new TextBlock
            {
                Text = item.Weight.ToString(),
                Width = 24,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47))
            };
            var btnPlus = new Button
            {
                Content = "+",
                Style = (Style)FindResource("MiniStepperBtn"),
                ToolTip = "비율 1 증가"
            };

            btnMinus.Click += (s, e) =>
            {
                if (item.Weight > 1)
                {
                    item.Weight--;
                    txtWeight.Text = item.Weight.ToString();
                    RenderWheel();
                    UpdateSummary();
                }
            };
            btnPlus.Click += (s, e) =>
            {
                if (item.Weight < 99)
                {
                    item.Weight++;
                    txtWeight.Text = item.Weight.ToString();
                    RenderWheel();
                    UpdateSummary();
                }
            };

            stepperPanel.Children.Add(btnMinus);
            stepperPanel.Children.Add(txtWeight);
            stepperPanel.Children.Add(btnPlus);
            Grid.SetColumn(stepperPanel, 2);
            rowGrid.Children.Add(stepperPanel);

            // 4. Delete Button
            var btnDelete = new Button
            {
                Content = "✕",
                Width = 22,
                Height = 22,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "항목 삭제"
            };
            btnDelete.Click += (s, e) =>
            {
                if (_items.Count > 1)
                {
                    _items.Remove(item);
                    AssignColors();
                    RefreshAll();
                }
            };
            Grid.SetColumn(btnDelete, 3);
            rowGrid.Children.Add(btnDelete);

            PanelItemList.Children.Add(rowGrid);
        }
    }

    private void CbPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        LoadPreset(CbPresets.SelectedIndex);
    }

    private void BtnResetRatio_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
        {
            item.Weight = 1;
        }
        RefreshAll();
    }

    private void BtnToggleEdit_Click(object sender, RoutedEventArgs e)
    {
        if (PanelCustomInput.Visibility == Visibility.Visible)
        {
            PanelCustomInput.Visibility = Visibility.Collapsed;
            BtnToggleEdit.Content = "📝 일괄 입력";
        }
        else
        {
            // Populate TbCustomItems with current items: "name:weight, name:weight"
            TbCustomItems.Text = string.Join(", ", _items.Select(it => it.Weight > 1 ? $"{it.Name}:{it.Weight}" : it.Name));
            PanelCustomInput.Visibility = Visibility.Visible;
            BtnToggleEdit.Content = "▲ 접기";
        }
    }

    private void BtnApplyBulk_Click(object sender, RoutedEventArgs e)
    {
        string raw = TbCustomItems.Text;
        var parts = raw.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToList();

        if (parts.Count > 0)
        {
            _items.Clear();
            foreach (string p in parts)
            {
                string name = p;
                int weight = 1;
                int colonIdx = p.LastIndexOf(':');
                if (colonIdx > 0 && colonIdx < p.Length - 1)
                {
                    if (int.TryParse(p.Substring(colonIdx + 1).Trim(), out int parsedWeight) && parsedWeight >= 1)
                    {
                        name = p.Substring(0, colonIdx).Trim();
                        weight = Math.Clamp(parsedWeight, 1, 99);
                    }
                }
                _items.Add(new WheelItem { Name = name, Weight = weight });
            }
            AssignColors();
            RefreshAll();
        }

        PanelCustomInput.Visibility = Visibility.Collapsed;
        BtnToggleEdit.Content = "📝 일괄 입력";
    }

    private void BtnAddItem_Click(object sender, RoutedEventArgs e)
    {
        _items.Add(new WheelItem
        {
            Name = $"항목 {_items.Count + 1}",
            Weight = 1
        });
        AssignColors();
        RefreshAll();
    }

    private void RenderWheel()
    {
        if (WheelCanvas == null) return;
        WheelCanvas.Children.Clear();

        int count = _items.Count;
        if (count == 0) return;

        double cx = 160;
        double cy = 160;
        double radius = 158;

        int totalWeight = Math.Max(1, _items.Sum(it => it.Weight));

        if (count == 1)
        {
            var singleCircle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(_items[0].Color),
                Stroke = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(singleCircle, cx - radius);
            Canvas.SetTop(singleCircle, cy - radius);
            WheelCanvas.Children.Add(singleCircle);

            var textBlock = new TextBlock
            {
                Text = _items[0].Name,
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

        double currentAngleDeg = 0.0;
        for (int i = 0; i < count; i++)
        {
            var item = _items[i];
            double stepDeg = 360.0 * (item.Weight / (double)totalWeight);
            double startAngleDeg = currentAngleDeg;
            double endAngleDeg = currentAngleDeg + stepDeg;
            currentAngleDeg = endAngleDeg;

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
                Fill = new SolidColorBrush(item.Color),
                Stroke = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                StrokeThickness = 1.5
            };
            WheelCanvas.Children.Add(path);

            // Radial Text Label positioned at ~65% radius along midAngle
            if (stepDeg >= 12.0)
            {
                double midAngleDeg = startAngleDeg + stepDeg / 2.0;
                string labelText = item.Weight > 1 ? $"{item.Name} ({item.Weight})" : item.Name;
                double fontSize = stepDeg switch
                {
                    > 60 => 14.0,
                    > 35 => 12.5,
                    > 20 => 11.0,
                    _ => 9.5
                };

                var textBlock = new TextBlock
                {
                    Text = labelText,
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
    }

    private void BtnSpin_Click(object sender, RoutedEventArgs e)
    {
        if (_isSpinning || _items.Count == 0 || !_isActive || _disposed) return;

        CancelSpin();
        _spinCts = new CancellationTokenSource();

        _isSpinning = true;
        BtnSpin.IsEnabled = false;
        TxtResult.Text = "빙글빙글 회전 중...";
        TxtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));

        int totalWeight = Math.Max(1, _items.Sum(it => it.Weight));
        var rng = new Random();

        // Weighted random selection: pick a value in [0, totalWeight)
        double roll = rng.NextDouble() * totalWeight;
        double accumulated = 0.0;
        int winnerIndex = 0;
        double winnerStartAngle = 0.0;
        double winnerStepDeg = 0.0;

        double curAngle = 0.0;
        for (int i = 0; i < _items.Count; i++)
        {
            double step = 360.0 * (_items[i].Weight / (double)totalWeight);
            accumulated += _items[i].Weight;
            if (roll < accumulated || i == _items.Count - 1)
            {
                winnerIndex = i;
                winnerStartAngle = curAngle;
                winnerStepDeg = step;
                break;
            }
            curAngle += step;
        }

        WheelItem winner = _items[winnerIndex];
        double midAngleDeg = winnerStartAngle + winnerStepDeg / 2.0;

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
            TxtResult.Text = $"🎉 {winner.Name} 당첨!";
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


