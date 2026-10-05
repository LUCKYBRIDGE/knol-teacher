using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KnolTeacher.Desktop.Services;

namespace KnolTeacher.Desktop.Views.Windows;

/// <summary>
/// 모니터 2(전자칠판)의 파워포인트 슬라이드 쇼를 모니터 1(교탁 PC)에서 원격 제어하는 플로팅 스마트 리모컨.
/// WS_EX_NOACTIVATE가 적용되어 교탁에서 나이스나 웹 서핑 등 다른 작업을 진행 중일 때도 포커스를 빼앗지 않습니다.
/// </summary>
public partial class PptRemoteControllerWindow : Window
{
    private static PptRemoteControllerWindow? _instance;
    private readonly DispatcherTimer _pollTimer;
    private int _stopwatchSeconds = 0;
    private bool _hasCustomPosition = false;

    public static PptRemoteControllerWindow Instance => _instance ??= new PptRemoteControllerWindow();

    public PptRemoteControllerWindow()
    {
        InitializeComponent();

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _pollTimer.Tick += PollTimer_Tick;

        Loaded += PptRemoteControllerWindow_Loaded;
        Closing += (s, e) =>
        {
            e.Cancel = true;
            CloseRemote();
        };
        PreviewKeyDown += PptRemoteControllerWindow_PreviewKeyDown;
    }

