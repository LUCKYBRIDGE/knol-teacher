using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        RelocateNeolithicCoastWaves();
        AddBronzeAgeStructures();
        PopulateBronzeAgeArtifactCards();
        RebuildThreeEraMinimap();
    }

    private void RelocateNeolithicCoastWaves()
    {
        // 구석기/신석기 레거시 벡터 파도 선 제거
        var legacyWaves = RaceCanvas.Children
            .OfType<System.Windows.Shapes.Path>()
            .Where(path => Panel.GetZIndex(path) == -15)
            .ToArray();

        foreach (var wave in legacyWaves)
        {
            RaceCanvas.Children.Remove(wave);
        }

        // AI 생성 해안선 에셋이 이미 배치되어 있는지 확인하고 없으면 신석기 바닷가 위치에 자연스럽게 배치
        bool hasCoast = RaceCanvas.Children
            .OfType<Image>()
            .Any(img => img.Source?.ToString().Contains("cartoon_sea_shore.png", StringComparison.OrdinalIgnoreCase) == true);

        if (!hasCoast)
        {
            double coastW = 210;
            double coastH = 138;
            var coastImg = new Image
            {
                Width = coastW,
                Height = coastH,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/cartoon_sea_shore.png")),
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(coastImg, BitmapScalingMode.HighQuality);
            Canvas.SetLeft(coastImg, 465);
            Canvas.SetTop(coastImg, 2140);
            Panel.SetZIndex(coastImg, -15);
            RaceCanvas.Children.Add(coastImg);
        }
    }

    private void AddBronzeAgeStructures()
    {
        AddBronzeRiceField(24, 2500);
        AddBronzeRiceField(505, 2640, 0.88);
        AddBronzeStorageJars(514, 2820);
        AddDolmenStructure(42, 3135);
        AddDolmenStructure(482, 3340, 0.78);
    }

    private void AddBronzeRiceField(double x, double y, double scale = 1.0)
    {
        // 청동기 벼농사 논밭 (풍요로운 황금빛 벼와 논둑) - AI 생성 2D 게임 에셋
        double w = 155 * scale;
        double h = 115 * scale;
        var fieldImg = new Image
        {
            Width = w,
            Height = h,
            Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/cartoon_rice_field.png")),
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(fieldImg, BitmapScalingMode.HighQuality);
        Canvas.SetLeft(fieldImg, x);
        Canvas.SetTop(fieldImg, y);
        Panel.SetZIndex(fieldImg, -12);
        RaceCanvas.Children.Add(fieldImg);
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
        // 사용자 요구사항: 유물 아래 한글 이름 라벨 제거
        // 10종 유물은 맵 내부의 실제 충돌 장애물/구조물로 다양한 각도로 직접 배치됩니다.
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
