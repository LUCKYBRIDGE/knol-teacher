using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using KnolTeacher.Desktop.Services;
using Microsoft.Web.WebView2.Core;

namespace KnolTeacher.Desktop.Views.Windows;

/// <summary>
/// 전자칠판 및 수업용 무간섭 스마트 웹 브라우저 창.
/// - PlayReady DRM(넷플릭스 등) 및 백그라운드 미디어 지속 재생 지원.
/// - WS_EX_NOACTIVATE 보호막으로 전자칠판 터치 시 교탁 PC 마우스 납치/포커스 전환 원천 차단.
/// </summary>
public partial class ClassroomWebBrowserWindow : Window
{
    private readonly IConfigService? _configService;
    private readonly IDisplayManager? _displayManager;
    private int _currentMonitorIndex = 1;
    private bool _isShieldActive = true;
    private double _zoomFactor = 1.0;
    private bool _isInitialized = false;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
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

    public ClassroomWebBrowserWindow(IConfigService? configService = null, IDisplayManager? displayManager = null)
    {
        _configService = configService ?? (Application.Current as App)?.Services?.GetService(typeof(IConfigService)) as IConfigService;
        _displayManager = displayManager ?? (Application.Current as App)?.Services?.GetService(typeof(IDisplayManager)) as IDisplayManager;

        InitializeComponent();

        // 터치 지연 방지
        Stylus.SetIsPressAndHoldEnabled(this, false);
        Stylus.SetIsFlicksEnabled(this, false);
        Stylus.SetIsTapFeedbackEnabled(this, false);
        Stylus.SetIsTouchFeedbackEnabled(this, false);

        Loaded += async (s, e) =>
        {
            PositionToDefaultMonitor();
            await InitializeBrowserAsync();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyShieldMode(_isShieldActive);
    }

    private void ApplyShieldMode(bool enable)
    {
        try
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle == IntPtr.Zero) return;

            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            if (enable)
            {
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
            }
            else
            {
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_NOACTIVATE);
            }

