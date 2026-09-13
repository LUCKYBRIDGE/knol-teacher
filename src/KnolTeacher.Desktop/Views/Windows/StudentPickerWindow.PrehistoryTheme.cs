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
    private bool _prehistoryMapV2Applied;
    private PrehistoryRaceMapRenderer? _prehistoryMapRenderer;

    static StudentPickerWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(StudentPickerWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPrehistoryMapV2Loaded));
    }

    private static void OnPrehistoryMapV2Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not StudentPickerWindow window)
        {
            return;
        }

        window.ApplyPrehistoryMapV2();

        // The instance Loaded handler still owns the legacy lightweight gameplay
        // setup. Reconcile it after Loaded so Map v2 can keep the proven physics
        // loop while replacing only the relic placements that are era-sensitive.
        _ = window.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(window.NormalizeRaceMapV2Gameplay));
    }

    private void ApplyPrehistoryMapV2()
    {
        if (_prehistoryMapV2Applied || RaceCanvas == null || MinimapCanvas == null)
        {
            return;
        }

        _prehistoryMapV2Applied = true;
        Title = "뽑기 레이스 · 구석기 → 신석기 → 청동기";

        SuppressLegacyRaceScenery();

        _prehistoryMapRenderer = new PrehistoryRaceMapRenderer(RaceCanvas);
        _prehistoryMapRenderer.Render();
        RebuildPrehistoryMinimapV2();
    }

    private void SuppressLegacyRaceScenery()
    {
        RaceViewport.Background = new SolidColorBrush(Color.FromRgb(31, 35, 30));
        RaceCanvas.Background = Brushes.Transparent;

        foreach (Rectangle rectangle in RaceCanvas.Children.OfType<Rectangle>())
        {
            double top = Canvas.GetTop(rectangle);

            bool isLegacyFullBackground = rectangle.Width >= 679 && rectangle.Height >= 3499;
            bool isLegacyCenterLine = rectangle.Width <= 3 && rectangle.Height >= 3000;
            bool isLegacyFinishBasin = rectangle.Width >= 150 && rectangle.Height >= 130 && top >= 3300;

            if (isLegacyFullBackground || isLegacyCenterLine || isLegacyFinishBasin)
            {
                rectangle.Visibility = Visibility.Collapsed;
            }
        }

        foreach (TextBlock guide in RaceCanvas.Children.OfType<TextBlock>())
        {
            if (guide.Text.Contains("m —", StringComparison.Ordinal))
            {
                guide.Visibility = Visibility.Collapsed;
            }
        }

        foreach (Border border in RaceCanvas.Children.OfType<Border>())
        {
            double top = Canvas.GetTop(border);
            if (border.Width >= 150 && border.Height >= 130 && top >= 3300)
            {
                border.Visibility = Visibility.Collapsed;
            }
        }

        foreach (Image image in RaceCanvas.Children.OfType<Image>())
        {
            image.IsHitTestVisible = false;
        }
    }

    private void RebuildPrehistoryMinimapV2()
    {
        var zonePanel = MinimapCanvas.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Math.Abs(panel.Width - 154) < 1 && Math.Abs(panel.Height - 620) < 1);

        if (zonePanel == null)
        {
            return;
        }

        zonePanel.Children.Clear();

        foreach (PrehistoryRaceZone zone in PrehistoryRaceThemeSpec.Zones)
        {
            double height = 620.0 * ((zone.EndY - zone.StartY) / PrehistoryRaceThemeSpec.TrackHeight);
            var row = new Border
            {
                Width = 154,
                Height = height,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(zone.BackgroundHex)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(7, 5, 7, 3),
                IsHitTestVisible = false
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = zone.Period,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 240, 212)),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            });
            stack.Children.Add(new TextBlock
            {
                Text = zone.Title,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 207, 184)),
                FontSize = 8.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
            row.Child = stack;
            zonePanel.Children.Add(row);
        }
    }

    private void NormalizeRaceMapV2Gameplay()
    {
        RebuildMapV2InteractiveRelics();

        // Preserve the existing two-layer static-obstacle response. RaceRail
        // segments provide normal reflection and downhill sliding along the rock
        // edges; the composite island watchdogs in GameTimer_Tick are a cheap
        // anti-tunneling fallback for extreme velocity. Map v2 only binds their
        // footprint to the PNG visual contract; it does not replace this stable
        // lightweight physics with WPF hit-testing or pixel collision.
        foreach (RaceBumper bumper in _bumpers)
        {
            bumper.Visual.IsHitTestVisible = false;
        }

        foreach (RotatingLog log in _rotatingLogs)
        {
            log.Visual.IsHitTestVisible = false;
        }

        foreach (BreakablePottery pottery in _potteries)
        {
            pottery.Visual.IsHitTestVisible = false;
        }

        foreach (PopOutSquirrel squirrel in _squirrels)
        {
            squirrel.BranchVisual.IsHitTestVisible = false;
            squirrel.SquirrelVisual.IsHitTestVisible = false;

            // Projectiles are created after Loaded. WPF hit-testing remains off;
            // the existing circle collision in GameTimer_Tick is authoritative.
            squirrel.OnThrowProjectile = projectile =>
            {
                projectile.Visual.IsHitTestVisible = false;
                _projectiles.Add(projectile);
                RaceCanvas.Children.Add(projectile.Visual);
            };
        }

        foreach (ThrownProjectile projectile in _projectiles)
        {
            projectile.Visual.IsHitTestVisible = false;
        }
    }

    private void RebuildMapV2InteractiveRelics()
    {
        foreach (RaceBumper bumper in _bumpers)
        {
            RaceCanvas.Children.Remove(bumper.Visual);
        }
        _bumpers.Clear();

        foreach (BreakablePottery pottery in _potteries)
        {
            RaceCanvas.Children.Remove(pottery.Visual);
        }
        _potteries.Clear();

        var rulesByKey = PrehistoryRaceMapV2.InteractiveRelics
            .ToDictionary(rule => rule.Key, StringComparer.Ordinal);

        foreach (InteractiveRelicPlacement placement in PrehistoryRaceMapV2.InteractiveRelicPlacements)
        {
            if (!rulesByKey.TryGetValue(placement.RuleKey, out InteractiveRelicRule? rule))
            {
                continue;
            }

            switch (rule.Interaction)
            {
                case RaceMapInteractionRole.StaticBumper
                    when rule.ColliderShape == RaceColliderShape.Circle:
                    AddBumper(
                        placement.X,
                        placement.Y,
                        placement.ColliderRadius,
                        rule.AssetName);
                    ApplyMapV2GameplayVisualLayout(_bumpers[^1].Visual, placement);
                    break;

                case RaceMapInteractionRole.Breakable
                    when rule.ColliderShape == RaceColliderShape.Circle &&
                         string.Equals(rule.BehaviorKey, "breakable-pottery", StringComparison.Ordinal):
                    // BreakablePottery intentionally keeps the established shatter,
                    // rebound and dizzy/overtake mechanic. Only its placement and
                    // display footprint are changed by Map v2.
                    AddBreakablePottery(
                        placement.X,
                        placement.Y,
                        placement.ColliderRadius);
                    ApplyMapV2GameplayVisualLayout(_potteries[^1].Visual, placement);
                    break;
            }
        }
    }

    private static void ApplyMapV2GameplayVisualLayout(
        Grid visual,
        InteractiveRelicPlacement placement)
    {
        visual.Width = placement.VisualWidth;
        visual.Height = placement.VisualHeight;
        visual.IsHitTestVisible = false;

        foreach (Image image in visual.Children.OfType<Image>())
        {
            image.Width = placement.VisualWidth;
            image.Height = placement.VisualHeight;
            image.Stretch = Stretch.Uniform;
            image.IsHitTestVisible = false;
        }

        Canvas.SetLeft(visual, placement.X - placement.VisualWidth / 2.0);
        Canvas.SetTop(visual, placement.Y - placement.VisualHeight / 2.0);
        Panel.SetZIndex(visual, placement.ZIndex);
    }
}
