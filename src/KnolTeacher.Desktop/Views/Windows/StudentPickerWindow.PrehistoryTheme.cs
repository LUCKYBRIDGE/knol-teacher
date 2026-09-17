using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        // 1. Natural AI-generated River Stream crossing
        double riverW = TrackWidth;
        double riverH = 210;
        double riverY = 1720;
        var riverImg = new Image
        {
            Width = riverW,
            Height = riverH,
            Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/cartoon_river_stream.png")),
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(riverImg, BitmapScalingMode.HighQuality);
        Canvas.SetLeft(riverImg, 0);
        Canvas.SetTop(riverImg, riverY);
        Panel.SetZIndex(riverImg, -25);
        RaceCanvas.Children.Add(riverImg);

        // 2. Realistic Stepping Stones (신석기 강가 징검다리)
        var stones = new (double X, double Y, double W, double H)[]
        {
            (145, 1810, 68, 52),
            (225, 1835, 62, 48),
            (305, 1805, 74, 56),
            (395, 1840, 64, 50),
            (475, 1812, 70, 54)
        };
        foreach (var item in stones)
        {
            var stoneImg = new Image
            {
                Width = item.W,
                Height = item.H,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/river_stone.png")),
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(stoneImg, BitmapScalingMode.HighQuality);
            Canvas.SetLeft(stoneImg, item.X);
            Canvas.SetTop(stoneImg, item.Y);
            Panel.SetZIndex(stoneImg, -23);
            RaceCanvas.Children.Add(stoneImg);
        }

        // 3. Water splashes around stepping stones
        var splashes = new (double X, double Y, double W, double H)[]
        {
            (185, 1835, 42, 22),
            (350, 1828, 48, 24),
            (440, 1838, 44, 22)
        };
        foreach (var sp in splashes)
        {
            var splashImg = new Image
            {
                Width = sp.W,
                Height = sp.H,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/race/cartoon_water_splash.png")),
                Stretch = Stretch.Uniform,
                Opacity = 0.75,
                IsHitTestVisible = false
            };
            RenderOptions.SetBitmapScalingMode(splashImg, BitmapScalingMode.HighQuality);
            Canvas.SetLeft(splashImg, sp.X);
            Canvas.SetTop(splashImg, sp.Y);
            Panel.SetZIndex(splashImg, -22);
            RaceCanvas.Children.Add(splashImg);
        }

        // 4. Natural shallow water immersion wash (자연스러운 얕은 여울물 침수 연출)
        // 상하 가장자리는 0 투명도로 부드럽게 감쇄되어 레이서가 물에 들어오고 나갈 때 자연스럽게 반투명 물빛에 잠김
        var waterWash = new Rectangle
        {
            Width = riverW,
            Height = 120,
            Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0, 56, 189, 248), 0.0),
                    new GradientStop(Color.FromArgb(42, 56, 189, 248), 0.22),
                    new GradientStop(Color.FromArgb(58, 14, 165, 233), 0.5),
                    new GradientStop(Color.FromArgb(42, 56, 189, 248), 0.78),
                    new GradientStop(Color.FromArgb(0, 56, 189, 248), 1.0)
                }
            },
            IsHitTestVisible = false
        };
        Canvas.SetLeft(waterWash, 0);
        Canvas.SetTop(waterWash, riverY + 45);
        Panel.SetZIndex(waterWash, 5);
        RaceCanvas.Children.Add(waterWash);
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
