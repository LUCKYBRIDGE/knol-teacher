using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using KnolTeacher.Desktop.Models;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class StudentPickerWindow
{
    private static readonly bool BronzeAgeThemeHookRegistered = RegisterBronzeAgeThemeHook();

    private static bool RegisterBronzeAgeThemeHook()
    {
        EventManager.RegisterClassHandler(
            typeof(StudentPickerWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnBronzeAgeThemeLoaded));
        return true;
    }

    private static void OnBronzeAgeThemeLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not StudentPickerWindow window)
        {
            return;
        }

        _ = window.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(window.ApplyBronzeAgeThemePolish));
    }

    private void ApplyBronzeAgeThemePolish()
    {
        if (RaceCanvas == null || MinimapCanvas == null)
        {
            return;
        }

        Title = "뽑기 레이스 · 구석기 → 신석기 → 청동기";

        MoveFirstEraTransitionRibbon();
        AddBronzeAgeTransitionRibbon();
        RelocateNeolithicCoastWaves();
        AddBronzeAgeStructures();
        PopulateBronzeAgeArtifactCards();
        RebuildThreeEraMinimap();
    }

    private void MoveFirstEraTransitionRibbon()
    {
        foreach (var border in RaceCanvas.Children.OfType<Border>())
        {
            if (border.Child is TextBlock label &&
                label.Text.Contains("구석기 → 신석기", StringComparison.Ordinal))
            {
                Canvas.SetTop(border, 1146);
                return;
            }
        }
    }

    private void AddBronzeAgeTransitionRibbon()
    {
        if (RaceCanvas.Children
            .OfType<Border>()
            .Any(border => border.Child is TextBlock text &&
                           text.Text.Contains("신석기 → 청동기", StringComparison.Ordinal)))
        {
            return;
        }

        var ribbon = new Border
        {
            Width = 360,
            Height = 42,
            Background = BrushFrom("#E93A3027"),
            BorderBrush = BrushFrom("#C99554"),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(21),
            IsHitTestVisible = false
        };
        ribbon.Child = new TextBlock
        {
            Text = "농경과 마을의 성장 · 신석기 → 청동기",
            Foreground = BrushFrom("#FFF0CF"),
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Canvas.SetLeft(ribbon, 160);
        Canvas.SetTop(ribbon, 2346);
        Panel.SetZIndex(ribbon, -5);
        RaceCanvas.Children.Add(ribbon);
    }

    private void RelocateNeolithicCoastWaves()
    {
        var legacyWaves = RaceCanvas.Children
            .OfType<System.Windows.Shapes.Path>()
            .Where(path => Panel.GetZIndex(path) == -15 && Canvas.GetTop(path) >= 3180)
            .ToArray();

        foreach (var wave in legacyWaves)
        {
            RaceCanvas.Children.Remove(wave);
        }

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
            Canvas.SetTop(wave, 2200 + row * 42);
            Panel.SetZIndex(wave, -15);
            RaceCanvas.Children.Add(wave);
        }
    }

    private void AddBronzeAgeStructures()
    {
        AddBronzeRiceField(28, 2520);
        AddBronzeStorageJars(514, 2780);
        AddDolmenStructure(42, 3135);
        AddDolmenStructure(482, 3340, 0.78);
    }

    private void AddBronzeRiceField(double x, double y)
    {
        var field = new Canvas
        {
            Width = 150,
            Height = 120,
            IsHitTestVisible = false,
            Opacity = 0.9
        };

        var ground = new Rectangle
        {
            Width = 148,
            Height = 108,
            Fill = BrushFrom("#715536"),
            Stroke = BrushFrom("#3F3528"),
            StrokeThickness = 2,
            RadiusX = 10,
            RadiusY = 10
        };
        field.Children.Add(ground);

        for (int row = 0; row < 5; row++)
        {
            double top = 14 + row * 18;
            AddLine(field, 12, top, 136, top, "#A6814F", 4);
            for (int plant = 0; plant < 6; plant++)
            {
                double px = 20 + plant * 21;
                AddLine(field, px, top - 6, px, top + 4, "#D6B354", 2);
                AddLine(field, px, top - 3, px - 5, top - 8, "#D6B354", 1.4);
                AddLine(field, px, top - 3, px + 5, top - 8, "#D6B354", 1.4);
            }
        }

        Canvas.SetLeft(field, x);
        Canvas.SetTop(field, y);
        Panel.SetZIndex(field, -12);
        RaceCanvas.Children.Add(field);
    }

    private void AddBronzeStorageJars(double x, double y)
    {
        var scene = new Canvas
        {
            Width = 140,
            Height = 92,
            IsHitTestVisible = false,
            Opacity = 0.92
        };

        AddPottery(scene, 8, 20, 46, 60, "#B06F49");
        AddPottery(scene, 53, 7, 54, 72, "#C17A4D");
        AddPottery(scene, 98, 27, 36, 52, "#9F6545");

        Canvas.SetLeft(scene, x);
        Canvas.SetTop(scene, y);
        Panel.SetZIndex(scene, -12);
        RaceCanvas.Children.Add(scene);
    }

    private static void AddPottery(Canvas canvas, double x, double y, double width, double height, string fillHex)
    {
        var pot = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M10,2 L34,2 C32,10 34,14 40,20 C48,30 46,48 38,56 C30,64 14,64 6,56 C-2,48 -4,30 4,20 C10,14 12,10 10,2 Z"),
            Fill = BrushFrom(fillHex),
            Stroke = BrushFrom("#4E3427"),
            StrokeThickness = 2,
            Stretch = Stretch.Fill,
            Width = width,
            Height = height
        };
        Canvas.SetLeft(pot, x);
        Canvas.SetTop(pot, y);
        canvas.Children.Add(pot);
    }

    private void AddDolmenStructure(double x, double y, double scale = 1.0)
    {
        var scene = new Canvas
        {
            Width = 166 * scale,
            Height = 112 * scale,
            IsHitTestVisible = false,
            Opacity = 0.95,
            RenderTransformOrigin = new Point(0, 0),
            RenderTransform = new ScaleTransform(scale, scale)
        };

        var leftStone = new Polygon
        {
            Points = new PointCollection { new(28, 104), new(34, 42), new(66, 38), new(72, 104) },
            Fill = BrushFrom("#716C67"),
            Stroke = BrushFrom("#383633"),
            StrokeThickness = 3
        };
        scene.Children.Add(leftStone);

        var rightStone = new Polygon
        {
            Points = new PointCollection { new(94, 104), new(100, 38), new(132, 42), new(139, 104) },
            Fill = BrushFrom("#77716B"),
            Stroke = BrushFrom("#383633"),
            StrokeThickness = 3
        };
        scene.Children.Add(rightStone);

        var capStone = new Polygon
        {
            Points = new PointCollection { new(13, 43), new(28, 15), new(139, 9), new(156, 35), new(138, 52), new(31, 55) },
            Fill = BrushFrom("#85807A"),
            Stroke = BrushFrom("#383633"),
            StrokeThickness = 4
        };
        scene.Children.Add(capStone);

        AddLine(scene, 33, 29, 128, 22, "#A7A19A", 2);
        AddLine(scene, 48, 47, 115, 42, "#5F5B56", 1.5);

        Canvas.SetLeft(scene, x);
        Canvas.SetTop(scene, y);
        Panel.SetZIndex(scene, -11);
        RaceCanvas.Children.Add(scene);
    }

    private void PopulateBronzeAgeArtifactCards()
    {
        foreach (var border in RaceCanvas.Children.OfType<Border>())
        {
            if (border.Child is not Grid grid)
            {
                continue;
            }

            var title = grid.Children
                .OfType<StackPanel>()
                .SelectMany(panel => panel.Children.OfType<TextBlock>())
                .FirstOrDefault(text => text.Text.StartsWith("청동기 · ", StringComparison.Ordinal));

            if (title == null)
            {
                continue;
            }

            border.BorderBrush = BrushFrom("#8A633C");
            var iconCanvas = grid.Children.OfType<Canvas>().FirstOrDefault();
            if (iconCanvas == null || iconCanvas.Children.Count > 0)
            {
                continue;
            }

            string name = title.Text["청동기 · ".Length..];
            PopulateBronzeArtifactIcon(iconCanvas, name);
        }
    }

    private static void PopulateBronzeArtifactIcon(Canvas canvas, string name)
    {
        Brush outline = BrushFrom("#4A3427");

        switch (name)
        {
            case "반달 돌칼":
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M5,22 C13,8 34,5 42,18 C35,38 15,47 5,22 Z"),
                    Fill = BrushFrom("#8D8372"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                });
                AddLine(canvas, 12, 22, 35, 15, "#C8BDA9", 1.7);
                AddBronzeIconHole(canvas, 15, 29);
                AddBronzeIconHole(canvas, 27, 25);
                break;

            case "민무늬 토기":
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M12,5 L35,5 C33,13 34,18 39,24 C44,32 42,45 35,51 C28,57 18,57 11,51 C4,45 2,32 7,24 C12,18 14,13 12,5 Z"),
                    Fill = BrushFrom("#B8734B"),
                    Stroke = outline,
                    StrokeThickness = 2.2
                });
                AddLine(canvas, 13, 11, 34, 11, "#D99A69", 1.4);
                break;

            case "비파형 동검":
                canvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M23,3 L31,16 L28,38 L25,47 L21,47 L18,38 L15,16 Z"),
                    Fill = BrushFrom("#B88743"),
                    Stroke = outline,
                    StrokeThickness = 2
                });
                AddLine(canvas, 23, 7, 23, 43, "#E3BE76", 1.5);
                var guard = new Rectangle
                {
                    Width = 25,
                    Height = 5,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = BrushFrom("#8D6333"),
                    Stroke = outline,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(guard, 10.5);
                Canvas.SetTop(guard, 44);
                canvas.Children.Add(guard);
                AddLine(canvas, 23, 48, 23, 55, "#6F4A2D", 4);
                break;

            case "고인돌":
                var left = new Polygon
                {
                    Points = new PointCollection { new(7, 48), new(11, 23), new(20, 20), new(23, 48) },
                    Fill = BrushFrom("#77716B"),
                    Stroke = outline,
                    StrokeThickness = 1.7
                };
                canvas.Children.Add(left);
                var right = new Polygon
                {
                    Points = new PointCollection { new(29, 48), new(32, 20), new(40, 23), new(43, 48) },
                    Fill = BrushFrom("#77716B"),
                    Stroke = outline,
                    StrokeThickness = 1.7
                };
                canvas.Children.Add(right);
                var cap = new Polygon
                {
                    Points = new PointCollection { new(3, 24), new(8, 10), new(39, 7), new(45, 19), new(40, 27), new(8, 28) },
                    Fill = BrushFrom("#8C8781"),
                    Stroke = outline,
                    StrokeThickness = 2
                };
                canvas.Children.Add(cap);
                break;
        }
    }

    private static void AddBronzeIconHole(Canvas canvas, double x, double y)
    {
        var hole = new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = BrushFrom("#4A3427"),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(hole, x);
        Canvas.SetTop(hole, y);
        canvas.Children.Add(hole);
    }

    private void RebuildThreeEraMinimap()
    {
        var zonePanel = MinimapCanvas.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Math.Abs(panel.Width - 154) < 1 && Math.Abs(panel.Height - 620) < 1);

        if (zonePanel == null)
        {
            return;
        }

        zonePanel.Children.Clear();
        string[] icons = { "🪨", "🔥", "🌊", "🏠", "🌾", "🗿" };

        for (int i = 0; i < PrehistoryRaceThemeSpec.Zones.Count; i++)
        {
            var zone = PrehistoryRaceThemeSpec.Zones[i];
            double height = 620.0 * ((zone.EndY - zone.StartY) / PrehistoryRaceThemeSpec.TrackHeight);

            var border = new Border
            {
                Width = 154,
                Height = height,
                Background = BrushFrom(zone.BackgroundHex),
                BorderBrush = BrushFrom("#506052"),
                BorderThickness = new Thickness(0, 0, 0, i == PrehistoryRaceThemeSpec.Zones.Count - 1 ? 0 : 1),
                Padding = new Thickness(5, 2, 5, 2),
                IsHitTestVisible = false
            };

            border.Child = new TextBlock
            {
                Text = $"{icons[i]} {zone.Period} · {zone.Title}",
                Foreground = BrushFrom("#F6E8CC"),
                FontSize = 8.7,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            zonePanel.Children.Add(border);
        }
    }
}
