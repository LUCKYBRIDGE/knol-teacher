using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using KnolTeacher.Desktop.Services;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class FloatingToolbarWindow : Window
{
    private readonly StudentDisplayWindow _studentBoard;
    private readonly ScreenDrawingOverlayWindow _screenDrawing;
    private readonly ClassroomTimerWindow _timerWindow;
    private readonly VisualizerWindow _visualizerWindow;
    private readonly StudentPickerWindow _pickerWindow;
    private readonly IQrCodeService _qrCodeService;
    private readonly IDesktopCleanerService _cleanerService;
    private readonly DigitalSignatureWindow _signatureWindow;
    private readonly NoiseTrafficLightWindow _noiseWindow;
    private readonly ClassroomSoundboardWindow _soundboardWindow;
    private readonly SmartSeatShuffleWindow _seatWindow;
    private readonly IDisplayManager? _displayManager;
    private readonly DispatcherTimer _activeTimer;
    private bool _isExpanded = false;

    public FloatingToolbarWindow(
        StudentDisplayWindow studentBoard,
        ScreenDrawingOverlayWindow screenDrawing,
        ClassroomTimerWindow timerWindow,
        VisualizerWindow visualizerWindow,
        StudentPickerWindow pickerWindow,
        IQrCodeService qrCodeService,
        IDesktopCleanerService cleanerService,
        DigitalSignatureWindow signatureWindow,
        NoiseTrafficLightWindow noiseWindow,
        ClassroomSoundboardWindow soundboardWindow,
        SmartSeatShuffleWindow seatWindow,
        IDisplayManager? displayManager = null)
    {
        _studentBoard = studentBoard;
        _screenDrawing = screenDrawing;
        _timerWindow = timerWindow;
        _visualizerWindow = visualizerWindow;
        _pickerWindow = pickerWindow;
        _qrCodeService = qrCodeService;
        _cleanerService = cleanerService;
        _signatureWindow = signatureWindow;
        _noiseWindow = noiseWindow;
        _soundboardWindow = soundboardWindow;
        _seatWindow = seatWindow;
        _displayManager = displayManager;

        InitializeComponent();

        _activeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _activeTimer.Tick += (s, e) => UpdateActiveToolIndicators();

        Loaded += (s, e) =>
        {
            PositionAtTopCenter();
            _activeTimer.Start();
            UpdateActiveToolIndicators();
        };

        IsVisibleChanged += (s, e) =>
        {
            if (IsVisible)
            {
                _activeTimer.Start();
                UpdateActiveToolIndicators();
            }
            else
            {
                _activeTimer.Stop();
            }
        };
    }

    public void PositionAtTopCenter()
    {
        UpdateLayout();
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double w = ActualWidth > 0 ? ActualWidth : 560;
        Left = Math.Max(20, (screenWidth - w) / 2);
        Top = 16;
    }

    private void EnsureWindowWithinScreen()
    {
        UpdateLayout();
        double screenW = SystemParameters.PrimaryScreenWidth;
        double screenH = SystemParameters.PrimaryScreenHeight;
        if (Left + ActualWidth > screenW) Left = Math.Max(10, screenW - ActualWidth - 10);
        if (Left < 0) Left = 10;
        if (Top < 0) Top = 10;
        if (Top + ActualHeight > screenH) Top = Math.Max(10, screenH - ActualHeight - 10);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static readonly Brush DockBrushActiveBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
    private static readonly Brush DockBrushActiveBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8"));
    private static readonly Brush DockBrushInactiveBg = Brushes.White;
    private static readonly Brush DockBrushInactiveBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
    private static readonly Brush DockBrushTextMain = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));

    private void ApplyDockButtonState(Button? btn, Ellipse? dot, bool isActive)
    {
        if (btn == null) return;
        if (isActive)
        {
            btn.Background = DockBrushActiveBg;
            btn.BorderBrush = DockBrushActiveBorder;
            btn.BorderThickness = new Thickness(1.5);
            btn.Foreground = Brushes.White;
            if (dot != null) dot.Visibility = Visibility.Visible;
        }
        else
        {
            btn.Background = DockBrushInactiveBg;
            btn.BorderBrush = DockBrushInactiveBorder;
            btn.BorderThickness = new Thickness(1.5);
            btn.Foreground = DockBrushTextMain;
            if (dot != null) dot.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateActiveToolIndicators()
    {
        if (!IsVisible) return;

        ApplyDockButtonState(BtnBoard, DotBoard, _studentBoard.IsVisible);
        ApplyDockButtonState(BtnDrawScreen, DotDrawScreen, _screenDrawing.IsVisible && !_screenDrawing.IsBoardMode);
        ApplyDockButtonState(BtnDrawBoard, DotDrawBoard, _screenDrawing.IsVisible && _screenDrawing.IsBoardMode);
        ApplyDockButtonState(BtnTimer, DotTimer, _timerWindow.IsVisible);
        ApplyDockButtonState(BtnPicker, DotPicker, _pickerWindow.IsVisible);

        ApplyDockButtonState(BtnVisualizer, DotVisualizer, _visualizerWindow.IsVisible);
        ApplyDockButtonState(BtnNoise, DotNoise, _noiseWindow.IsVisible);
        ApplyDockButtonState(BtnSoundboard, DotSoundboard, _soundboardWindow.IsVisible);
        ApplyDockButtonState(BtnSeat, DotSeat, _seatWindow.IsVisible);
        ApplyDockButtonState(BtnSignature, DotSignature, _signatureWindow.IsVisible);

        if (_displayManager != null && _displayManager.IsDualMonitor)
        {
            BtnSwitchMonitor.ToolTip = "학생 화면(놀보드) 모니터 1 ↔ 2 전환 (듀얼 모니터 감지됨)";
        }
        else
        {
            BtnSwitchMonitor.ToolTip = "단일 모니터 환경 (듀얼 모니터 연결 시 1↔2 전환 지원)";
        }
    }

    private void MoveWindowToMonitor(Window win, int monitorIndex, bool maximize = false)
    {
        if (_displayManager != null)
        {
            int target = (_displayManager.ScreenCount > monitorIndex && monitorIndex >= 0) ? monitorIndex : 0;
            _displayManager.MoveWindowToScreen(win, target, maximize);
        }
    }

    private void BtnBoard_Click(object sender, RoutedEventArgs e)
    {
        if (_studentBoard.IsVisible && _studentBoard.CurrentMonitorIndex == 0)
        {
            _studentBoard.Hide();
        }
        else
        {
            _studentBoard.ShowOnMonitor(0);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnBoard_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_studentBoard.IsVisible && _studentBoard.CurrentMonitorIndex == target)
        {
            _studentBoard.Hide();
        }
        else
        {
            _studentBoard.ShowOnMonitor(target);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnDrawScreen_Click(object sender, RoutedEventArgs e)
    {
        if (_screenDrawing.IsVisible && !_screenDrawing.IsBoardMode)
        {
            _screenDrawing.CloseOverlay();
        }
        else
        {
            _screenDrawing.FreezeAndShow(0);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnDrawScreen_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_screenDrawing.IsVisible && !_screenDrawing.IsBoardMode)
        {
            _screenDrawing.CloseOverlay();
        }
        else
        {
            _screenDrawing.FreezeAndShow(target);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnDrawBoard_Click(object sender, RoutedEventArgs e)
    {
        if (_screenDrawing.IsVisible && _screenDrawing.IsBoardMode)
        {
            _screenDrawing.CloseOverlay();
        }
        else
        {
            _screenDrawing.ShowBoardMode("chalkboard", 0);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnDrawBoard_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_screenDrawing.IsVisible && _screenDrawing.IsBoardMode)
        {
            _screenDrawing.CloseOverlay();
        }
        else
        {
            _screenDrawing.ShowBoardMode("chalkboard", target);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnTimer_Click(object sender, RoutedEventArgs e)
    {
        if (_timerWindow.IsVisible)
        {
            _timerWindow.Hide();
        }
        else
        {
            _timerWindow.PositionToMonitor(0);
            _timerWindow.Show();
            _timerWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnTimer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_timerWindow.IsVisible)
        {
            _timerWindow.Hide();
        }
        else
        {
            _timerWindow.PositionToMonitor(target);
            _timerWindow.Show();
            _timerWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnPicker_Click(object sender, RoutedEventArgs e)
    {
        if (_pickerWindow.IsVisible)
        {
            _pickerWindow.Hide();
        }
        else
        {
            _pickerWindow.ShowOnMonitor(0);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnPicker_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_pickerWindow.IsVisible)
        {
            _pickerWindow.Hide();
        }
        else
        {
            _pickerWindow.ShowOnMonitor(target);
        }
        UpdateActiveToolIndicators();
    }

    private void BtnNotice_Click(object sender, RoutedEventArgs e)
    {
        if (!_studentBoard.IsVisible)
        {
            _displayManager?.MoveToStudentMonitor(_studentBoard, maximize: true);
            _studentBoard.Show();
            _studentBoard.Activate();
        }

        var memoHost = _studentBoard.FindWidget("memo");
        if (memoHost != null)
        {
            memoHost.Visibility = Visibility.Visible;
        }

        HudNotificationWindow.Instance.ShowToast("📢 알림장", "학생 화면(놀보드)에 알림장을 표시합니다.");
        UpdateActiveToolIndicators();
    }

    private void BtnSwitchMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (_displayManager == null || !_displayManager.IsDualMonitor)
        {
            HudNotificationWindow.Instance.ShowToast("💻 단일 모니터", "현재 단일 모니터 환경입니다.\n듀얼 모니터(학생 화면) 연결 시 자동 지원됩니다.");
            return;
        }

        if (!_studentBoard.IsVisible)
        {
            _displayManager.MoveToStudentMonitor(_studentBoard, maximize: true);
            _studentBoard.Show();
            _studentBoard.Activate();
            HudNotificationWindow.Instance.ShowToast("📺 놀보드 표시", "학생용 모니터에 놀보드를 열었습니다.");
        }
        else
        {
            _studentBoard.ToggleMonitor();
            HudNotificationWindow.Instance.ShowToast("📺 화면 전환", "놀보드 표시 모니터를 전환했습니다.");
        }
        UpdateActiveToolIndicators();
    }

    private void BtnToggleExpand_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        SecondaryToolsPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        TxtExpandIcon.Text = _isExpanded ? "▲" : "▼";
        BtnToggleExpand.ToolTip = _isExpanded ? "보조 교실 도구 접기 (콤팩트 모드)" : "보조 교실 도구 더보기 (화상기, 소음, 효과음 등)";
        EnsureWindowWithinScreen();
    }

    private void BtnVisualizer_Click(object sender, RoutedEventArgs e)
    {
        if (_visualizerWindow.IsVisible) _visualizerWindow.Hide();
        else
        {
            MoveWindowToMonitor(_visualizerWindow, 0, maximize: false);
            _visualizerWindow.Show();
            _visualizerWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnVisualizer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_visualizerWindow.IsVisible) _visualizerWindow.Hide();
        else
        {
            MoveWindowToMonitor(_visualizerWindow, target, maximize: false);
            _visualizerWindow.Show();
            _visualizerWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnNoise_Click(object sender, RoutedEventArgs e)
    {
        if (_noiseWindow.IsVisible) _noiseWindow.Hide();
        else
        {
            MoveWindowToMonitor(_noiseWindow, 0, maximize: false);
            _noiseWindow.Show();
            _noiseWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnNoise_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_noiseWindow.IsVisible) _noiseWindow.Hide();
        else
        {
            MoveWindowToMonitor(_noiseWindow, target, maximize: false);
            _noiseWindow.Show();
            _noiseWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnSoundboard_Click(object sender, RoutedEventArgs e)
    {
        if (_soundboardWindow.IsVisible) _soundboardWindow.Hide();
        else
        {
            MoveWindowToMonitor(_soundboardWindow, 0, maximize: false);
            _soundboardWindow.Show();
            _soundboardWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnSoundboard_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_soundboardWindow.IsVisible) _soundboardWindow.Hide();
        else
        {
            MoveWindowToMonitor(_soundboardWindow, target, maximize: false);
            _soundboardWindow.Show();
            _soundboardWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnSeat_Click(object sender, RoutedEventArgs e)
    {
        if (_seatWindow.IsVisible) _seatWindow.Hide();
        else
        {
            MoveWindowToMonitor(_seatWindow, 0, maximize: false);
            _seatWindow.Show();
            _seatWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnSeat_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_seatWindow.IsVisible) _seatWindow.Hide();
        else
        {
            MoveWindowToMonitor(_seatWindow, target, maximize: false);
            _seatWindow.Show();
            _seatWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnSignature_Click(object sender, RoutedEventArgs e)
    {
        if (_signatureWindow.IsVisible) _signatureWindow.Hide();
        else
        {
            MoveWindowToMonitor(_signatureWindow, 0, maximize: false);
            _signatureWindow.Show();
            _signatureWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnSignature_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int target = (_displayManager != null && _displayManager.IsDualMonitor) ? 1 : 0;
        if (_signatureWindow.IsVisible) _signatureWindow.Hide();
        else
        {
            MoveWindowToMonitor(_signatureWindow, target, maximize: false);
            _signatureWindow.Show();
            _signatureWindow.Activate();
        }
        UpdateActiveToolIndicators();
    }

    private void BtnQr_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new QrCodeModalDialog(_qrCodeService, "📱 QR코드 생성기", "https://pinky-ne.com");
        dlg.Show();
    }

    private void BtnZen_Click(object sender, RoutedEventArgs e)
    {
        var (_, _, msg) = _cleanerService.ToggleDesktopIcons();
        MessageBox.Show(msg, "젠 클리너", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