    private void PptRemoteControllerWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Right:
            case Key.Down:
            case Key.PageDown:
            case Key.Space:
                PowerPointPresentationHelper.NextSlide();
                UpdateSlideStatus();
                e.Handled = true;
                break;
            case Key.Left:
            case Key.Up:
            case Key.PageUp:
                PowerPointPresentationHelper.PreviousSlide();
                UpdateSlideStatus();
                e.Handled = true;
                break;
            case Key.B:
                PowerPointPresentationHelper.ToggleBlackScreen();
                UpdateBlackoutStatus();
                e.Handled = true;
                break;
            case Key.Home:
                PowerPointPresentationHelper.FirstSlide();
                UpdateSlideStatus();
                e.Handled = true;
                break;
            case Key.End:
                PowerPointPresentationHelper.LastSlide();
                UpdateSlideStatus();
                e.Handled = true;
                break;
        }
    }

    private void PptRemoteControllerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        EnableNoActivateStyle();
        if (!_hasCustomPosition)
        {
            PositionToMonitor1BottomRight();
        }
    }

    /// <summary>
    /// 모니터 1(교탁 기본 화면) 우측 하단에 단정하게 배치합니다.
    /// </summary>
    public void PositionToMonitor1BottomRight()
    {
        try
        {
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                var workArea = primaryScreen.WorkingArea;
                // 우측 30px, 하단 30px 여백 (작업표시줄 위)
                Left = workArea.Right - ActualWidth - 30;
                Top = workArea.Bottom - ActualHeight - 30;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PptRemoteControllerWindow] PositionToMonitor1BottomRight failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 지정된 프레젠테이션 정보를 바탕으로 리모컨을 표시합니다.
    /// </summary>
    public void OpenForPresentation(string? title)
    {
        TxtPresentationTitle.Text = string.IsNullOrWhiteSpace(title) ? "프레젠테이션" : title;
        _stopwatchSeconds = 0;
        UpdateStopwatchDisplay();
        UpdateSlideStatus();

        Show();
        if (!_hasCustomPosition)
        {
            PositionToMonitor1BottomRight();
        }
        _pollTimer.Start();
    }

    /// <summary>
    /// 리모컨을 안전하게 숨깁니다.
    /// </summary>
    public void CloseRemote()
    {
        _pollTimer.Stop();
        Hide();
    }

    private void PollTimer_Tick(object? sender, EventArgs e)
    {
        // 파워포인트 슬라이드 쇼가 종료되었으면 리모컨도 자동으로 닫힘
        if (!PowerPointPresentationHelper.IsSlideShowRunning)
        {
            CloseRemote();
            return;
        }

        _stopwatchSeconds++;
        UpdateStopwatchDisplay();
        UpdateSlideStatus();
        UpdateBlackoutStatus();
    }

    private void UpdateSlideStatus()
    {
        var info = PowerPointPresentationHelper.GetSlideInfo();
        if (info.HasValue)
        {
            string counterText = $"{info.Value.Current} / {info.Value.Total}";
            TxtSlideCounter.Text = counterText;
            TxtCompactSlideCounter.Text = $"{info.Value.Current}/{info.Value.Total}";
        }
    }

    private void UpdateStopwatchDisplay()
    {
        int minutes = _stopwatchSeconds / 60;
        int seconds = _stopwatchSeconds % 60;
        TxtStopwatch.Text = $"⏱️ {minutes:D2}:{seconds:D2}";
    }

    private void UpdateBlackoutStatus()
    {
        bool isBlack = PowerPointPresentationHelper.IsBlackScreen;
        if (isBlack)
        {
            BtnToggleBlackout.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            BtnToggleBlackout.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            BtnToggleBlackout.Content = "💡 화면 가림 해제 (복원)";
        }
        else
        {
            BtnToggleBlackout.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            BtnToggleBlackout.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            BtnToggleBlackout.Content = "⬛ 화면 가리기 (선생님 주목)";
        }
    }

    #region Window Drag & Navigation Controls

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && !IsInsideClickable(e.OriginalSource as DependencyObject))
        {
            _hasCustomPosition = true;
            DragMove();
        }
    }

    private static bool IsInsideClickable(DependencyObject? element)
    {
        while (element != null)
        {
            if (element is System.Windows.Controls.Primitives.ButtonBase) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void BtnPrevSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.PreviousSlide();
        UpdateSlideStatus();
    }

    private void BtnNextSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.NextSlide();
        UpdateSlideStatus();
    }

    private void BtnFirstSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.FirstSlide();
        UpdateSlideStatus();
    }

    private void BtnLastSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.LastSlide();
        UpdateSlideStatus();
    }

    private void BtnToggleBlackout_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.ToggleBlackScreen();
        UpdateBlackoutStatus();
    }

    private void BtnEndSlideShow_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.CloseSlideShow();
        CloseRemote();
    }

    private void BtnCloseRemote_Click(object sender, RoutedEventArgs e)
    {
        CloseRemote();
    }

    private void BtnCompact_Click(object sender, RoutedEventArgs e)
    {
        PanelExpanded.Visibility = Visibility.Collapsed;
        PanelCompact.Visibility = Visibility.Visible;
    }

    private void BtnExpand_Click(object sender, RoutedEventArgs e)
    {
        PanelCompact.Visibility = Visibility.Collapsed;
        PanelExpanded.Visibility = Visibility.Visible;
    }

    private void BdTimerReset_Click(object sender, MouseButtonEventArgs e)
    {
        _stopwatchSeconds = 0;
        UpdateStopwatchDisplay();
    }

    #endregion

    #region Win32 NoActivate Shield (교탁 PC 마우스 납치 및 키보드 포커스 스틸 원천 차단)

    private void EnableNoActivateStyle()
    {
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);

                var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
                source?.AddHook(WndProcNoActivateHook);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PptRemoteControllerWindow] EnableNoActivateStyle failed: {ex.Message}");
        }
    }

    private IntPtr WndProcNoActivateHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 3;

        if (msg == WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(MA_NOACTIVATE);
        }
        return IntPtr.Zero;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static int GetWindowLong(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? unchecked((int)GetWindowLongPtr64(hWnd, nIndex).ToInt64())
            : GetWindowLong32(hWnd, nIndex);
    }

    private static void SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr64(hWnd, nIndex, new IntPtr(dwNewLong));
        else
            SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    #endregion
}