            var source = HwndSource.FromHwnd(helper.Handle);
            source?.RemoveHook(WndProcNoActivateHook);
            if (enable)
            {
                source?.AddHook(WndProcNoActivateHook);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClassroomWebBrowser] ApplyShieldMode error: {ex.Message}");
        }
    }

    private IntPtr WndProcNoActivateHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_isShieldActive && msg == WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(MA_NOACTIVATE);
        }
        return IntPtr.Zero;
    }

    private async Task InitializeBrowserAsync()
    {
        if (_isInitialized) return;

        try
        {
            LoadingBar.Visibility = Visibility.Visible;
            TxtStatus.Text = "브라우저 엔진을 준비하는 중...";

            // 1. PlayReady DRM, 백그라운드 미디어 지속, 자동재생 허용 브라우저 인수 구성
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--enable-features=msPlayReady,MediaFoundationD3D11VideoCapture --disable-background-media-suspend --autoplay-policy=no-user-gesture-required --enable-gpu-rasterization"
            };

            // 2. 영속적 프로필 디렉터리 (구글, 네이버, 띵커벨 등 로그인 세션 보존)
            string configDir = _configService?.ConfigDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KnolTeacher");
            string userDataFolder = Path.Combine(configDir, "browser_profile");
            Directory.CreateDirectory(userDataFolder);

            // 3. CoreWebView2 환경 생성 및 초기화
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            await BrowserWebView.EnsureCoreWebView2Async(environment);

            // 4. 브라우저 세부 설정
            var settings = BrowserWebView.CoreWebView2.Settings;
            settings.IsStatusBarEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsBuiltInErrorPageEnabled = true;
            settings.IsSwipeNavigationEnabled = true;

            // 표준 Microsoft Edge 정식 User-Agent 설정 (DRM 및 웹 앱 호환성)
            settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0";

            // 5. 브라우저 이벤트 연결
            BrowserWebView.NavigationStarting += (s, args) =>
            {
                LoadingBar.Visibility = Visibility.Visible;
                TxtStatus.Text = $"페이지 불러오는 중: {args.Uri}";
                TxtUrl.Text = args.Uri;
            };

            BrowserWebView.NavigationCompleted += (s, args) =>
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                TxtStatus.Text = args.IsSuccess ? "완료" : "페이지를 불러올 수 없습니다.";
                UpdateNavButtons();
            };

            BrowserWebView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                // 새 창 요청 시 외부 팝업 대신 현재 창에서 바로 탐색
                args.Handled = true;
                BrowserWebView.CoreWebView2.Navigate(args.Uri);
            };

            BrowserWebView.CoreWebView2.DocumentTitleChanged += (s, args) =>
            {
                Title = $"{BrowserWebView.CoreWebView2.DocumentTitle} - 스마트 교실 웹 브라우저";
            };

            _isInitialized = true;
            PanelFallback.Visibility = Visibility.Collapsed;
            BrowserWebView.Visibility = Visibility.Visible;

            // 첫 페이지 로드
            NavigateTo("https://www.naver.com");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClassroomWebBrowser] 초기화 실패: {ex.Message}");
            LoadingBar.Visibility = Visibility.Collapsed;
            TxtStatus.Text = "WebView2 초기화 실패";
            PanelFallback.Visibility = Visibility.Visible;
            BrowserWebView.Visibility = Visibility.Collapsed;
        }
    }

    public void NavigateTo(string urlOrQuery)
    {
        if (string.IsNullOrWhiteSpace(urlOrQuery)) return;

        string targetUrl;
        string trimmed = urlOrQuery.Trim();

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            targetUrl = trimmed;
        }
        else if (trimmed.Contains('.') && !trimmed.Contains(' '))
        {
            targetUrl = "https://" + trimmed;
        }
        else
        {
            // 검색어 입력 시 네이버/구글 검색
            targetUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(trimmed)}";
        }

        TxtUrl.Text = targetUrl;
        if (_isInitialized && BrowserWebView.CoreWebView2 != null)
        {
            BrowserWebView.CoreWebView2.Navigate(targetUrl);
        }
    }

    private void UpdateNavButtons()
    {
        if (_isInitialized && BrowserWebView.CoreWebView2 != null)
        {
            BtnBack.IsEnabled = BrowserWebView.CanGoBack;
            BtnForward.IsEnabled = BrowserWebView.CanGoForward;
        }
    }

    #region Navigation Controls

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (BrowserWebView.CanGoBack) BrowserWebView.GoBack();
    }

    private void BtnForward_Click(object sender, RoutedEventArgs e)
    {
        if (BrowserWebView.CanGoForward) BrowserWebView.GoForward();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        BrowserWebView.Reload();
    }

    private void BtnHome_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo("https://www.naver.com");
    }

    private void BtnGo_Click(object sender, RoutedEventArgs e)
    {
        NavigateTo(TxtUrl.Text);
    }

    private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateTo(TxtUrl.Text);
            e.Handled = true;
        }
    }

    private void QuickLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string url)
        {
            NavigateTo(url);
        }
    }

    #endregion

    #region Zoom & Shield Controls

    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Min(3.0, _zoomFactor + 0.1);
        ApplyZoom();
    }

    private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (_isInitialized && BrowserWebView.CoreWebView2 != null)
        {
            BrowserWebView.ZoomFactor = _zoomFactor;
        }
        if (TxtZoomLevel != null)
        {
            TxtZoomLevel.Text = $"{(_zoomFactor * 100):0}%";
        }
    }

    private void ToggleShieldMode_Checked(object sender, RoutedEventArgs e)
    {
        _isShieldActive = true;
        ApplyShieldMode(true);
        if (ToggleShieldMode != null)
        {
            ToggleShieldMode.Content = "🛡️ 마우스 보호 ON";
            ToggleShieldMode.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#059669"));
            ToggleShieldMode.Foreground = System.Windows.Media.Brushes.White;
            ToggleShieldMode.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#34D399"));
        }
        if (TxtShieldStatus != null)
        {
            TxtShieldStatus.Text = "🛡️ 교탁 PC 마우스 보호 중 (비간섭 모드)";
            TxtShieldStatus.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
        }
    }

    private void ToggleShieldMode_Unchecked(object sender, RoutedEventArgs e)
    {
        _isShieldActive = false;
        ApplyShieldMode(false);
        if (ToggleShieldMode != null)
        {
            ToggleShieldMode.Content = "⚠️ 보호 OFF (일반)";
            ToggleShieldMode.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#334155"));
            ToggleShieldMode.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8"));
            ToggleShieldMode.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748B"));
        }
        if (TxtShieldStatus != null)
        {
            TxtShieldStatus.Text = "⚠️ 보호 모드 해제됨 (일반 터치 입력 시 포커스 이동)";
            TxtShieldStatus.Foreground = System.Windows.Media.Brushes.LightGray;
        }
    }

    #endregion

    #region Monitor & Window Actions

    public void PositionToDefaultMonitor()
    {
        if (_displayManager != null)
        {
            _currentMonitorIndex = _displayManager.RecommendedStudentMonitorIndex;
            _displayManager.MoveToStudentMonitor(this, maximize: false);
            UpdateMonitorButtonText();
        }
    }

    public void ToggleMonitor()
    {
        if (_displayManager == null || _displayManager.ScreenCount < 2) return;
        _currentMonitorIndex = _currentMonitorIndex == 1 ? 0 : 1;
        _displayManager.MoveWindowToScreen(this, _currentMonitorIndex, maximize: false);
        UpdateMonitorButtonText();
    }

    private void UpdateMonitorButtonText()
    {
        if (BtnSwitchMonitor != null)
        {
            BtnSwitchMonitor.Content = _currentMonitorIndex == 1 ? "📺 모니터 2 (학생용)" : "💻 모니터 1 (메인)";
        }
    }

    private void BtnSwitchMonitor_Click(object sender, RoutedEventArgs e)
    {
        ToggleMonitor();
    }

    private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnOpenSystemBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string url = string.IsNullOrWhiteSpace(TxtUrl.Text) ? "https://www.naver.com" : TxtUrl.Text;
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }

    #endregion
}
