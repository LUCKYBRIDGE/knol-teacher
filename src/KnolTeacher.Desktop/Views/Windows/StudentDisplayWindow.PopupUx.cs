using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KnolTeacher.Desktop.Services;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class StudentDisplayWindow
{
    private static readonly bool V309WidgetUxRegistered = RegisterV309WidgetUx();
    private bool _v309WidgetUxApplied;
    private PopupLaunchPreferences? _v309PopupLaunchPreferences;

    private static bool RegisterV309WidgetUx()
    {
        EventManager.RegisterClassHandler(
            typeof(StudentDisplayWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(StudentDisplayWindowV309_Loaded),
            handledEventsToo: true);

        // Existing widget-state code updates button colors after open/close. Refresh our
        // popup-vs-widget visual contract after routed button clicks so the distinction
        // remains visible for the whole session, not only immediately after startup.
        EventManager.RegisterClassHandler(
            typeof(StudentDisplayWindow),
            Button.ClickEvent,
            new RoutedEventHandler(StudentDisplayWindowV309_ButtonClick),
            handledEventsToo: true);

        return true;
    }

    private static void StudentDisplayWindowV309_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not StudentDisplayWindow window) return;

        window.InitializeV309WidgetUx();
        _ = window.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(window.RefreshV309LauncherVisuals));
    }

    private static void StudentDisplayWindowV309_ButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not StudentDisplayWindow window || !window._v309WidgetUxApplied) return;

        _ = window.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(window.RefreshV309LauncherVisuals));
    }

    private void InitializeV309WidgetUx()
    {
        if (_v309WidgetUxApplied) return;
        _v309WidgetUxApplied = true;

        _v309PopupLaunchPreferences = new PopupLaunchPreferences(_configService.ConfigDir);

        // Connect Pinball (뽑기 레이스) to StudentPickerWindow with dual-monitor support
        BtnToolPinball.Click -= DockToolBtn_Click;
        BtnToolPinball.Click -= BtnPinballWindow_Click;
        BtnToolPinball.Click += BtnPinballWindow_Click;
        BtnToolPinball.MouseRightButtonUp -= BtnPinballWindow_MouseRightButtonUp;
        BtnToolPinball.MouseRightButtonUp += BtnPinballWindow_MouseRightButtonUp;
        BtnToolPinball.ToolTip = "뽑기 레이스 · 별도 레이스 창에서 32종 동물 아바타로 발표자를 뽑습니다 (우클릭: 다른 모니터로 열기)";

        RefreshV309LauncherVisuals();
    }

    private void RefreshV309LauncherVisuals()
    {
        if (!_v309WidgetUxApplied) return;
        if (FindResource("BoardNavButtonStyle") is not Style baseStyle) return;

        Brush widgetIdleBackground = BrushFrom("#1E293B");
        Brush widgetIdleBorder = BrushFrom("#334155");
        Brush widgetIdleForeground = BrushFrom("#94A3B8");
        Brush widgetActiveBackground = BrushFrom("#0284C7");
        Brush widgetActiveBorder = BrushFrom("#38BDF8");

        // These are the actual in-canvas widgets registered by WidgetRegistry.
        Button[] widgetButtons =
        {
            BtnToolTimer,
            BtnToolPicker,
            BtnToolDice,
            BtnToolWheel,
            BtnToolScore,
            BtnToolDrawing,
            BtnToolTimetable,
            BtnToolMeal,
            BtnToolMemo,
            BtnToolChecklist,
            BtnToolQr,
            BtnToolWeather,
            BtnToolDDay
        };

        foreach (Button button in widgetButtons)
        {
            bool active = button.Tag is string tag && FindWidget(tag) != null;
            button.Style = baseStyle;
            button.Background = active ? widgetActiveBackground : widgetIdleBackground;
            button.BorderBrush = active ? widgetActiveBorder : widgetIdleBorder;
            button.Foreground = active ? Brushes.White : widgetIdleForeground;
            button.BorderThickness = new Thickness(active ? 1.5 : 1.0);
            button.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;

            string existing = button.ToolTip?.ToString() ?? string.Empty;
            if (!existing.StartsWith("위젯 ·", StringComparison.Ordinal))
            {
                button.ToolTip = string.IsNullOrWhiteSpace(existing)
                    ? "위젯 · 놀보드 안에 열립니다."
                    : $"위젯 · {existing}";
            }
        }

        bool pinballActive = _pinballWindow != null && _pinballWindow.IsVisible;
        BtnToolPinball.Style = baseStyle;
        BtnToolPinball.Background = pinballActive ? widgetActiveBackground : widgetIdleBackground;
        BtnToolPinball.BorderBrush = pinballActive ? widgetActiveBorder : widgetIdleBorder;
        BtnToolPinball.Foreground = pinballActive ? Brushes.White : widgetIdleForeground;
        BtnToolPinball.BorderThickness = new Thickness(pinballActive ? 1.5 : 1.0);
        BtnToolPinball.FontWeight = pinballActive ? FontWeights.Bold : FontWeights.SemiBold;
        BtnToolPinball.ToolTip = "뽑기 레이스 · 별도 레이스 창에서 32종 동물 아바타로 발표자를 뽑습니다 (우클릭: 다른 모니터로 열기)";

        CbAddWidget.Background = BrushFrom("#1E293B");
        CbAddWidget.Foreground = widgetIdleForeground;
        CbAddWidget.BorderBrush = widgetIdleBorder;
        CbAddWidget.ToolTip = "이 목록의 도구는 별도 팝업이 아니라 놀보드 안의 위젯으로 열립니다.";
    }

    private void BtnPinballWindow_Click(object sender, RoutedEventArgs e)
    {
        if (_pinballWindow != null && _pinballWindow.IsVisible)
        {
            _pinballWindow.Hide();
        }
        else
        {
            OpenPinballOnMonitor(_currentMonitorIndex);
        }
        e.Handled = true;
        RefreshV309LauncherVisuals();
    }

    private void BtnPinballWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _v309PopupLaunchPreferences ??= new PopupLaunchPreferences(_configService.ConfigDir);
        if (!_v309PopupLaunchPreferences.RightClickOpensOnSecondMonitor)
        {
            return;
        }

        int targetMonitor = _displayManager?.IsDualMonitor == true
            ? (_currentMonitorIndex == 0 ? 1 : 0)
            : 0;

        if (_pinballWindow != null && _pinballWindow.IsVisible && _pinballWindow.CurrentMonitorIndex == targetMonitor)
        {
            _pinballWindow.Hide();
        }
        else
        {
            OpenPinballOnMonitor(targetMonitor);
        }
        e.Handled = true;
        RefreshV309LauncherVisuals();
    }

    private void OpenPinballOnMonitor(int monitorIndex)
    {
        var app = Application.Current as App;
        var resolved = app?.Services?.GetService(typeof(StudentPickerWindow)) as StudentPickerWindow;
        if (resolved != null)
        {
            _pinballWindow = resolved;
        }
        else if (_pinballWindow == null)
        {
            _pinballWindow = new StudentPickerWindow(_studentService, _soundService, _displayManager);
        }

        if (_pinballWindow == null) return;

        _pinballWindow.IsVisibleChanged -= PinballWindow_IsVisibleChanged;
        _pinballWindow.IsVisibleChanged += PinballWindow_IsVisibleChanged;

        _pinballWindow.ShowOnMonitor(monitorIndex);
        RefreshV309LauncherVisuals();
    }

    private void PinballWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _ = Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(RefreshV309LauncherVisuals));
    }

    private static SolidColorBrush BrushFrom(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex));
}
