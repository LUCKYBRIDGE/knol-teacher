using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.Views.Controls;
using KnolTeacher.Desktop.Views.Controls.Widgets;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class StudentDisplayWindow : Window
{
    private readonly ISoundService _soundService;
    private readonly IStudentManagerService _studentService;
    private readonly ITimetableService _timetableService;
    private readonly INeisService _neisService;
    private readonly IConfigService _configService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IClassroomTaskService? _taskService;
    private readonly ITtsService? _ttsService;
    private readonly IDisplayManager? _displayManager;
    private readonly IWeatherService? _weatherService;
    private int _currentMonitorIndex = 1;

    private readonly DrawingUndoManager _undoManager = new();
    private DrawingEraserHelper? _eraserHelper;
    private MultiTouchInkHelper? _multiTouchHelper;
    private readonly DispatcherTimer _clockTimer;
    private readonly List<BoardWidgetHost> _widgets = new();
    private bool _isWidgetsLocked = false;
    private double _currentCardOpacity = 0.95;
    private bool _isReady = false;
    private int _layoutSaveSuppressionDepth = 0;

    public StudentDisplayWindow(
        ISoundService soundService,
        IStudentManagerService studentService,
        ITimetableService timetableService,
        INeisService neisService,
        IConfigService configService,
        IQrCodeService qrCodeService,
        ITtsService? ttsService = null,
        IDisplayManager? displayManager = null,
        IWeatherService? weatherService = null,
        IClassroomTaskService? taskService = null)
    {
        _soundService = soundService;
        _studentService = studentService;
        _timetableService = timetableService;
        _neisService = neisService;
        _configService = configService;
        _qrCodeService = qrCodeService;
        _taskService = taskService ?? (Application.Current as App)?.Services?.GetService(typeof(IClassroomTaskService)) as IClassroomTaskService;
        _ttsService = ttsService ?? (Application.Current as App)?.Services?.GetService(typeof(ITtsService)) as ITtsService;
        _displayManager = displayManager ?? (Application.Current as App)?.Services?.GetService(typeof(IDisplayManager)) as IDisplayManager;
        _weatherService = weatherService ?? (Application.Current as App)?.Services?.GetService(typeof(IWeatherService)) as IWeatherService;

        InitializeComponent();

        // 전자칠판 터치 시 윈도우 우클릭 딜레이 및 제스처 렉 방지
        Stylus.SetIsPressAndHoldEnabled(this, false);
        Stylus.SetIsFlicksEnabled(this, false);
        Stylus.SetIsTapFeedbackEnabled(this, false);
        Stylus.SetIsTouchFeedbackEnabled(this, false);

        BoardInkCanvas.DefaultDrawingAttributes = new DrawingAttributes
        {
            Color = (Color)ColorConverter.ConvertFromString("#0F172A"),
            Width = 4,
            Height = 4,
            FitToCurve = true,
            IgnorePressure = false
        };

        Stylus.SetIsPressAndHoldEnabled(BoardInkCanvas, false);
        Stylus.SetIsFlicksEnabled(BoardInkCanvas, false);

        BoardInkCanvas.EditingMode = InkCanvasEditingMode.None;

        // 지우개 시스템 헬퍼 초기화 (부분/획/구역/영역 및 Undo 통합 관리)
        _eraserHelper = new DrawingEraserHelper(BoardInkCanvas, BoardEraserPreviewCanvas, _undoManager);

        // 전자칠판 선별적 멀티터치 판서 헬퍼 연결 (2명 이상 동시 필기 지원)
        _multiTouchHelper = new MultiTouchInkHelper(BoardInkCanvas);
        _multiTouchHelper.StrokeCollected += stroke =>
        {
            _undoManager.PushAdded(stroke);
        };
        _multiTouchHelper.IsEnabled = false; // 기본 잉크 비활성 (ToggleInkMode 켜질 때 활성화)

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => TxtClock.Text = DateTime.Now.ToString("HH:mm:ss");
        _clockTimer.Start();
        TxtClock.Text = DateTime.Now.ToString("HH:mm:ss");

        PowerPointPresentationHelper.SlideShowStateChanged += running =>
        {
            Dispatcher.Invoke(() =>
            {
                if (PptRemoteControlPanel != null)
                {
                    PptRemoteControlPanel.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
                }

                if (running)
                {
                    PptRemoteControllerWindow.Instance.OpenForPresentation(PowerPointPresentationHelper.CurrentPresentationTitle);
                }
                else
                {
                    PptRemoteControllerWindow.Instance.CloseRemote();
                }
            });
        };

        WidgetCanvas.SizeChanged += OnWidgetCanvasSizeChanged;

        InitDashboard();

        _isReady = true;
        Loaded += (s, e) =>
        {
            PositionToDefaultMonitor();
            RestoreWidgetsLayout();
            SetDisplayMode(true);
        };
    }

    #region Widget Management

    private BoardWidgetHost? _selectedWidget;

    public BoardWidgetHost? SelectedWidget
    {
        get => _selectedWidget;
        set
        {
            if (_selectedWidget == value) return;
            if (_selectedWidget != null)
            {
                _selectedWidget.IsSelected = false;
            }
            _selectedWidget = value;
            if (_selectedWidget != null)
            {
                _selectedWidget.IsSelected = true;
            }
        }
    }

    public void SelectWidget(BoardWidgetHost host)
    {
        SelectedWidget = host;
    }

    public void DeselectWidget()
    {
        SelectedWidget = null;
    }

    public void UpdateEmptyHint()
    {
        if (EmptyBoardHint == null) return;
        if (RbBoardModeDashboard?.IsChecked == true)
        {
            EmptyBoardHint.Visibility = Visibility.Collapsed;
            return;
        }
        EmptyBoardHint.Visibility = _widgets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool IsLayoutSaveSuppressed => _layoutSaveSuppressionDepth > 0;

    private void BeginLayoutBatch()
    {
        _layoutSaveSuppressionDepth++;
    }

    private void EndLayoutBatch(bool saveFinalState)
    {
        if (_layoutSaveSuppressionDepth > 0)
        {
            _layoutSaveSuppressionDepth--;
        }

        if (saveFinalState && _layoutSaveSuppressionDepth == 0)
        {
            SaveWidgetsLayout();
        }
    }

    public void ClearWidgets(bool saveLayout = true)
    {
        SelectedWidget = null;
        foreach (var widget in _widgets.ToArray())
        {
            widget.DisposeContent();
            WidgetCanvas.Children.Remove(widget);
        }

        _widgets.Clear();
        UpdateDockButtonsState();
        UpdateEmptyHint();

        if (saveLayout)
        {
            SaveWidgetsLayout();
        }
    }

    public void CloseWidget(BoardWidgetHost host)
    {
        if (host == null) return;
        host.DisposeContent();
        WidgetCanvas.Children.Remove(host);
        _widgets.Remove(host);
        if (_selectedWidget == host)
        {
            SelectedWidget = null;
        }
        UpdateDockButtonsState();
        UpdateEmptyHint();
        SaveWidgetsLayout();
    }

    public BoardWidgetHost AddWidget(string type, string title, UserControl view, double x, double y, double w, double h)
    {
        var host = new BoardWidgetHost
        {
            WidgetType = type,
            Title = title,
            WidgetContent = view,
            Width = w,
            Height = h,
            IsLocked = _isWidgetsLocked,
            CardOpacity = _currentCardOpacity
        };

        Canvas.SetLeft(host, x);
        Canvas.SetTop(host, y);

        host.Closed += target => CloseWidget(target);
        host.Selected += target => SelectWidget(target);
        host.MovedOrResized += _ => SaveWidgetsLayout();

        _widgets.Add(host);
        WidgetCanvas.Children.Add(host);
        ClampAllWidgetsWithinCanvas();
        SelectWidget(host);
        UpdateDockButtonsState();
        UpdateEmptyHint();
        SaveWidgetsLayout();
        return host;
    }

    public void SaveWidgetsLayout()
    {
        if (!_isReady || IsLayoutSaveSuppressed) return;

        try
        {
            var list = new List<NolboardWidgetState>();
            foreach (var widget in _widgets)
            {
                double x = Canvas.GetLeft(widget);
                double y = Canvas.GetTop(widget);
                if (double.IsNaN(x) || double.IsInfinity(x)) x = 0;
                if (double.IsNaN(y) || double.IsInfinity(y)) y = 0;

                double width = widget.ActualWidth > 0
                    ? widget.ActualWidth
                    : (double.IsNaN(widget.Width) ? 340 : widget.Width);
                double height = widget.ActualHeight > 0
                    ? widget.ActualHeight
                    : (double.IsNaN(widget.Height) ? 260 : widget.Height);

                list.Add(new NolboardWidgetState
                {
                    Tag = widget.WidgetType,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height
                });
            }

            var layout = _configService.NolboardLayout ?? new NolboardLayoutConfig();
            layout.SchemaVersion = NolboardLayoutConfig.CurrentSchemaVersion;
            layout.Widgets = list;
            layout.HasCustomLayout = true;
            layout.CanvasWidth = WidgetCanvas.ActualWidth > 0 ? WidgetCanvas.ActualWidth : 0;
            layout.CanvasHeight = WidgetCanvas.ActualHeight > 0 ? WidgetCanvas.ActualHeight : 0;
            _configService.NolboardLayout = layout;
            _configService.SaveNolboardLayout();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Nolboard] Failed to save layout: {ex.GetType().Name}");
        }
    }

    public bool RestoreWidgetsLayout()
    {
        var layout = _configService.NolboardLayout;
        if (layout == null || !layout.HasCustomLayout)
        {
            return false;
        }

        bool migrateLegacyLayout = layout.SchemaVersion < NolboardLayoutConfig.CurrentSchemaVersion;
        BeginLayoutBatch();
        try
        {
            ClearWidgets(saveLayout: false);

            // An intentionally empty custom workspace must remain empty on next launch.
            if (layout.Widgets == null || layout.Widgets.Count == 0)
            {
                layout.SchemaVersion = NolboardLayoutConfig.CurrentSchemaVersion;
                return true;
            }

            double currentCanvasWidth = WidgetCanvas.ActualWidth;
            double currentCanvasHeight = WidgetCanvas.ActualHeight;
            double scaleX = 1.0;
            double scaleY = 1.0;

            if (layout.CanvasWidth > 200 && layout.CanvasHeight > 200 &&
                currentCanvasWidth > 200 && currentCanvasHeight > 200)
            {
                scaleX = currentCanvasWidth / layout.CanvasWidth;
                scaleY = currentCanvasHeight / layout.CanvasHeight;
            }

            foreach (var state in layout.Widgets)
            {
                if (string.IsNullOrWhiteSpace(state.Tag)) continue;

                // v2 layouts used much smaller defaults. Reflow once on upgrade so the old
                // tiny dimensions do not defeat the new classroom-size defaults.
                var host = migrateLegacyLayout
                    ? SpawnWidget(state.Tag)
                    : SpawnWidget(state.Tag, state.X * scaleX, state.Y * scaleY);
                if (host == null) continue;

                if (!migrateLegacyLayout && state.Width > 100)
                {
                    host.Width = Math.Max(host.MinWidth, state.Width * scaleX);
                }

                if (!migrateLegacyLayout && state.Height > 80)
                {
                    host.Height = Math.Max(host.MinHeight, state.Height * scaleY);
                }
            }

            layout.SchemaVersion = NolboardLayoutConfig.CurrentSchemaVersion;
            ClampAllWidgetsWithinCanvas();
            UpdateDockButtonsState();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Nolboard] Failed to restore layout: {ex.GetType().Name}");
            return false;
        }
        finally
        {
            EndLayoutBatch(saveFinalState: migrateLegacyLayout);
        }
    }

    private void BtnSaveBoardLayout_Click(object sender, RoutedEventArgs e)
    {
        SaveWidgetsLayout();
        MessageBox.Show("현재 놀보드 위젯 배치가 안전하게 저장되었습니다.\n다음에 놀보드를 열 때 이 상태로 자동 복원됩니다.", "놀보드 배치 저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public BoardWidgetHost? FindWidget(string type) =>
        _widgets.Find(w => string.Equals(w.WidgetType, type, StringComparison.OrdinalIgnoreCase));

    public void ToggleWidget(string key)
    {
        if (string.Equals(key, "pinball", StringComparison.OrdinalIgnoreCase))
        {
            OpenPinballWindow();
            return;
        }

        var existing = FindWidget(key);
        if (existing != null)
        {
            CloseWidget(existing);
        }
        else
        {
            SpawnWidget(key);
        }
    }

    public int CurrentMonitorIndex => _currentMonitorIndex;

    public void ShowStudentDashboard(int? monitorIndex = null)
    {
        SetDisplayMode(true);
        if (monitorIndex.HasValue)
        {
            ShowOnMonitor(monitorIndex.Value);
        }
        else
        {
            if (!IsVisible) PositionToDefaultMonitor();
            Show();
            Activate();
        }
    }

    public void ShowWidgetsBoard(int? monitorIndex = null)
    {
        SetDisplayMode(false);
        if (monitorIndex.HasValue)
        {
            ShowOnMonitor(monitorIndex.Value);
        }
        else
        {
            if (!IsVisible) PositionToDefaultMonitor();
            Show();
            Activate();
        }
    }

    public void ShowOnMonitor(int monitorIndex)
    {
        if (_displayManager != null)
        {
            int target = (_displayManager.ScreenCount > monitorIndex && monitorIndex >= 0) ? monitorIndex : 0;
            _currentMonitorIndex = target;
            _displayManager.MoveWindowToScreen(this, target, maximize: true);
            UpdateMonitorButtonText();
        }

        Show();
        Activate();
    }

    private StudentPickerWindow? _pinballWindow;

    public void OpenPinballWindow()
    {
        OpenPinballOnMonitor(_currentMonitorIndex);
    }

    public void PositionToDefaultMonitor()
    {
        if (_displayManager != null)
        {
            _currentMonitorIndex = _displayManager.RecommendedStudentMonitorIndex;
            _displayManager.MoveToStudentMonitor(this, maximize: true);
            UpdateMonitorButtonText();
        }
    }

    private void UpdateMonitorButtonText()
    {
        if (BtnSwitchBoardMonitor != null)
        {
            BtnSwitchBoardMonitor.Content = _currentMonitorIndex == 1 ? "📺 모니터 2 (학생용)" : "💻 모니터 1 (메인)";
        }
    }

    public void ToggleMonitor()
    {
        if (_displayManager == null || _displayManager.ScreenCount < 2) return;
        _currentMonitorIndex = _currentMonitorIndex == 1 ? 0 : 1;
        _displayManager.MoveWindowToScreen(this, _currentMonitorIndex, maximize: true);
        UpdateMonitorButtonText();
    }

    private void BtnSwitchBoardMonitor_Click(object sender, RoutedEventArgs e)
    {
        ToggleMonitor();
    }

    private RulerToolControl? _boardRuler;
    private TriangleRulerToolControl? _boardTriangle;
    private ProtractorToolControl? _boardProtractor;

    private void BtnToggleRuler_Click(object sender, RoutedEventArgs e)
    {
        if (_boardRuler == null)
        {
            _boardRuler = new RulerToolControl();
            _boardRuler.CloseRequested += () =>
            {
                BoardToolsCanvas.Children.Remove(_boardRuler);
                _boardRuler = null;
            };
            Canvas.SetLeft(_boardRuler, Math.Max(40, (BoardContainer.ActualWidth - 460) / 2));
            Canvas.SetTop(_boardRuler, Math.Max(40, (BoardContainer.ActualHeight - 80) / 2));
            BoardToolsCanvas.Children.Add(_boardRuler);
        }
        else
        {
            BoardToolsCanvas.Children.Remove(_boardRuler);
            _boardRuler = null;
        }
    }

    private void BtnToggleTriangle_Click(object sender, RoutedEventArgs e)
    {
        if (_boardTriangle == null)
        {
            _boardTriangle = new TriangleRulerToolControl();
            _boardTriangle.CloseRequested += () =>
            {
                BoardToolsCanvas.Children.Remove(_boardTriangle);
                _boardTriangle = null;
            };
            Canvas.SetLeft(_boardTriangle, Math.Max(40, (BoardContainer.ActualWidth - 320) / 2));
            Canvas.SetTop(_boardTriangle, Math.Max(40, (BoardContainer.ActualHeight - 260) / 2));
            BoardToolsCanvas.Children.Add(_boardTriangle);
        }
        else
        {
            BoardToolsCanvas.Children.Remove(_boardTriangle);
            _boardTriangle = null;
        }
    }

    private void BtnToggleProtractor_Click(object sender, RoutedEventArgs e)
    {
        if (_boardProtractor == null)
        {
            _boardProtractor = new ProtractorToolControl();
            _boardProtractor.CloseRequested += () =>
            {
                BoardToolsCanvas.Children.Remove(_boardProtractor);
                _boardProtractor = null;
            };
            Canvas.SetLeft(_boardProtractor, Math.Max(40, (BoardContainer.ActualWidth - 380) / 2));
            Canvas.SetTop(_boardProtractor, Math.Max(40, (BoardContainer.ActualHeight - 210) / 2));
            BoardToolsCanvas.Children.Add(_boardProtractor);
        }
        else
        {
            BoardToolsCanvas.Children.Remove(_boardProtractor);
            _boardProtractor = null;
        }
    }

    public BoardWidgetHost? SpawnWidget(string tag, double? x = null, double? y = null)
    {
        if (string.Equals(tag, "pinball", StringComparison.OrdinalIgnoreCase))
        {
            OpenPinballWindow();
            return null;
        }

        var definition = WidgetRegistry.GetOrDefault(tag);
        if (definition == null)
        {
            return null;
        }

        if (!definition.AllowMultiple)
        {
            var existing = FindWidget(tag);
            if (existing != null)
            {
                existing.BringToFront();
                return existing;
            }
        }

        Size spawnSize = GetWidgetSpawnSize(definition);
        Point spawnPoint = x.HasValue && y.HasValue
            ? new Point(x.Value, y.Value)
            : FindFreeSpawnPosition(spawnSize.Width, spawnSize.Height);

        UserControl? view = tag switch
        {
            "timer" => new TimerWidgetView(_soundService),
            "picker" => new PickerWidgetView(_studentService, _soundService),
            "dice" => new DiceWidgetView(_soundService),
            "wheel" => new WheelWidgetView(_soundService),
            "score" => new ScoreWidgetView(),
            "drawing" => new DrawingWidgetView(),
            "blackboard" => new DrawingWidgetView(),
            "timetable" => new TimetableWidgetView(_timetableService),
            "meal" => new MealWidgetView(_neisService),
            "memo" => new MemoWidgetView(_configService, _ttsService),
            "checklist" => new ChecklistWidgetView(_configService, _studentService),
            "qr" => new QrWidgetView(_qrCodeService),
            "weather" => new WeatherWidgetView(_weatherService),
            "dday" => new DDayWidgetView(_configService),
            _ => null
        };

        return view == null
            ? null
            : AddWidget(
                definition.Type,
                definition.Title,
                view,
                spawnPoint.X,
                spawnPoint.Y,
                spawnSize.Width,
                spawnSize.Height);
    }

    private Size GetWidgetSpawnSize(WidgetDefinition definition)
    {
        double canvasWidth = WidgetCanvas.ActualWidth > 200
            ? WidgetCanvas.ActualWidth
            : Math.Max(800, ActualWidth - 32);
        double canvasHeight = WidgetCanvas.ActualHeight > 200
            ? WidgetCanvas.ActualHeight
            : Math.Max(560, ActualHeight - 140);

        double maxWidth = Math.Max(definition.MinWidth, canvasWidth - 32);
        double maxHeight = Math.Max(definition.MinHeight, canvasHeight - 32);

        return new Size(
            Math.Min(definition.DefaultWidth, maxWidth),
            Math.Min(definition.DefaultHeight, maxHeight));
    }

    private Point FindFreeSpawnPosition(double width, double height)
    {
        double canvasWidth = WidgetCanvas.ActualWidth > 200
            ? WidgetCanvas.ActualWidth
            : Math.Max(800, ActualWidth - 32);
        double canvasHeight = WidgetCanvas.ActualHeight > 200
            ? WidgetCanvas.ActualHeight
            : Math.Max(560, ActualHeight - 140);

        var occupied = new List<Rect>(_widgets.Count);
        foreach (var widget in _widgets)
        {
            double left = Canvas.GetLeft(widget);
            double top = Canvas.GetTop(widget);
            if (double.IsNaN(left) || double.IsInfinity(left)) left = 16;
            if (double.IsNaN(top) || double.IsInfinity(top)) top = 16;

            double widgetWidth = widget.ActualWidth > 0 ? widget.ActualWidth : widget.Width;
            double widgetHeight = widget.ActualHeight > 0 ? widget.ActualHeight : widget.Height;
            if (double.IsNaN(widgetWidth) || widgetWidth <= 0) widgetWidth = widget.MinWidth;
            if (double.IsNaN(widgetHeight) || widgetHeight <= 0) widgetHeight = widget.MinHeight;

            occupied.Add(new Rect(left, top, widgetWidth, widgetHeight));
        }

        return NolboardPlacementPlanner.FindBestPosition(
            canvasWidth,
            canvasHeight,
            width,
            height,
            occupied);
    }

    public void UpdateDockButtonsState()
    {
        if (!_isReady) return;

        UpdateBtnState(BtnToolTimer, "timer");
        // BtnToolPinball opens the separate race window, not a widget, so its highlight is owned by
        // RefreshV309LauncherVisuals (window visibility). Updating it here always painted it idle.
        UpdateBtnState(BtnToolPicker, "picker");
        UpdateBtnState(BtnToolDice, "dice");
        UpdateBtnState(BtnToolWheel, "wheel");
        UpdateBtnState(BtnToolScore, "score");
        UpdateBtnState(BtnToolDrawing, "drawing");
        UpdateBtnState(BtnToolBlackboard, "blackboard");
        UpdateBtnState(BtnToolTimetable, "timetable");
        UpdateBtnState(BtnToolMeal, "meal");
        UpdateBtnState(BtnToolMemo, "memo");
        UpdateBtnState(BtnToolChecklist, "checklist");
        UpdateBtnState(BtnToolQr, "qr");
        UpdateBtnState(BtnToolWeather, "weather");
        UpdateBtnState(BtnToolDDay, "dday");
    }

    private void UpdateBtnState(Button? btn, string tag)
    {
        if (btn == null) return;
        bool active = _widgets.Exists(w => string.Equals(w.WidgetType, tag, StringComparison.OrdinalIgnoreCase));
        if (active)
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            btn.Foreground = Brushes.White;
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            btn.BorderThickness = new Thickness(1.5);
            btn.FontWeight = FontWeights.Bold;
        }
        else
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            btn.BorderThickness = new Thickness(1);
            btn.FontWeight = FontWeights.SemiBold;
        }
    }

    private void OnWidgetCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double newWidth = e.NewSize.Width;
        double newHeight = e.NewSize.Height;
        double oldWidth = e.PreviousSize.Width;
        double oldHeight = e.PreviousSize.Height;

        if (oldWidth > 200 && oldHeight > 200 && newWidth > 200 && newHeight > 200)
        {
            double scaleX = newWidth / oldWidth;
            double scaleY = newHeight / oldHeight;

            if (Math.Abs(scaleX - 1.0) > 0.01 || Math.Abs(scaleY - 1.0) > 0.01)
            {
                foreach (var widget in _widgets)
                {
                    double curLeft = Canvas.GetLeft(widget);
                    double curTop = Canvas.GetTop(widget);
                    if (double.IsNaN(curLeft)) curLeft = 20;
                    if (double.IsNaN(curTop)) curTop = 20;

                    double curW = widget.ActualWidth > 0 ? widget.ActualWidth : (double.IsNaN(widget.Width) ? 400 : widget.Width);
                    double curH = widget.ActualHeight > 0 ? widget.ActualHeight : (double.IsNaN(widget.Height) ? 300 : widget.Height);

                    double nextW = Math.Max(widget.MinWidth, curW * scaleX);
                    double nextH = Math.Max(widget.MinHeight, curH * scaleY);
                    double nextLeft = curLeft * scaleX;
                    double nextTop = curTop * scaleY;

                    if (nextW > newWidth - 20) nextW = Math.Max(widget.MinWidth, newWidth - 20);
                    if (nextH > newHeight - 20) nextH = Math.Max(widget.MinHeight, newHeight - 20);

                    double maxLeft = Math.Max(10, newWidth - nextW - 10);
                    double maxTop = Math.Max(10, newHeight - nextH - 10);

                    widget.Width = nextW;
                    widget.Height = nextH;
                    Canvas.SetLeft(widget, Math.Clamp(nextLeft, 10, maxLeft));
                    Canvas.SetTop(widget, Math.Clamp(nextTop, 10, maxTop));
                }
            }
        }

        ClampAllWidgetsWithinCanvas();
    }

    public void ClampAllWidgetsWithinCanvas()
    {
        double canvasWidth = WidgetCanvas.ActualWidth;
        double canvasHeight = WidgetCanvas.ActualHeight;
        if (canvasWidth <= 100 || canvasHeight <= 100) return;

        foreach (var widget in _widgets)
        {
            double curLeft = Canvas.GetLeft(widget);
            double curTop = Canvas.GetTop(widget);
            if (double.IsNaN(curLeft)) curLeft = 20;
            if (double.IsNaN(curTop)) curTop = 20;

            double actualWidth = widget.ActualWidth > 0 ? widget.ActualWidth : widget.Width;
            double actualHeight = widget.ActualHeight > 0 ? widget.ActualHeight : widget.Height;
            if (double.IsNaN(actualWidth) || actualWidth <= 0) actualWidth = widget.MinWidth;
            if (double.IsNaN(actualHeight) || actualHeight <= 0) actualHeight = widget.MinHeight;

            if (actualWidth > canvasWidth - 20)
            {
                widget.Width = Math.Max(widget.MinWidth, canvasWidth - 20);
                actualWidth = widget.Width;
            }

            if (actualHeight > canvasHeight - 20)
            {
                widget.Height = Math.Max(widget.MinHeight, canvasHeight - 20);
                actualHeight = widget.Height;
            }

            double maxLeft = Math.Max(10, canvasWidth - actualWidth - 10);
            double maxTop = Math.Max(10, canvasHeight - actualHeight - 10);
            Canvas.SetLeft(widget, Math.Clamp(curLeft, 10, maxLeft));
            Canvas.SetTop(widget, Math.Clamp(curTop, 10, maxTop));
        }
    }

    public void TileActiveWidgets()
    {
        if (_widgets.Count == 0) return;

        double canvasWidth = WidgetCanvas.ActualWidth;
        double canvasHeight = WidgetCanvas.ActualHeight;
        if (canvasWidth <= 200 || canvasHeight <= 200)
        {
            canvasWidth = Width - 40;
            canvasHeight = Height - 140;
        }

        int n = _widgets.Count;
        if (n == 1)
        {
            var widget = _widgets[0];
            double ww = Math.Max(widget.MinWidth, Math.Min(780, canvasWidth - 40));
            double wh = Math.Max(widget.MinHeight, Math.Min(520, canvasHeight - 40));
            Canvas.SetLeft(widget, Math.Max(20, (canvasWidth - ww) / 2));
            Canvas.SetTop(widget, Math.Max(20, (canvasHeight - wh) / 2));
            widget.Width = ww;
            widget.Height = wh;
        }
        else if (n == 2)
        {
            double halfW = (canvasWidth - 36) / 2;
            double h = Math.Max(300, canvasHeight - 40);
            for (int i = 0; i < 2; i++)
            {
                var widget = _widgets[i];
                Canvas.SetLeft(widget, 12 + i * (halfW + 12));
                Canvas.SetTop(widget, 16);
                widget.Width = Math.Max(widget.MinWidth, halfW);
                widget.Height = Math.Max(widget.MinHeight, h);
            }
        }
        else if (n == 3)
        {
            double colW = (canvasWidth - 48) / 3;
            double h = Math.Max(300, canvasHeight - 36);
            for (int i = 0; i < 3; i++)
            {
                var widget = _widgets[i];
                Canvas.SetLeft(widget, 12 + i * (colW + 12));
                Canvas.SetTop(widget, 16);
                widget.Width = Math.Max(widget.MinWidth, colW);
                widget.Height = Math.Max(widget.MinHeight, h);
            }
        }
        else if (n == 4)
        {
            double halfW = (canvasWidth - 36) / 2;
            double halfH = (canvasHeight - 36) / 2;
            for (int i = 0; i < 4; i++)
            {
                int row = i / 2;
                int col = i % 2;
                var widget = _widgets[i];
                Canvas.SetLeft(widget, 12 + col * (halfW + 12));
                Canvas.SetTop(widget, 12 + row * (halfH + 12));
                widget.Width = Math.Max(widget.MinWidth, halfW);
                widget.Height = Math.Max(widget.MinHeight, halfH);
            }
        }
        else
        {
            int cols = n <= 6 ? 3 : 4;
            int rows = (n + cols - 1) / cols;
            double cellWidth = (canvasWidth - (cols + 1) * 12) / cols;
            double cellHeight = (canvasHeight - (rows + 1) * 12) / rows;
            for (int i = 0; i < n; i++)
            {
                int row = i / cols;
                int col = i % cols;
                var widget = _widgets[i];
                Canvas.SetLeft(widget, 12 + col * (cellWidth + 12));
                Canvas.SetTop(widget, 12 + row * (cellHeight + 12));
                widget.Width = Math.Max(widget.MinWidth, cellWidth);
                widget.Height = Math.Max(widget.MinHeight, cellHeight);
            }
        }

        ClampAllWidgetsWithinCanvas();
        SaveWidgetsLayout();
    }

    private void DockToolBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            ToggleWidget(tag);
        }
    }

    private void BtnTileWidgets_Click(object sender, RoutedEventArgs e)
    {
        TileActiveWidgets();
    }

    private void BtnCloseAllWidgets_Click(object sender, RoutedEventArgs e)
    {
        ClearWidgets();
    }

    private void BtnToggleDock_Click(object sender, RoutedEventArgs e)
    {
        if (DockBody.Visibility == Visibility.Visible)
        {
            DockBody.Visibility = Visibility.Collapsed;
            BtnToggleDock.Content = "▲ 도구 바 펼치기";
        }
        else
        {
            DockBody.Visibility = Visibility.Visible;
            BtnToggleDock.Content = "▼ 도구 바 접기";
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // 0. ESC belongs to the focused control first. This handler tunnels before the control sees
            //    the key, so without this check ESC closed the widget the teacher was typing in
            //    (wheel items, memo, checklist) or the widget that owns an open drop-down.
            if (Keyboard.FocusedElement is DependencyObject focused)
            {
                if (IsInsideOpenDropDown(focused))
                {
                    return; // let the ComboBox close its list
                }

                if (focused is System.Windows.Controls.Primitives.TextBoxBase || focused is PasswordBox)
                {
                    // First ESC only leaves the text box; the next ESC closes the widget as before.
                    Keyboard.Focus(this);
                    e.Handled = true;
                    return;
                }
            }

            // 1. 보드 필기 모드가 켜져 있다면 해제
            if (ToggleInkMode != null && ToggleInkMode.IsChecked == true)
            {
                ToggleInkMode.IsChecked = false;
                e.Handled = true;
                return;
            }

            // 2. 켜져 있는 수학 교구(자, 삼각자, 각도기) 닫기
            if (CloseActiveMathTool())
            {
                e.Handled = true;
                return;
            }

            // 3. 선택된 위젯이 있다면 해당 위젯 닫기
            if (SelectedWidget != null && _widgets.Contains(SelectedWidget))
            {
                CloseWidget(SelectedWidget);
                e.Handled = true;
                return;
            }

            // 4. 선택된 위젯이 지정되지 않았으나 위젯들이 캔버스에 있다면 최상위 위젯 닫기
            var topWidget = GetTopWidget();
            if (topWidget != null)
            {
                CloseWidget(topWidget);
                e.Handled = true;
                return;
            }
        }
    }

    private static bool IsInsideOpenDropDown(DependencyObject element)
    {
        if (element is ComboBox { IsDropDownOpen: true }) return true;
        return element is ComboBoxItem item
            && ItemsControl.ItemsControlFromItemContainer(item) is ComboBox { IsDropDownOpen: true };
    }

    private bool CloseActiveMathTool()
    {
        if (_boardRuler != null)
        {
            BoardToolsCanvas.Children.Remove(_boardRuler);
            _boardRuler = null;
            return true;
        }
        if (_boardTriangle != null)
        {
            BoardToolsCanvas.Children.Remove(_boardTriangle);
            _boardTriangle = null;
            return true;
        }
        if (_boardProtractor != null)
        {
            BoardToolsCanvas.Children.Remove(_boardProtractor);
            _boardProtractor = null;
            return true;
        }
        return false;
    }

    private BoardWidgetHost? GetTopWidget()
    {
        if (_widgets.Count == 0) return null;
        return _widgets.OrderByDescending(Panel.GetZIndex).FirstOrDefault();
    }

    private void CbAddWidget_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isReady) return;
        if (CbAddWidget?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            SpawnWidget(tag);
            CbAddWidget.SelectedIndex = 0;
        }
    }

    #endregion

    #region Board Themes & Fullscreen

    private void BtnGreenBoard_Click(object sender, RoutedEventArgs e)
    {
        BoardContainer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B4332"));
        BoardInkCanvas.DefaultDrawingAttributes.Color = Colors.White;
    }

    private void BtnWhiteBoard_Click(object sender, RoutedEventArgs e)
    {
        BoardContainer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
        BoardInkCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString("#0F172A");
    }

    private void BtnDarkBoard_Click(object sender, RoutedEventArgs e)
    {
        BoardContainer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
        BoardInkCanvas.DefaultDrawingAttributes.Color = Colors.White;
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
        SaveWidgetsLayout();
        Hide();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        SaveWidgetsLayout();
        e.Cancel = true;
        Hide();
    }

    #endregion

    #region Drawing Overlay

    private void ToggleInkMode_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (PanelInkTools != null) PanelInkTools.Visibility = Visibility.Visible;
        if (BoardInkCanvas != null)
        {
            BoardInkCanvas.Visibility = Visibility.Visible;
            BoardInkCanvas.IsHitTestVisible = true;
            BoardInkCanvas.EditingMode = InkCanvasEditingMode.None;
        }
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = true;
        _eraserHelper?.SetToolMode(EraserToolMode.Pen);
        if (RbPen != null) RbPen.IsChecked = true;
    }

    private void ToggleInkMode_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (PanelInkTools != null) PanelInkTools.Visibility = Visibility.Collapsed;
        if (BoardInkCanvas != null)
        {
            BoardInkCanvas.IsHitTestVisible = false;
            BoardInkCanvas.EditingMode = InkCanvasEditingMode.None;
        }
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = false;
        _eraserHelper?.CancelInteraction();
    }

    private void RbPen_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = true;
        if (BoardInkCanvas != null) BoardInkCanvas.EditingMode = InkCanvasEditingMode.None;
        _eraserHelper?.SetToolMode(EraserToolMode.Pen);
    }

    private void RbEraserPoint_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = false;
        _eraserHelper?.SetToolMode(EraserToolMode.Point);
    }

    private void RbEraserStroke_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = false;
        _eraserHelper?.SetToolMode(EraserToolMode.Stroke);
    }

    private void RbEraserBox_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = false;
        _eraserHelper?.SetToolMode(EraserToolMode.Box);
    }

    private void RbEraserLasso_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        if (_multiTouchHelper != null) _multiTouchHelper.IsEnabled = false;
        _eraserHelper?.SetToolMode(EraserToolMode.Lasso);
    }

    private bool IsAnyEraserSelected()
    {
        return RbEraserPoint?.IsChecked == true ||
               RbEraserStroke?.IsChecked == true ||
               RbEraserBox?.IsChecked == true ||
               RbEraserLasso?.IsChecked == true;
    }

    private void BtnColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            BoardInkCanvas.DefaultDrawingAttributes.Color = color;
            if (IsAnyEraserSelected())
            {
                RbPen.IsChecked = true;
            }
        }
    }

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        _eraserHelper?.Undo();
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        // A mis-tap on a touch board must be recoverable: clearing is one undo step.
        _eraserHelper?.ClearAll(undoable: true);
    }

    #endregion

    #region Lock & Opacity Controls

    private void ToggleLockWidgets_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        _isWidgetsLocked = true;
        ApplyLockState();
    }

    private void ToggleLockWidgets_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        _isWidgetsLocked = false;
        ApplyLockState();
    }

    private void ApplyLockState()
    {
        if (ToggleLockWidgets != null)
        {
            ToggleLockWidgets.Content = _isWidgetsLocked ? "🔒 위치 잠김" : "🔓 위치 고정";
            ToggleLockWidgets.Background = _isWidgetsLocked
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
        }

        foreach (var widget in _widgets)
        {
            widget.IsLocked = _isWidgetsLocked;
        }
    }

    private void BtnOpacityPopup_Click(object sender, RoutedEventArgs e)
    {
        PopupOpacity.IsOpen = !PopupOpacity.IsOpen;
    }

    private void SliderCardOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isReady) return;
        _currentCardOpacity = e.NewValue;
        if (TxtOpacityValue != null)
        {
            TxtOpacityValue.Text = $"{(_currentCardOpacity * 100):0}%";
        }

        foreach (var widget in _widgets)
        {
            widget.CardOpacity = _currentCardOpacity;
        }
    }

    #endregion

    #region Smart Classroom Dashboard Handlers (스마트 학급 대시보드 모드)

    private DateTime _dashboardMealDate = DateTime.Today;

    private void InitDashboard()
    {
        // 1. 과제/할 일 실시간 변경 구독
        if (_taskService != null)
        {
            _taskService.TasksChanged += () =>
            {
                Dispatcher.Invoke(RefreshDashboardTasks);
            };
        }

        // 2. 시간표 변경 구독
        if (_timetableService != null)
        {
            _timetableService.OnTimetableChanged += () =>
            {
                Dispatcher.Invoke(RefreshDashboardTimetable);
            };
        }

        // 3. 알림장 실시간 메모 변경 구독
        MemoWidgetView.OnNoticeChanged += (newText, sender) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (TxtDashboardNotice != null) TxtDashboardNotice.Text = newText;
            });
        };

        // 4. 시계 타이머에 대시보드 시계 및 교시 상태 연동
        _clockTimer.Tick += (s, e) =>
        {
            if (TxtDashboardClock != null) TxtDashboardClock.Text = DateTime.Now.ToString("HH:mm:ss");
            UpdateDashboardPeriodStatus();
        };

        RefreshDashboardData();
    }

    public void RefreshDashboardData()
    {
        if (TxtDashboardClock != null) TxtDashboardClock.Text = DateTime.Now.ToString("HH:mm:ss");
        if (TxtDashboardDate != null) TxtDashboardDate.Text = DateTime.Now.ToString("yyyy년 M월 d일 dddd");

        UpdateDashboardPeriodStatus();
        RefreshDashboardTimetable();
        RefreshDashboardTasks();
        _ = LoadDashboardMealAsync(_dashboardMealDate);
        RefreshDashboardDDayAndNotice();
    }

    private void RefreshDashboardTimetable()
    {
        if (_timetableService == null) return;
        var schedule = _timetableService.GetTodaySchedule();
        if (DashboardListPeriods != null)
        {
            DashboardListPeriods.ItemsSource = null;
            DashboardListPeriods.ItemsSource = schedule;
        }
        if (TxtDashboardDayOfWeek != null)
        {
            TxtDashboardDayOfWeek.Text = $"({DateTime.Today:ddd})";
        }
        if (TxtDashboardPeriodSummary != null)
        {
            int count = schedule?.Count ?? 0;
            TxtDashboardPeriodSummary.Text = count > 0 ? $"총 {count}교시 수업 진행 예정" : "오늘 등록된 수업이 없습니다.";
        }
    }

    private void UpdateDashboardPeriodStatus()
    {
        if (TxtDashboardPeriod == null) return;

        var now = DateTime.Now.TimeOfDay;
        var schedule = _timetableService?.GetTodaySchedule();

        if (schedule != null && schedule.Count > 0)
        {
            PeriodItem? active = null;
            foreach (var item in schedule)
            {
                if (TimeSpan.TryParse(item.Start, out var start) && TimeSpan.TryParse(item.End, out var end))
                {
                    if (now >= start && now <= end)
                    {
                        active = item;
                        item.IsCurrentPeriod = true;
                    }
                    else
                    {
                        item.IsCurrentPeriod = false;
                    }
                }
            }

            if (active != null)
            {
                TxtDashboardPeriod.Text = $"🔔 {active.Period}교시 {active.Subject} 수업 중 ({active.TimeRange})";
                if (BdDashboardPeriod != null) BdDashboardPeriod.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                return;
            }

            // 수업 전후 또는 쉬는 시간 체크
            if (TimeSpan.TryParse(schedule[0].Start, out var firstStart) && now < firstStart)
            {
                TxtDashboardPeriod.Text = $"🌅 아침 조회 및 1교시 수업 준비 중";
                if (BdDashboardPeriod != null) BdDashboardPeriod.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                return;
            }

            if (TimeSpan.TryParse(schedule[^1].End, out var lastEnd) && now > lastEnd)
            {
                TxtDashboardPeriod.Text = $"🏡 오늘의 정규 수업이 종료되었습니다";
                if (BdDashboardPeriod != null) BdDashboardPeriod.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                return;
            }

            TxtDashboardPeriod.Text = "☕ 쉬는 시간 (다음 교시 준비)";
            if (BdDashboardPeriod != null) BdDashboardPeriod.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
        }
        else
        {
            TxtDashboardPeriod.Text = "🏫 즐거운 학교생활";
            if (BdDashboardPeriod != null) BdDashboardPeriod.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
        }
    }

    private void RefreshDashboardTasks()
    {
        if (_taskService == null) return;

        var today = _taskService.GetTodayTasks();
        if (DashboardListTodayTasks != null)
        {
            DashboardListTodayTasks.ItemsSource = null;
            DashboardListTodayTasks.ItemsSource = today;
        }
        if (TxtDashboardTodayCount != null)
        {
            int pending = today.Count(t => !t.IsCompleted);
            int done = today.Count(t => t.IsCompleted);
            TxtDashboardTodayCount.Text = $"진행 {pending}건 / 완료 {done}건";
        }
        if (TxtDashboardTodayEmpty != null)
        {
            TxtDashboardTodayEmpty.Visibility = today.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        var weekly = _taskService.GetWeeklyTasks();
        if (DashboardListWeeklyTasks != null)
        {
            DashboardListWeeklyTasks.ItemsSource = null;
            DashboardListWeeklyTasks.ItemsSource = weekly;
        }
        if (TxtDashboardWeeklyCount != null)
        {
            int pending = weekly.Count(t => !t.IsCompleted);
            int done = weekly.Count(t => t.IsCompleted);
            TxtDashboardWeeklyCount.Text = $"진행 {pending}건 / 완료 {done}건";
        }
        if (TxtDashboardWeeklyEmpty != null)
        {
            TxtDashboardWeeklyEmpty.Visibility = weekly.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private async Task LoadDashboardMealAsync(DateTime date)
    {
        _dashboardMealDate = date;
        if (_neisService == null || TxtDashboardMealMenu == null) return;

        bool isToday = date.Date == DateTime.Today;
        string dateLabel = isToday ? $"오늘 ({date:M.d} {date:ddd})" : $"{date:M.d} ({date:ddd})";

        TxtDashboardMealMenu.Text = $"{dateLabel} 급식 식단을 가져오는 중...";
        if (TxtDashboardMealCalorie != null) TxtDashboardMealCalorie.Text = "열량 계산 중";

        try
        {
            var meal = await _neisService.GetMealAsync(date);
            if (meal != null && !string.IsNullOrWhiteSpace(meal.MenuText))
            {
                TxtDashboardMealMenu.Text = meal.MenuText;
                if (TxtDashboardMealCalorie != null)
                {
                    TxtDashboardMealCalorie.Text = string.IsNullOrEmpty(meal.Calorie) ? $"{dateLabel}" : $"{meal.Calorie}";
                }
            }
            else
            {
                TxtDashboardMealMenu.Text = $"{dateLabel}\n등록된 급식 식단이 없습니다.\n(주말, 휴업일 또는 미등록)";
                if (TxtDashboardMealCalorie != null) TxtDashboardMealCalorie.Text = "- kcal";
            }
        }
        catch
        {
            TxtDashboardMealMenu.Text = "급식 정보를 불러올 수 없습니다.\n나이스 학교 설정 또는 인터넷 상태를 확인해 주세요.";
            if (TxtDashboardMealCalorie != null) TxtDashboardMealCalorie.Text = "- kcal";
        }
    }

    private void RefreshDashboardDDayAndNotice()
    {
        // 1. D-Day
        var cfg = _configService.DDayConfig;
        if (cfg != null)
        {
            if (TxtDashboardDDayTitle != null)
            {
                TxtDashboardDDayTitle.Text = string.IsNullOrWhiteSpace(cfg.Title) ? "학기 목표" : cfg.Title;
            }
            if (TxtDashboardDDayCount != null)
            {
                int diff = (cfg.TargetDate.Date - DateTime.Today).Days;
                if (diff == 0) TxtDashboardDDayCount.Text = "D-Day";
                else if (diff > 0) TxtDashboardDDayCount.Text = $"D-{diff}";
                else TxtDashboardDDayCount.Text = $"D+{Math.Abs(diff)}";
            }
        }

        // 2. 알림장
        try
        {
            string noticePath = Path.Combine(_configService.ConfigDir, "board_memo.txt");
            if (SafeLocalFileStore.TryReadAllTextWithBackup(noticePath, out string text))
            {
                if (TxtDashboardNotice != null) TxtDashboardNotice.Text = text;
            }
        }
        catch { }
    }

    public void SetDisplayMode(bool isDashboard)
    {
        if (!_isReady || PanelStudentDashboard == null || WidgetCanvas == null) return;

        if (isDashboard)
        {
            if (RbBoardModeDashboard != null && RbBoardModeDashboard.IsChecked != true)
            {
                RbBoardModeDashboard.IsChecked = true;
            }
            if (TxtBoardTitle != null) TxtBoardTitle.Text = "🏫 학생 화면";
            PanelStudentDashboard.Visibility = Visibility.Visible;
            WidgetCanvas.Visibility = Visibility.Collapsed;
            EmptyBoardHint.Visibility = Visibility.Collapsed;
            if (DockBody != null) DockBody.Visibility = Visibility.Collapsed;
            RefreshDashboardData();
        }
        else
        {
            if (RbBoardModeWidgets != null && RbBoardModeWidgets.IsChecked != true)
            {
                RbBoardModeWidgets.IsChecked = true;
            }
            if (TxtBoardTitle != null) TxtBoardTitle.Text = "🧩 놀보드";
            PanelStudentDashboard.Visibility = Visibility.Collapsed;
            WidgetCanvas.Visibility = Visibility.Visible;
            if (DockBody != null) DockBody.Visibility = Visibility.Visible;
            UpdateEmptyHint();
        }
    }

    private void RbBoardMode_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isReady) return;
        SetDisplayMode(RbBoardModeDashboard?.IsChecked == true);
    }

    private void StudentTaskCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is ClassroomTaskItem item)
        {
            _taskService?.ToggleTaskCompletion(item.Id);
        }
    }

    private void BtnDashboardEditTimetable_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = new WeeklyTimetableWindow(_timetableService);
            win.Owner = this;
            win.ShowDialog();
            RefreshDashboardTimetable();
        }
        catch { }
    }

    private async void BtnDashboardPrevMeal_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardMealAsync(_dashboardMealDate.AddDays(-1));
    }

    private async void BtnDashboardTodayMeal_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardMealAsync(DateTime.Today);
    }

    private async void BtnDashboardNextMeal_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardMealAsync(_dashboardMealDate.AddDays(1));
    }

    private void StudentTaskCheckbox_PreviewTouchDown(object sender, TouchEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is ClassroomTaskItem item)
        {
            _taskService?.ToggleTaskCompletion(item.Id);
            e.Handled = true; // 터치 승격(마우스 변환) 차단하여 교탁 PC 마우스 커서 보호
        }
    }

    private ClassroomWebBrowserWindow? _browserWindow;

    private void BtnOpenWebBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_browserWindow == null || !_browserWindow.IsLoaded)
            {
                _browserWindow = new ClassroomWebBrowserWindow(_configService, _displayManager);
                _browserWindow.Owner = this;
                _browserWindow.Closed += (s, ev) => _browserWindow = null;
                _browserWindow.Show();
            }
            else
            {
                _browserWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"웹 브라우저를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnOpenPowerPoint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "무간섭 파워포인트 슬라이드 쇼 파일 선택",
                Filter = "PowerPoint 파일 (*.pptx;*.ppt;*.ppsx;*.pps)|*.pptx;*.ppt;*.ppsx;*.pps|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                Rect? targetBounds = null;
                var screens = System.Windows.Forms.Screen.AllScreens;
                if (screens.Length > 1)
                {
                    var s2 = screens[1].Bounds;
                    targetBounds = new Rect(s2.X, s2.Y, s2.Width, s2.Height);
                }

                var (success, msg) = PowerPointPresentationHelper.StartNonIntrusiveSlideShow(dlg.FileName, targetBounds);
                MessageBox.Show(msg, success ? "무간섭 PPT 재생" : "PPT 실행 안내", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"파워포인트를 실행할 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPptPrevSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.PreviousSlide();
    }

    private void BtnPptNextSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.NextSlide();
    }

    private void BtnPptCloseSlide_Click(object sender, RoutedEventArgs e)
    {
        PowerPointPresentationHelper.CloseSlideShow();
    }

    private void BtnPptShowRemoteWindow_Click(object sender, RoutedEventArgs e)
    {
        PptRemoteControllerWindow.Instance.OpenForPresentation(PowerPointPresentationHelper.CurrentPresentationTitle);
    }

    #endregion

    #region Win32 NoActivate Shield (교탁 PC 마우스 납치 및 키보드 포커스 스틸 원천 차단)

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnableNoActivateStyle();
    }

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
            Debug.WriteLine($"[StudentDisplayWindow] EnableNoActivateStyle failed: {ex.Message}");
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
