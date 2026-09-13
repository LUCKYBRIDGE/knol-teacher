using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using KnolTeacher.Desktop.Models;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class StudentPickerWindow
{
    private bool _prehistoryThemeApplied;

    static StudentPickerWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(StudentPickerWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPrehistoryThemeLoaded));
    }

    private static void OnPrehistoryThemeLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is StudentPickerWindow window)
        {
            window.ApplyPrehistoryTheme();
        }
    }

    private void ApplyPrehistoryTheme()
    {
        if (_prehistoryThemeApplied || RaceCanvas == null || MinimapCanvas == null)
        {
            return;
        }

        _prehistoryThemeApplied = true;
        Title = "뽑기 레이스 · 선사시대 탐험";
        RaceViewport.Background = BrushFrom("#20221E");
        RaceCanvas.Background = BrushFrom("#26332B");

        SuppressLegacyTrackGuides();
        AddPrehistoryMapLayers();
        ApplyPrehistoryMinimapTheme();
    }

    private void SuppressLegacyTrackGuides()
    {
        var legacyBackground = RaceCanvas.Children
            .OfType<Rectangle>()
            .FirstOrDefault(rect => rect.Width >= 679 && rect.Height >= 3499);

        if (legacyBackground != null)
        {
            legacyBackground.Fill = Brushes.Transparent;
        }

        foreach (var guide in RaceCanvas.Children.OfType<TextBlock>())
        {
            if (guide.Text.Contains("m —", StringComparison.Ordinal))
            {
                guide.Visibility = Visibility.Collapsed;
            }
        }

        var legacyCenterLine = RaceCanvas.Children
            .OfType<Rectangle>()
            .FirstOrDefault(rect => rect.Width <= 3 && rect.Height >= 3000);

        if (legacyCenterLine != null)
        {
            legacyCenterLine.Visibility = Visibility.Collapsed;
        }
    }

    private void AddPrehistoryMapLayers()
    {
        foreach (var zone in PrehistoryRaceThemeSpec.Zones)
        {
            var layer = new Rectangle
            {
                Width = TrackWidth,
                Height = zone.EndY - zone.StartY,
                Fill = BrushFrom(zone.BackgroundHex),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(layer, 0);
            Canvas.SetTop(layer, zone.StartY);
            Panel.SetZIndex(layer, -40);
            RaceCanvas.Children.Add(layer);

            AddZoneBanner(zone);
        }

        Geometry trackGeometry = BuildPhysicsAlignedTrackGeometry();
        var trackShadow = new System.Windows.Shapes.Path
        {
            Data = trackGeometry,
            Fill = BrushFrom("#76563A"),
            Stroke = BrushFrom("#2B211A"),
            StrokeThickness = 16,
            Opacity = 0.96,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(trackShadow, -30);
        RaceCanvas.Children.Add(trackShadow);

        var trackSurface = new System.Windows.Shapes.Path
        {
            Data = trackGeometry,
            Fill = BrushFrom("#C89D68"),
            Stroke = BrushFrom("#E8C58B"),
            StrokeThickness = 4,
            Opacity = 0.98,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(trackSurface, -29);
        RaceCanvas.Children.Add(trackSurface);

        AddTrackTexture();
        AddPrehistoryStructures();

        foreach (var artifact in PrehistoryRaceThemeSpec.Artifacts)
        {
            AddArtifactLandmark(artifact);
        }

        AddEraTransitionRibbon();
    }

    private Geometry BuildPhysicsAlignedTrackGeometry()
    {
        const double step = 40.0;
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            GetTrackBoundaries(0, out double firstLeft, out _);
            context.BeginFigure(new Point(firstLeft, 0), true, true);

            for (double y = step; y <= PrehistoryRaceThemeSpec.TrackHeight; y += step)
            {
                double sampleY = Math.Min(y, PrehistoryRaceThemeSpec.TrackHeight);
                GetTrackBoundaries(sampleY, out double left, out _);
                context.LineTo(new Point(left, sampleY), true, false);
            }

            for (double y = PrehistoryRaceThemeSpec.TrackHeight; y >= 0; y -= step)
            {
                GetTrackBoundaries(y, out _, out double right);
                context.LineTo(new Point(right, y), true, false);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private void AddTrackTexture()
    {
        var marks = new (double X, double Y, double W, double H)[]
        {
            (218, 270, 12, 7), (455, 420, 9, 6), (286, 650, 11, 6),
            (206, 860, 10, 7), (446, 1110, 12, 7), (302, 1320, 8, 5),
            (180, 1520, 11, 6), (486, 1770, 12, 7), (324, 1990, 9, 6),
            (210, 2210, 11, 7), (452, 2440, 10, 6), (280, 2730, 12, 7),
            (190, 2940, 10, 6), (420, 3170, 11, 7), (330, 3290, 8, 5)
        };

        foreach (var mark in marks)
        {
            var pebble = new Ellipse
            {
                Width = mark.W,
                Height = mark.H,
                Fill = BrushFrom("#8E6847"),
                Opacity = 0.52,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(pebble, mark.X);
            Canvas.SetTop(pebble, mark.Y);
            Panel.SetZIndex(pebble, -27);
            RaceCanvas.Children.Add(pebble);
        }
    }

    private void AddZoneBanner(PrehistoryRaceZone zone)
    {
        double top = zone.StartY + (zone.StartY == 0 ? 205 : 28);
        var border = new Border
        {
            Width = 226,
            MinHeight = 58,
            Background = BrushFrom("#E92B211A"),
            BorderBrush = BrushFrom(zone.AccentHex),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(13),
            Padding = new Thickness(11, 7, 11, 7),
            IsHitTestVisible = false
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = $"{zone.Period}  ·  {zone.Title}",
            Foreground = BrushFrom("#FFF4D8"),
            FontSize = 14,
            FontWeight = FontWeights.Bold
        });
        stack.Children.Add(new TextBlock
        {
            Text = zone.Subtitle,
            Foreground = BrushFrom("#E8D9C4"),
            FontSize = 9.5,
            Margin = new Thickness(0, 3, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        border.Child = stack;

        Canvas.SetLeft(border, 227);
        Canvas.SetTop(border, top);
        Panel.SetZIndex(border, -8);
        RaceCanvas.Children.Add(border);
    }

    private void AddArtifactLandmark(PrehistoryArtifactLandmark artifact)
    {
        const double cardWidth = 154;
        const double cardHeight = 78;
        var card = new Border
        {
            Width = cardWidth,
            Height = cardHeight,
            Background = BrushFrom("#F4E5C8"),
            BorderBrush = BrushFrom(artifact.Period == "구석기" ? "#7C5539" : "#576B3B"),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(7, 6, 7, 6),
            Opacity = 0.96,
            IsHitTestVisible = false
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        FrameworkElement icon = CreateArtifactIcon(artifact.Key);
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var text = new StackPanel { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock
        {
            Text = $"{artifact.Period} · {artifact.Name}",
            Foreground = BrushFrom("#3A2A21"),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold
        });
        text.Children.Add(new TextBlock
        {
            Text = artifact.Caption,
            Foreground = BrushFrom("#5F4B3E"),
            FontSize = 8.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        card.Child = grid;

        Canvas.SetLeft(card, artifact.AlignRight ? TrackWidth - cardWidth - 8 : 8);
        Canvas.SetTop(card, artifact.Y);
        Panel.SetZIndex(card, -6);
        RaceCanvas.Children.Add(card);
    }

    private FrameworkElement CreateArtifactIcon(string key)
    {
        var canvas = new Canvas { Width = 46, Height = 56, IsHitTestVisible = false };
        Brush outline = BrushFrom("#4A3427");

        switch (key)
        {
            case "chopper":
                canvas.Children.Add(new Polygon
                {
                    Points = new PointCollection
                    {
                        new(7, 15), new(23, 6), new(39, 13), new(42, 31),
                        new(30, 44), new(14, 42), new(5, 29)
                    },
                    Fill = BrushFrom("#8E8576"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                });
                AddLine(canvas, 11, 17, 25, 10, "#D5C9B4", 2);
                AddLine(canvas, 8, 24, 19, 20, "#D5C9B4", 2);
                AddLine(canvas, 31, 12, 39, 20, "#5E584F", 2);
                break;

            case "handaxe":
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M23,3 C34,12 41,24 37,35 C34,43 28,49 23,53 C17,48 10,42 8,34 C5,24 12,12 23,3 Z"),
                    Fill = BrushFrom("#9B8667"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                });
                AddLine(canvas, 23, 7, 17, 43, "#CDB998", 1.8);
                AddLine(canvas, 23, 7, 30, 43, "#6E5B45", 1.5);
                break;

            case "bone-needle":
                AddLine(canvas, 8, 46, 37, 12, "#F2E4BF", 5.2);
                AddLine(canvas, 8, 46, 37, 12, "#6C5A42", 1.0);
                var eye = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = BrushFrom("#F2E4BF"),
                    Stroke = outline,
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(eye, 33);
                Canvas.SetTop(eye, 6);
                canvas.Children.Add(eye);
                var hole = new Ellipse { Width = 3, Height = 3, Fill = outline };
                Canvas.SetLeft(hole, 36);
                Canvas.SetTop(hole, 9);
                canvas.Children.Add(hole);
                break;

            case "polished-stone":
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M12,45 C7,37 8,24 15,12 C20,4 30,5 35,13 C40,23 39,36 32,46 C27,52 17,51 12,45 Z"),
                    Fill = BrushFrom("#748477"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                });
                AddLine(canvas, 15, 39, 33, 17, "#AFC1B4", 2.2);
                break;

            case "spindle-whorl":
                var whorl = new Ellipse
                {
                    Width = 38,
                    Height = 38,
                    Fill = BrushFrom("#C77F4C"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                };
                Canvas.SetLeft(whorl, 4);
                Canvas.SetTop(whorl, 9);
                canvas.Children.Add(whorl);
                var whorlHole = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = BrushFrom("#4A3427"),
                    Stroke = BrushFrom("#EAB77A"),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(whorlHole, 18);
                Canvas.SetTop(whorlHole, 23);
                canvas.Children.Add(whorlHole);
                AddLine(canvas, 10, 20, 36, 36, "#EAB77A", 1.3);
                AddLine(canvas, 10, 36, 36, 20, "#EAB77A", 1.3);
                break;

            case "shell-mask":
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M23,4 C36,7 43,18 42,31 C41,43 34,51 23,53 C12,51 5,43 4,31 C3,18 10,7 23,4 Z"),
                    Fill = BrushFrom("#EFE2C2"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                });
                AddLine(canvas, 23, 8, 23, 48, "#C9AE7C", 1.2);
                AddLine(canvas, 12, 13, 18, 47, "#D8C397", 1.1);
                AddLine(canvas, 34, 13, 28, 47, "#D8C397", 1.1);
                AddMaskEye(canvas, 12, 23);
                AddMaskEye(canvas, 28, 23);
                break;
        }

        return canvas;
    }

    private void AddPrehistoryStructures()
    {
        AddCampfire(82, 735);
        AddCave(478, 835);
        AddRiverStones();
        AddNeolithicHut(34, 2345);
        AddNeolithicHut(488, 2660);
        AddCoastWaves();
    }

    private void AddCampfire(double x, double y)
    {
        var scene = new Canvas { Width = 95, Height = 80, IsHitTestVisible = false, Opacity = 0.9 };
        for (int i = 0; i < 5; i++)
        {
            var stone = new Ellipse { Width = 18, Height = 10, Fill = BrushFrom("#70665A"), Stroke = BrushFrom("#3F3932"), StrokeThickness = 1 };
            Canvas.SetLeft(stone, 14 + i * 13);
            Canvas.SetTop(stone, 56 + Math.Abs(2 - i) * 2);
            scene.Children.Add(stone);
        }
        var flame = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M46,58 C27,48 34,34 43,26 C42,39 52,36 54,18 C70,34 67,49 55,58 Z"),
            Fill = BrushFrom("#F59E0B"),
            Stroke = BrushFrom("#7C2D12"),
            StrokeThickness = 2
        };
        scene.Children.Add(flame);
        Canvas.SetLeft(scene, x);
        Canvas.SetTop(scene, y);
        Panel.SetZIndex(scene, -12);
        RaceCanvas.Children.Add(scene);
    }

    private void AddCave(double x, double y)
    {
        var scene = new Canvas { Width = 190, Height = 125, IsHitTestVisible = false, Opacity = 0.94 };
        var rock = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M6,116 C8,59 35,18 84,7 C133,-2 177,34 185,116 Z"),
            Fill = BrushFrom("#6E6257"),
            Stroke = BrushFrom("#332B27"),
            StrokeThickness = 4
        };
        scene.Children.Add(rock);
        var entrance = new Ellipse
        {
            Width = 92,
            Height = 91,
            Fill = BrushFrom("#211C1A"),
            Stroke = BrushFrom("#4A4039"),
            StrokeThickness = 3
        };
        Canvas.SetLeft(entrance, 50);
        Canvas.SetTop(entrance, 33);
        scene.Children.Add(entrance);
        AddLine(scene, 25, 88, 48, 55, "#978A78", 3);
        AddLine(scene, 153, 90, 133, 53, "#978A78", 3);
        Canvas.SetLeft(scene, x);
        Canvas.SetTop(scene, y);
        Panel.SetZIndex(scene, -13);
        RaceCanvas.Children.Add(scene);
    }

    private void AddRiverStones()
    {
        var river = new Rectangle
        {
            Width = 680,
            Height = 150,
            Fill = BrushFrom("#3F7A78"),
            Opacity = 0.46,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(river, 0);
        Canvas.SetTop(river, 1760);
        Panel.SetZIndex(river, -25);
        RaceCanvas.Children.Add(river);

        var stones = new (double X, double Y, double W)[]
        {
            (160, 1795, 58), (235, 1825, 52), (310, 1788, 60),
            (390, 1825, 54), (470, 1798, 58)
        };
        foreach (var item in stones)
        {
            var stone = new Ellipse
            {
                Width = item.W,
                Height = 26,
                Fill = BrushFrom("#8D8B79"),
                Stroke = BrushFrom("#4E5148"),
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(stone, item.X);
            Canvas.SetTop(stone, item.Y);
            Panel.SetZIndex(stone, -23);
            RaceCanvas.Children.Add(stone);
        }
    }

    private void AddNeolithicHut(double x, double y)
    {
        var scene = new Canvas { Width = 158, Height = 104, IsHitTestVisible = false, Opacity = 0.94 };
        var wall = new Border
        {
            Width = 118,
            Height = 54,
            Background = BrushFrom("#B68A55"),
            BorderBrush = BrushFrom("#5C412A"),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(5)
        };
        Canvas.SetLeft(wall, 20);
        Canvas.SetTop(wall, 47);
        scene.Children.Add(wall);
        var roof = new Polygon
        {
            Points = new PointCollection { new(8, 55), new(78, 4), new(150, 55) },
            Fill = BrushFrom("#827044"),
            Stroke = BrushFrom("#463A28"),
            StrokeThickness = 3
        };
        scene.Children.Add(roof);
        var door = new Rectangle { Width = 28, Height = 42, Fill = BrushFrom("#433229"), RadiusX = 12, RadiusY = 12 };
        Canvas.SetLeft(door, 65);
        Canvas.SetTop(door, 59);
        scene.Children.Add(door);
        AddLine(scene, 32, 58, 48, 96, "#D1A86E", 2);
        AddLine(scene, 126, 58, 110, 96, "#D1A86E", 2);
        Canvas.SetLeft(scene, x);
        Canvas.SetTop(scene, y);
        Panel.SetZIndex(scene, -12);
        RaceCanvas.Children.Add(scene);
    }

    private void AddCoastWaves()
    {
        for (int row = 0; row < 4; row++)
        {
            var wave = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M0,12 C24,0 48,24 72,12 C96,0 120,24 144,12 C168,0 192,24 216,12"),
                Stroke = BrushFrom(row % 2 == 0 ? "#A7E3E7" : "#78C6CE"),
                StrokeThickness = 3,
                Opacity = 0.56,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(wave, row % 2 == 0 ? 42 : 418);
            Canvas.SetTop(wave, 3190 + row * 52);
            Panel.SetZIndex(wave, -15);
            RaceCanvas.Children.Add(wave);
        }
    }

    private void AddEraTransitionRibbon()
    {
        var ribbon = new Border
        {
            Width = 360,
            Height = 42,
            Background = BrushFrom("#E9372F27"),
            BorderBrush = BrushFrom("#D6B47A"),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(21),
            IsHitTestVisible = false
        };
        ribbon.Child = new TextBlock
        {
            Text = "이동 생활에서 정착 생활로 · 구석기 → 신석기",
            Foreground = BrushFrom("#FFF0CF"),
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Canvas.SetLeft(ribbon, 160);
        Canvas.SetTop(ribbon, 2025);
        Panel.SetZIndex(ribbon, -5);
        RaceCanvas.Children.Add(ribbon);
    }

    private void ApplyPrehistoryMinimapTheme()
    {
        var zonePanel = MinimapCanvas.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Math.Abs(panel.Width - 154) < 1 && Math.Abs(panel.Height - 620) < 1);

        if (zonePanel == null)
        {
            return;
        }

        var zoneBorders = zonePanel.Children.OfType<Border>().Take(5).ToArray();
        string[] icons = { "🪨", "🔥", "🌊", "🏠", "🐚" };

        for (int i = 0; i < zoneBorders.Length && i < PrehistoryRaceThemeSpec.Zones.Count; i++)
        {
            var zone = PrehistoryRaceThemeSpec.Zones[i];
            var border = zoneBorders[i];
            border.Background = BrushFrom(zone.BackgroundHex);
            border.BorderBrush = BrushFrom("#506052");

            if (border.Child is TextBlock label)
            {
                label.Text = $"{icons[i]} {zone.Title}";
                label.Foreground = BrushFrom("#F6E8CC");
                label.FontSize = 9.5;
                label.FontWeight = FontWeights.Bold;
            }
        }
    }

    private static SolidColorBrush BrushFrom(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    private static void AddLine(Canvas canvas, double x1, double y1, double x2, double y2, string hex, double thickness)
    {
        canvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = BrushFrom(hex),
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false
        });
    }

    private static void AddMaskEye(Canvas canvas, double x, double y)
    {
        var eye = new Ellipse
        {
            Width = 8,
            Height = 6,
            Fill = BrushFrom("#4A3427"),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(eye, x);
        Canvas.SetTop(eye, y);
        canvas.Children.Add(eye);
    }
}
