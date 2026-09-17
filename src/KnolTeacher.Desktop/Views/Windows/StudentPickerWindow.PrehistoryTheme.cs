using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
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
        // 사용자 요구사항: 유물 아래 한글 이름 라벨 제거
        // 10종 유물은 맵 내부의 실제 충돌 장애물/구조물로 다양한 각도로 직접 배치되므로,
        // 트랙 외곽의 한글 이름/설명 카드는 생성하지 않습니다.
    }

    public static FrameworkElement CreateArtifactIcon(string key)
    {
        string assetFile = key switch
        {
            "chopper" => "cartoon_chipped_stone.png",
            "handaxe" => "cartoon_handaxe.png",
            "bone-needle" => "cartoon_relic_bone_needle.png",
            "polished-stone" => "cartoon_polished_stone.png",
            "comb-pottery" => "cartoon_comb_pottery.png",
            "spindle-whorl" => "cartoon_relic_spindle_whorl.png",
            "shell-mask" => "cartoon_relic_shell_mask.png",
            "half-moon-stone-knife" => "cartoon_relic_half_moon_knife.png",
            "plain-pottery" => "cartoon_relic_plain_pottery.png",
            "bronze-dagger" => "cartoon_relic_bronze_dagger.png",
            "dolmen" => "cartoon_dolmen.png",
            _ => "cartoon_pebble_bumper.png"
        };

        var img = new Image
        {
            Width = 48,
            Height = 48,
            Stretch = Stretch.Uniform,
            Source = new BitmapImage(new Uri($"pack://application:,,,/assets/race/{assetFile}")),
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
        return img;
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
        // 1. Organic Flowing River Surface (자연스러운 유선형 강줄기 지오메트리)
        var riverGeo = Geometry.Parse(
            "M -10,1720 C 160,1702 330,1736 510,1714 C 600,1704 690,1718 " +
            "L 690,1935 C 520,1952 340,1918 160,1945 C 65,1938 -10,1930 Z");

        var riverPath = new System.Windows.Shapes.Path
        {
            Data = riverGeo,
            Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(90, 49, 81, 77), 0.0),    // 젖은 모래/강변 그라데이션
                    new GradientStop(Color.FromArgb(190, 14, 165, 233), 0.12), // 맑은 여울목 수면 (#0EA5E9)
                    new GradientStop(Color.FromArgb(235, 2, 132, 199), 0.42),  // 깊은 청록빛 여울 (#0284C7)
                    new GradientStop(Color.FromArgb(245, 3, 105, 161), 0.60),  // 깊은 물길 중심 (#0369A1)
                    new GradientStop(Color.FromArgb(190, 14, 165, 233), 0.88), // 맑은 얕은 물빛
                    new GradientStop(Color.FromArgb(90, 49, 81, 77), 1.0)     // 하류 젖은 모래 그라데이션
                }
            },
            IsHitTestVisible = false
        };
        Panel.SetZIndex(riverPath, -25);
        RaceCanvas.Children.Add(riverPath);

        // 2. Flowing Water Wave Ripples (강물 수면 유속 물결선)
        var rippleCurves = new[]
        {
            "M 30,1748 C 170,1736 300,1758 460,1742 C 550,1735 650,1746",
            "M 70,1782 C 210,1768 350,1794 490,1776 C 580,1770 665,1784",
            "M 25,1822 C 185,1806 330,1832 480,1814 C 570,1808 655,1820",
            "M 65,1862 C 195,1846 340,1875 500,1856 C 590,1850 670,1865",
            "M 35,1898 C 175,1885 320,1912 480,1894 C 570,1888 645,1900"
        };
        foreach (var data in rippleCurves)
        {
            var ripple = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(data),
                Stroke = BrushFrom("#BAE6FD"),
                StrokeThickness = 2.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0.62,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(ripple, -24);
            RaceCanvas.Children.Add(ripple);
        }

        // 3. Shoreline Foam Traces (강둑 얇은 백색 물거품/포말 라인)
        var topFoam = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M 0,1720 C 160,1702 330,1736 510,1714 C 600,1704 680,1718"),
            Stroke = BrushFrom("#F0F9FF"),
            StrokeThickness = 2.4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = 0.55,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(topFoam, -24);
        RaceCanvas.Children.Add(topFoam);

        var bottomFoam = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse("M 680,1935 C 520,1952 340,1918 160,1945 C 65,1938 0,1930"),
            Stroke = BrushFrom("#F0F9FF"),
            StrokeThickness = 2.4,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = 0.55,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(bottomFoam, -24);
        RaceCanvas.Children.Add(bottomFoam);

        // 4. Realistic River Stepping Stones (신석기 강가 징검돌과 조약돌)
        var stones = new (double X, double Y, double W, double H)[]
        {
            (175, 1785, 56, 42),
            (255, 1830, 50, 36),
            (340, 1795, 60, 44),
            (425, 1835, 52, 38),
            (495, 1790, 54, 40)
        };
        foreach (var item in stones)
        {
            var stoneImg = new Image
            {
                Width = item.W,
                Height = item.H,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/river_stone.png")),
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 7,
                    Opacity = 0.45,
                    ShadowDepth = 2.5,
                    Color = (Color)ColorConverter.ConvertFromString("#0C4A6E")
                }
            };
            RenderOptions.SetBitmapScalingMode(stoneImg, BitmapScalingMode.HighQuality);
            Canvas.SetLeft(stoneImg, item.X);
            Canvas.SetTop(stoneImg, item.Y);
            Panel.SetZIndex(stoneImg, -23);
            RaceCanvas.Children.Add(stoneImg);
        }

        // River pebbles scattered along the riverbed
        var pebbles = new (double X, double Y, double R)[]
        {
            (130, 1750, 10), (220, 1770, 8), (470, 1755, 11), (540, 1775, 9),
            (150, 1905, 9), (290, 1885, 12), (390, 1895, 10), (520, 1880, 8)
        };
        foreach (var p in pebbles)
        {
            var pebble = new Ellipse
            {
                Width = p.R * 1.3,
                Height = p.R * 0.9,
                Fill = BrushFrom("#94A3B8"),
                Stroke = BrushFrom("#475569"),
                StrokeThickness = 1,
                Opacity = 0.7,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(pebble, p.X);
            Canvas.SetTop(pebble, p.Y);
            Panel.SetZIndex(pebble, -23);
            RaceCanvas.Children.Add(pebble);
        }

        // 5. Riverbank Foliage / Reeds (강변 양쪽 수변 갈대 식생)
        AddRiverbankReeds(55, 1735);
        AddRiverbankReeds(85, 1885);
        AddRiverbankReeds(590, 1740);
        AddRiverbankReeds(620, 1880);

        // 6. Natural shallow water immersion wash (레이서 수면 반사/침수 연출)
        var waterWash = new Rectangle
        {
            Width = TrackWidth,
            Height = 150,
            Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0, 56, 189, 248), 0.0),
                    new GradientStop(Color.FromArgb(28, 56, 189, 248), 0.2),
                    new GradientStop(Color.FromArgb(46, 14, 165, 233), 0.5),
                    new GradientStop(Color.FromArgb(28, 56, 189, 248), 0.8),
                    new GradientStop(Color.FromArgb(0, 56, 189, 248), 1.0)
                }
            },
            IsHitTestVisible = false
        };
        Canvas.SetLeft(waterWash, 0);
        Canvas.SetTop(waterWash, 1750);
        Panel.SetZIndex(waterWash, 5);
        RaceCanvas.Children.Add(waterWash);
    }

    private void AddRiverbankReeds(double x, double y)
    {
        var cluster = new Canvas { Width = 30, Height = 40, IsHitTestVisible = false };
        var reedColors = new[] { "#166534", "#15803D", "#22C55E", "#15803D" };
        var offsets = new (double x1, double y1, double x2, double y2)[]
        {
            (6, 36, 4, 10),
            (12, 38, 14, 6),
            (18, 36, 20, 8),
            (24, 38, 26, 12)
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            var (x1, y1, x2, y2) = offsets[i];
            cluster.Children.Add(new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = BrushFrom(reedColors[i]),
                StrokeThickness = 2.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
            var tip = new Ellipse
            {
                Width = 4, Height = 9,
                Fill = BrushFrom("#78350F")
            };
            Canvas.SetLeft(tip, x2 - 2);
            Canvas.SetTop(tip, y2 - 4);
            cluster.Children.Add(tip);
        }
        Canvas.SetLeft(cluster, x);
        Canvas.SetTop(cluster, y);
        Panel.SetZIndex(cluster, -18);
        RaceCanvas.Children.Add(cluster);
    }

    private void AddNeolithicHut(double x, double y, double scale = 1.0)
    {
        double w = 142 * scale;
        double h = 134 * scale;
        var hutImg = new Image
        {
            Width = w,
            Height = h,
            Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/cartoon_neolithic_hut.png")),
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(hutImg, BitmapScalingMode.HighQuality);
        Canvas.SetLeft(hutImg, x);
        Canvas.SetTop(hutImg, y);
        Panel.SetZIndex(hutImg, -12);
        RaceCanvas.Children.Add(hutImg);
    }

    private void AddCoastWaves()
    {
        // 신석기 바닷가 정착지 (조개 채집과 바닷가 풍경) - AI 생성 해변과 파도 아트
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
