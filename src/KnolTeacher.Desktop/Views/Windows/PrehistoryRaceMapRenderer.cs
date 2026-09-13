using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KnolTeacher.Desktop.Models;

namespace KnolTeacher.Desktop.Views.Windows;

/// <summary>
/// Static, lightweight renderer for the long vertical prehistoric race map.
///
/// This class intentionally does not own physics. It renders the world from
/// transparent PNG/WebP-style asset slots while StudentPickerWindow remains the
/// authority for track boundaries and gameplay collision.
/// </summary>
public sealed class PrehistoryRaceMapRenderer
{
    private const string RenderTag = "prehistory-race-map-v2";
    private const double TrackWidth = 680.0;
    private readonly Canvas _canvas;

    private static readonly Dictionary<string, BitmapSource> AssetCache =
        new(StringComparer.OrdinalIgnoreCase);

    public PrehistoryRaceMapRenderer(Canvas canvas)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
    }

    public void Render()
    {
        ClearPreviousRender();
        RenderZoneGround();
        RenderPhysicsAlignedRoad();
        RenderZoneLabels();
        RenderProps();
    }

    private void ClearPreviousRender()
    {
        var previous = _canvas.Children
            .OfType<FrameworkElement>()
            .Where(element => string.Equals(element.Tag as string, RenderTag, StringComparison.Ordinal))
            .ToArray();

        foreach (var element in previous)
        {
            _canvas.Children.Remove(element);
        }
    }

    private void RenderZoneGround()
    {
        for (int i = 0; i < PrehistoryRaceThemeSpec.Zones.Count; i++)
        {
            var zone = PrehistoryRaceThemeSpec.Zones[i];
            Brush fill;

            if (i < PrehistoryRaceThemeSpec.Zones.Count - 1)
            {
                Color current = ParseColor(zone.BackgroundHex);
                Color next = ParseColor(PrehistoryRaceThemeSpec.Zones[i + 1].BackgroundHex);
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
                gradient.GradientStops.Add(new GradientStop(current, 0));
                gradient.GradientStops.Add(new GradientStop(current, 0.82));
                gradient.GradientStops.Add(new GradientStop(next, 1));
                if (gradient.CanFreeze) gradient.Freeze();
                fill = gradient;
            }
            else
            {
                var brush = new SolidColorBrush(ParseColor(zone.BackgroundHex));
                if (brush.CanFreeze) brush.Freeze();
                fill = brush;
            }

            var ground = new Rectangle
            {
                Width = TrackWidth,
                Height = zone.EndY - zone.StartY,
                Fill = fill,
                IsHitTestVisible = false,
                Tag = RenderTag
            };
            Canvas.SetLeft(ground, 0);
            Canvas.SetTop(ground, zone.StartY);
            Panel.SetZIndex(ground, -50);
            _canvas.Children.Add(ground);
        }
    }

    private void RenderPhysicsAlignedRoad()
    {
        Geometry geometry = BuildTrackGeometry();

        var roadShadow = new Path
        {
            Data = geometry,
            Fill = FrozenBrush("#6E5137"),
            Stroke = FrozenBrush("#3A2B20"),
            StrokeThickness = 12,
            Opacity = 0.94,
            IsHitTestVisible = false,
            Tag = RenderTag
        };
        Panel.SetZIndex(roadShadow, -36);
        _canvas.Children.Add(roadShadow);

        var road = new Path
        {
            Data = geometry,
            Fill = FrozenBrush("#B98B5B"),
            Stroke = FrozenBrush("#D7AF79"),
            StrokeThickness = 3,
            Opacity = 0.98,
            IsHitTestVisible = false,
            Tag = RenderTag
        };
        Panel.SetZIndex(road, -35);
        _canvas.Children.Add(road);
    }

    private static Geometry BuildTrackGeometry()
    {
        const double sampleStep = 36.0;
        var geometry = new StreamGeometry();

        using (StreamGeometryContext context = geometry.Open())
        {
            StudentPickerWindow.GetTrackBoundaries(0, out double firstLeft, out _);
            context.BeginFigure(new Point(firstLeft, 0), true, true);

            for (double y = sampleStep; y <= PrehistoryRaceThemeSpec.TrackHeight; y += sampleStep)
            {
                double sampleY = Math.Min(y, PrehistoryRaceThemeSpec.TrackHeight);
                StudentPickerWindow.GetTrackBoundaries(sampleY, out double left, out _);
                context.LineTo(new Point(left, sampleY), true, false);
            }

            for (double y = PrehistoryRaceThemeSpec.TrackHeight; y >= 0; y -= sampleStep)
            {
                StudentPickerWindow.GetTrackBoundaries(y, out _, out double right);
                context.LineTo(new Point(right, y), true, false);
            }
        }

        geometry.Freeze();
        return geometry;
    }

    private void RenderZoneLabels()
    {
        for (int i = 0; i < PrehistoryRaceThemeSpec.Zones.Count; i++)
        {
            var zone = PrehistoryRaceThemeSpec.Zones[i];

            var period = new TextBlock
            {
                Text = zone.Period,
                Foreground = FrozenBrush("#FFF0D4"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Opacity = 0.72,
                IsHitTestVisible = false,
                Tag = RenderTag
            };
            Canvas.SetLeft(period, 14);
            Canvas.SetTop(period, zone.StartY + (i == 0 ? 190 : 22));
            Panel.SetZIndex(period, -8);
            _canvas.Children.Add(period);

            var title = new TextBlock
            {
                Text = zone.Title,
                Foreground = FrozenBrush("#EAD9BD"),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.68,
                IsHitTestVisible = false,
                Tag = RenderTag
            };
            Canvas.SetLeft(title, 15);
            Canvas.SetTop(title, zone.StartY + (i == 0 ? 214 : 46));
            Panel.SetZIndex(title, -8);
            _canvas.Children.Add(title);
        }
    }

    private void RenderProps()
    {
        IEnumerable<RaceMapProp> allProps =
            PrehistoryRaceEnvironmentV2.Props.Concat(PrehistoryRaceMapV2.Props);

        foreach (RaceMapProp prop in allProps)
        {
            BitmapSource? bitmap = TryGetAsset(prop.AssetName);
            if (bitmap == null)
            {
                // Missing final art is intentionally skipped. Do not synthesize a
                // programmer-drawn vector fallback here.
                continue;
            }

            var image = new Image
            {
                Width = prop.Width,
                Height = prop.Height,
                Source = bitmap,
                Stretch = Stretch.Uniform,
                Opacity = prop.Opacity,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true,
                Tag = RenderTag
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            if (prop.FlipX)
            {
                image.RenderTransformOrigin = new Point(0.5, 0.5);
                image.RenderTransform = new ScaleTransform(-1, 1);
            }

            Canvas.SetLeft(image, prop.X);
            Canvas.SetTop(image, prop.Y);
            Panel.SetZIndex(image, prop.ZIndex);
            _canvas.Children.Add(image);

            if (prop.Role == RaceMapVisualRole.Landmark && !string.IsNullOrWhiteSpace(prop.Label))
            {
                AddLandmarkLabel(prop);
            }
        }
    }

    private void AddLandmarkLabel(RaceMapProp prop)
    {
        string prefix = string.IsNullOrWhiteSpace(prop.Period) ? string.Empty : $"{prop.Period} · ";
        var label = new TextBlock
        {
            Text = prefix + prop.Label,
            Foreground = FrozenBrush("#FFF3D7"),
            FontSize = 10.5,
            FontWeight = FontWeights.Bold,
            Opacity = 0.9,
            TextAlignment = TextAlignment.Center,
            Width = Math.Max(84, prop.Width),
            IsHitTestVisible = false,
            Tag = RenderTag
        };
        Canvas.SetLeft(label, Math.Clamp(prop.X, 4, TrackWidth - label.Width - 4));
        Canvas.SetTop(label, prop.Y + prop.Height - 4);
        Panel.SetZIndex(label, Math.Max(prop.ZIndex, -5));
        _canvas.Children.Add(label);
    }

    private static BitmapSource? TryGetAsset(string assetName)
    {
        if (AssetCache.TryGetValue(assetName, out BitmapSource? cached))
        {
            return cached;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri($"pack://application:,,,/{PrehistoryRaceMapV2.AssetRoot}{assetName}", UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            // 512 px is ample for the current projector-scale props and prevents
            // multi-megapixel source art from occupying unnecessary memory.
            bitmap.DecodePixelWidth = 512;
            bitmap.EndInit();
            if (bitmap.CanFreeze) bitmap.Freeze();
            AssetCache[assetName] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static SolidColorBrush FrozenBrush(string hex)
    {
        var brush = new SolidColorBrush(ParseColor(hex));
        if (brush.CanFreeze) brush.Freeze();
        return brush;
    }

    private static Color ParseColor(string hex) =>
        (Color)ColorConverter.ConvertFromString(hex);
}
