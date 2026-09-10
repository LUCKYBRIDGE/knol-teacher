using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui.Appearance;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.ViewModels;
using KnolTeacher.Desktop.Views.Windows;

namespace KnolTeacher.Desktop;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "KnolTeacherDesktopNetMutex";

    private readonly IHost _host;
    public IServiceProvider? Services => _host?.Services;

    public App()
    {
        // Global Unhandled Exception Handling
        DispatcherUnhandledException += (s, args) =>
        {
            BootLog($"[DispatcherUnhandledException] {args.Exception}");
            MessageBox.Show($"UI 예외 발생:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "놀티쳐 .NET 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                BootLog($"[AppDomain UnhandledException] {ex}");
                MessageBox.Show($"시스템 예외 발생:\n{ex.Message}\n\n{ex.StackTrace}",
                    "놀티쳐 .NET 치명적 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core Services
                services.AddSingleton<IConfigService, ConfigService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<IDisplayManager, DisplayManager>();
                services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
                services.AddSingleton<IDesktopCleanerService, DesktopCleanerService>();
                services.AddSingleton<IStudentManagerService, StudentManagerService>();
                services.AddSingleton<INeisService, NeisService>();
                services.AddSingleton<ISoundService, SoundService>();
                services.AddSingleton<ITimetableService, TimetableService>();
                services.AddSingleton<ISchedulerService, SchedulerService>();
                services.AddSingleton<ISiteBookmarkService, SiteBookmarkService>();
                services.AddSingleton<IQrCodeService, QrCodeService>();
                services.AddSingleton<INeisCommentBatchService, NeisCommentBatchService>();
                services.AddSingleton<ISchoolScaleService, SchoolScaleService>();
                services.AddSingleton<INoiseMeterService, NoiseMeterService>();
                services.AddSingleton<IWeatherService, WeatherService>();
                services.AddSingleton<IWorkdayCalculatorService, WorkdayCalculatorService>();
                services.AddSingleton<IAcademicCalendarService, AcademicCalendarService>();
                services.AddSingleton<ITrayService, TrayService>();
                services.AddSingleton<ITtsService, TtsService>();
                services.AddSingleton<IEarlyLeaveCalculatorService, EarlyLeaveCalculatorService>();
                services.AddSingleton<IUpdateService, UpdateService>();
                services.AddSingleton<IStartupService, StartupService>();
                services.AddSingleton<IDataShareService, DataShareService>();
                services.AddSingleton<IClassroomRecordService, ClassroomRecordService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();

                // Windows
                services.AddSingleton<MainWindow>();
                services.AddSingleton<StudentDisplayWindow>();
                services.AddSingleton<ScreenDrawingOverlayWindow>();
                services.AddSingleton<VisualizerWindow>();
                services.AddSingleton<ClassroomTimerWindow>();
                services.AddSingleton<StudentPickerWindow>();
                services.AddSingleton<FloatingToolbarWindow>();
                services.AddSingleton<SchoolScaleWindow>();
                services.AddSingleton<NoiseTrafficLightWindow>();
                services.AddSingleton<WorkdayCalculatorWindow>();
                services.AddSingleton<SmartSeatShuffleWindow>();
                services.AddSingleton<ClassroomSoundboardWindow>();
                services.AddSingleton<DigitalSignatureWindow>();
                services.AddTransient<TemplateShareWindow>();
                services.AddSingleton<ClassroomHubWindow>();
                services.AddTransient<WeeklyTimetableWindow>();
            })
            .Build();
    }

    public static void BootLog(string msg)
    {
        try
        {
            string appData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KnolTeacher");
            System.IO.Directory.CreateDirectory(appData);
            var logPath = System.IO.Path.Combine(appData, "knol_boot.log");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        BootLog("OnStartup started");
        try
        {
            // 1. Single-Instance Check
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            BootLog($"Mutex createdNew: {createdNew}");
            if (!createdNew)
            {
                BootLog("Single instance check failed - bringing existing instance to front");
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            base.OnStartup(e);
            BootLog("base.OnStartup done");
            ShowSplash();

            // 2. Start DI Host
            _host.Start();
            BootLog("DI host started");

            // 3. Apply Theme
            try
            {
                var themeService = _host.Services.GetRequiredService<IThemeService>();
                themeService.ApplyTheme(themeService.CurrentTheme);
                BootLog($"Theme applied: {themeService.CurrentTheme}");
            }
            catch (Exception ex)
            {
                BootLog($"Theme error: {ex}");
            }

            // 4. Show MainWindow First
            BootLog("Resolving MainWindow...");
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            BootLog("Showing MainWindow...");
            mainWindow.Show();
            mainWindow.Activate();
            CloseSplash();
            BootLog("MainWindow shown successfully");

            // 5. Initialize Global Hotkeys & Tool Handlers
            BootLog("Starting step 5: hotkeys...");
            try
            {
                var hotkeyService = _host.Services.GetRequiredService<IGlobalHotkeyService>();
                BootLog("Calling hotkeyService.Initialize()...");
                hotkeyService.Initialize();
                BootLog("hotkeyService.Initialize() done");
                var displayManager = _host.Services.GetRequiredService<IDisplayManager>();
                BootLog("displayManager resolved");

                hotkeyService.HotkeyPressed += (action) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        switch (action)
                        {
                            case "board":
                            case "board_f2":
                                var studentBoard = _host.Services.GetRequiredService<StudentDisplayWindow>();
                                if (studentBoard.IsVisible)
                                {
                                    studentBoard.Hide();
                                    HudNotificationWindow.Instance.ShowToast("📺", "놀보드 닫힘");
                                }
                                else
                                {
                                    displayManager.MoveToStudentMonitor(studentBoard, maximize: true);
                                    studentBoard.Show();
                                    studentBoard.Activate();
                                    HudNotificationWindow.Instance.ShowToast("📺", "놀보드 열림 (F2)");
                                }
                                break;

                            case "drawing":
                                var screenDrawing = _host.Services.GetRequiredService<ScreenDrawingOverlayWindow>();
                                if (screenDrawing.IsVisible && !screenDrawing.IsBoardMode)
                                {
                                    screenDrawing.CloseOverlay();
                                    HudNotificationWindow.Instance.ShowToast("🖼️", "화면 주석 판서 종료 (ESC)");
                                }
                                else
                                {
                                    screenDrawing.FreezeAndShow();
                                    HudNotificationWindow.Instance.ShowToast("🖼️", "화면 주석 판서 시작 (Alt+2)");
                                }
                                break;

                            case "board_drawing":
                                var boardDrawing = _host.Services.GetRequiredService<ScreenDrawingOverlayWindow>();
                                if (boardDrawing.IsVisible && boardDrawing.IsBoardMode)
                                {
                                    boardDrawing.CloseOverlay();
                                    HudNotificationWindow.Instance.ShowToast("🟩", "수업 칠판 보드판 닫기 (ESC)");
                                }
                                else
                                {
                                    boardDrawing.ShowBoardMode("chalkboard");
                                    HudNotificationWindow.Instance.ShowToast("🟩", "수업 칠판 보드판 시작 (Alt+4)");
                                }
                                break;

                            case "timer":
                                var timerWindow = _host.Services.GetRequiredService<ClassroomTimerWindow>();
                                if (timerWindow.IsVisible)
                                {
                                    timerWindow.Hide();
                                    HudNotificationWindow.Instance.ShowToast("⏱️", "교실 타이머 숨김");
                                }
                                else
                                {
                                    timerWindow.PositionToDefaultMonitor();
                                    timerWindow.Show();
                                    timerWindow.Activate();
                                    HudNotificationWindow.Instance.ShowToast("⏱️", "교실 집중 타이머 (Alt+3)");
                                }
                                break;

                            case "picker":
                                var pickerWindow = _host.Services.GetRequiredService<StudentPickerWindow>();
                                if (pickerWindow.IsVisible)
                                {
                                    pickerWindow.Hide();
                                    HudNotificationWindow.Instance.ShowToast("🎲", "발표자 추첨 숨김");
                                }
                                else
                                {
                                    displayManager.MoveToStudentMonitor(pickerWindow, maximize: false);
                                    pickerWindow.Show();
                                    pickerWindow.Activate();
                                    HudNotificationWindow.Instance.ShowToast("🎲", "발표자 추첨기 (Alt+8)");
                                }
                                break;

                            case "dock":
                                var dockWindow = _host.Services.GetRequiredService<FloatingToolbarWindow>();
                                if (dockWindow.IsVisible)
                                {
                                    dockWindow.Hide();
                                    HudNotificationWindow.Instance.ShowToast("🏝️", "화면 상단 도구바 숨김");
                                }
                                else
                                {
                                    dockWindow.Show();
                                    dockWindow.Activate();
                                    HudNotificationWindow.Instance.ShowToast("🏝️", "화면 상단 도구바 열림 (Alt+9)");
                                }
                                break;

                            case "visualizer":
                                var visualizer = _host.Services.GetRequiredService<VisualizerWindow>();
                                if (visualizer.IsVisible)
                                {
                                    visualizer.Hide();
                                    HudNotificationWindow.Instance.ShowToast("📷", "실물화상기 닫힘");
                                }
                                else
                                {
                                    displayManager.MoveToStudentMonitor(visualizer, maximize: false);
                                    visualizer.Show();
                                    visualizer.Activate();
                                    HudNotificationWindow.Instance.ShowToast("📷", "스마트 실물화상기");
                                }
                                break;

                            case "youtube":
                            case "qr":
                                var qrService = _host.Services.GetRequiredService<IQrCodeService>();
                                var qrDlg = new QrCodeModalDialog(qrService, "📱 빠른 QR코드 생성기", "https://pinky-ne.com/");
                                qrDlg.Show();
                                qrDlg.Activate();
                                HudNotificationWindow.Instance.ShowToast("📱", "QR코드 생성기 (Alt+Q)");
                                break;

                            case "signature":
                                var sigWin = _host.Services.GetRequiredService<DigitalSignatureWindow>();
                                if (sigWin.IsVisible)
                                {
                                    sigWin.Hide();
                                    HudNotificationWindow.Instance.ShowToast("🔏", "전자서명 숨김");
                                }
                                else
                                {
                                    sigWin.Show();
                                    sigWin.Activate();
                                    HudNotificationWindow.Instance.ShowToast("🔏", "전자서명 & 도장 (Alt+S)");
                                }
                                break;

                            case "noise":
                                var noiseWin = _host.Services.GetRequiredService<NoiseTrafficLightWindow>();
                                if (noiseWin.IsVisible)
                                {
                                    noiseWin.Hide();
                                    HudNotificationWindow.Instance.ShowToast("🚦", "소음 신호등 숨김");
                                }
                                else
                                {
                                    noiseWin.Show();
                                    noiseWin.Activate();
                                    HudNotificationWindow.Instance.ShowToast("🚦", "교실 소음 신호등 (Alt+N)");
                                }
                                break;

                            case "soundboard":
                                var soundWin = _host.Services.GetRequiredService<ClassroomSoundboardWindow>();
                                if (soundWin.IsVisible)
                                {
                                    soundWin.Hide();
                                    HudNotificationWindow.Instance.ShowToast("🔔", "효과음 보드 숨김");
                                }
                                else
                                {
                                    soundWin.Show();
                                    soundWin.Activate();
                                    HudNotificationWindow.Instance.ShowToast("🔔", "교실 효과음 보드 (Alt+B)");
                                }
                                break;
                        }
                    });
                };
                BootLog("Step 5 completed successfully");
            }
            catch (Exception ex)
            {
                BootLog($"Hotkey/tool init error: {ex}");
                var fullErr = ex.InnerException != null ? $"{ex.Message}\n\n[상세 정보]: {ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"핫키 초기화 알림: {fullErr}", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            // 6. Start Background Scheduler Service
            try
            {
                var scheduler = _host.Services.GetRequiredService<ISchedulerService>();
                scheduler.Start();
                BootLog("Scheduler service started successfully");
            }
            catch (Exception ex)
            {
                BootLog($"Scheduler service start note: {ex.Message}");
            }

            BootLog("OnStartup finished completely");
        }
        catch (Exception ex)
        {
            BootLog($"FATAL OnStartup error: {ex}");
            MessageBox.Show($"시작 오류 발생:\n{ex.Message}\n\n{ex.StackTrace}", "시작 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        BootLog($"OnExit called with Application ExitCode: {e.ApplicationExitCode}");
        try
        {
            var hotkeyService = _host.Services.GetService<IGlobalHotkeyService>();
            hotkeyService?.Dispose();

            var trayService = _host.Services.GetService<ITrayService>();
            trayService?.Dispose();

            await _host.StopAsync();
            _host.Dispose();

            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch { }

        base.OnExit(e);
    }

    private static Window? _splashWindow;

    private static void ShowSplash()
    {
        try
        {
            var win = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Width = 320,
                Height = 100,
                Content = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F172A")),
                    CornerRadius = new CornerRadius(14),
                    BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#38BDF8")),
                    BorderThickness = new Thickness(1.5),
                    Padding = new Thickness(18, 14, 18, 14),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 24,
                        Opacity = 0.5,
                        ShadowDepth = 5,
                        Color = System.Windows.Media.Colors.Black
                    },
                    Child = new System.Windows.Controls.StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Children =
                        {
                            new System.Windows.Controls.StackPanel
                            {
                                Orientation = System.Windows.Controls.Orientation.Horizontal,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                Margin = new Thickness(0, 0, 0, 6),
                                Children =
                                {
                                    new System.Windows.Controls.TextBlock { Text = "✨", FontSize = 16, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center },
                                    new System.Windows.Controls.TextBlock { Text = "놀티쳐 (KnolTeacher)", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White, VerticalAlignment = VerticalAlignment.Center }
                                }
                            },
                            new System.Windows.Controls.TextBlock
                            {
                                Text = "프로그램을 시작하는 중입니다...",
                                FontSize = 11,
                                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8")),
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                            }
                        }
                    }
                }
            };
            win.Show();
            _splashWindow = win;
        }
        catch { }
    }

    private static void CloseSplash()
    {
        try
        {
            _splashWindow?.Close();
            _splashWindow = null;
        }
        catch { }
    }

    private static void BringExistingInstanceToFront()
    {
        try
        {
            var current = System.Diagnostics.Process.GetCurrentProcess();
            var processes = System.Diagnostics.Process.GetProcessesByName(current.ProcessName)
                .Concat(System.Diagnostics.Process.GetProcessesByName("놀티쳐"))
                .Concat(System.Diagnostics.Process.GetProcessesByName("KnolTeacher.Desktop"))
                .Where(p => p.Id != current.Id)
                .ToList();

            foreach (var p in processes)
            {
                IntPtr hWnd = p.MainWindowHandle;
                int retry = 0;
                while (hWnd == IntPtr.Zero && retry < 8)
                {
                    System.Threading.Thread.Sleep(200);
                    p.Refresh();
                    hWnd = p.MainWindowHandle;
                    retry++;
                }

                if (hWnd != IntPtr.Zero)
                {
                    if (NativeMethods.IsIconic(hWnd))
                    {
                        NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SW_RESTORE);
                    }
                    else
                    {
                        NativeMethods.ShowWindowAsync(hWnd, NativeMethods.SW_SHOWNORMAL);
                    }
                    NativeMethods.SetForegroundWindow(hWnd);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            BootLog($"BringExistingInstanceToFront error: {ex.Message}");
        }
    }
}
