using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Services;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class WeeklyTimetableWindow : Window
{
    private readonly ITimetableService _timetableService;
    private bool _isBaseMode = false;
    private bool _isSwapMode = false;
    private (string dayKey, int lessonIndex, Border border)? _selectedForSwap = null;

    private static readonly string[] DayKeys = { "mon", "tue", "wed", "thu", "fri" };
    private static readonly string[] DayNames = { "월요일", "화요일", "수요일", "목요일", "금요일" };

    private static readonly string[] SubjectPresets =
    {
        "국어", "수학", "사회", "과학", "영어", "체육", "음악", "미술", "도덕", "실과", "창체", "동아리", "자율", "진로", "보건"
    };

    // Working copy of timetables during editing
    private Dictionary<string, List<Dictionary<string, string>>> _workingWeekly = new();
    private Dictionary<string, List<Dictionary<string, string>>> _workingBase = new();

    public WeeklyTimetableWindow(ITimetableService timetableService)
    {
        _timetableService = timetableService ?? throw new ArgumentNullException(nameof(timetableService));
        InitializeComponent();

        _workingWeekly = _timetableService.GetWeeklyTimetable();
        _workingBase = _timetableService.GetBaseTimetable();

        Loaded += (s, e) => RenderTable();
    }

    private Dictionary<string, List<Dictionary<string, string>>> CurrentTable =>
        _isBaseMode ? _workingBase : _workingWeekly;

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (RbWeeklyTab == null || RbBaseTab == null) return;

        _isBaseMode = RbBaseTab.IsChecked == true;
        _selectedForSwap = null;

        if (BtnResetToBase != null) BtnResetToBase.Visibility = _isBaseMode ? Visibility.Collapsed : Visibility.Visible;
        if (BtnSaveAsBase != null) BtnSaveAsBase.Visibility = _isBaseMode ? Visibility.Collapsed : Visibility.Visible;
        if (BtnToggleSwapMode != null) BtnToggleSwapMode.Visibility = _isBaseMode ? Visibility.Collapsed : Visibility.Visible;

        RenderTable();
    }

    private void RenderTable()
    {
        if (GridTimetableTable == null) return;
        GridTimetableTable.Children.Clear();
        GridTimetableTable.RowDefinitions.Clear();

        var periods = _timetableService.GetPeriods();
        int rowIndex = 0;

        foreach (var p in periods)
        {
            if (p.Period == 7 && !_timetableService.Settings.EnablePeriod7) continue;

            GridTimetableTable.RowDefinitions.Add(new RowDefinition { Height = new GridLength(p.IsLunch ? 44 : 64) });

            // 0 Column: Period Name & Time
            var timeBorder = new Border
            {
                Background = p.IsLunch ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"))
                                       : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(2),
                Padding = new Thickness(6, 4, 6, 4)
            };

            var timeStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            timeStack.Children.Add(new TextBlock
            {
                Text = p.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = p.IsLunch ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"))
                                       : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            timeStack.Children.Add(new TextBlock
            {
                Text = $"{p.Start}~{p.End}",
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            timeBorder.Child = timeStack;
            Grid.SetRow(timeBorder, rowIndex);
            Grid.SetColumn(timeBorder, 0);
            GridTimetableTable.Children.Add(timeBorder);

            if (p.IsLunch)
            {
                // Lunch spans columns 1 to 5
                var lunchBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(2),
                    Padding = new Thickness(8)
                };
                var lunchText = new TextBlock
                {
                    Text = "🍱 점심식사 및 자유 휴식시간 (12:20 ~ 13:20)",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                lunchBorder.Child = lunchText;
                Grid.SetRow(lunchBorder, rowIndex);
                Grid.SetColumn(lunchBorder, 1);
                Grid.SetColumnSpan(lunchBorder, 5);
                GridTimetableTable.Children.Add(lunchBorder);
            }
            else
            {
                int lessonIdx = p.Period <= 4 ? p.Period - 1 : p.Period - 1; // 1~4 => 0~3, 5~7 => 4~6
                if (p.Period >= 5) lessonIdx = p.Period - 1;

                for (int d = 0; d < DayKeys.Length; d++)
                {
                    string dayKey = DayKeys[d];
                    string subject = "";
                    string tag = "담임";

                    if (CurrentTable.TryGetValue(dayKey, out var list) && lessonIdx < list.Count)
                    {
                        subject = list[lessonIdx].TryGetValue("subject", out var s) ? s : "";
                        tag = list[lessonIdx].TryGetValue("tag", out var t) ? t : "담임";
                    }

                    var cellBorder = CreateSubjectCell(dayKey, lessonIdx, subject, tag, p.Period);
                    Grid.SetRow(cellBorder, rowIndex);
                    Grid.SetColumn(cellBorder, d + 1);
                    GridTimetableTable.Children.Add(cellBorder);
                }
            }

            rowIndex++;
        }
    }

    private Border CreateSubjectCell(string dayKey, int lessonIndex, string subject, string tag, int periodNumber)
    {
        bool isSelectedForSwap = _selectedForSwap.HasValue &&
                                 _selectedForSwap.Value.dayKey == dayKey &&
                                 _selectedForSwap.Value.lessonIndex == lessonIndex;

        var border = new Border
        {
            Background = isSelectedForSwap ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"))
                                           : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
            BorderBrush = isSelectedForSwap ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"))
                                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
            BorderThickness = new Thickness(isSelectedForSwap ? 2.5 : 1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(2),
            Padding = new Thickness(6, 4, 6, 4),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "클릭하여 과목 변경 (수업 맞바꾸기 모드 시 두 셀을 클릭하여 교환)"
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var subjectBlock = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(subject) ? "-" : subject,
            FontWeight = FontWeights.Bold,
            FontSize = 13.5,
            Foreground = string.IsNullOrWhiteSpace(subject)
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(subjectBlock);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var tagBadge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(0, 3, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = tag switch
                {
                    "전담" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF2FF")),
                    "외강" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECFDF5"))
                }
            };
            tagBadge.Child = new TextBlock
            {
                Text = tag,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = tag switch
                {
                    "전담" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F46E5")),
                    "외강" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"))
                }
            };
            stack.Children.Add(tagBadge);
        }

        border.Child = stack;

        border.MouseLeftButtonUp += (s, e) =>
        {
            OnCellClicked(dayKey, lessonIndex, border, subject, tag, periodNumber);
        };

        return border;
    }

    private void OnCellClicked(string dayKey, int lessonIndex, Border border, string subject, string tag, int periodNumber)
    {
        if (_isSwapMode)
        {
            if (!_selectedForSwap.HasValue)
            {
                // Select first cell
                _selectedForSwap = (dayKey, lessonIndex, border);
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                border.BorderThickness = new Thickness(2.5);
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                if (TxtSwapModeLabel != null) TxtSwapModeLabel.Text = "맞바꿀 두 번째 수업을 클릭하세요...";
            }
            else
            {
                var first = _selectedForSwap.Value;
                if (first.dayKey == dayKey && first.lessonIndex == lessonIndex)
                {
                    // Cancel selection
                    _selectedForSwap = null;
                    if (TxtSwapModeLabel != null) TxtSwapModeLabel.Text = "수업 맞바꾸기(스왑)";
                    RenderTable();
                    return;
                }

                // Swap in working table
                SwapInWorkingTable(first.dayKey, first.lessonIndex, dayKey, lessonIndex);
                _selectedForSwap = null;
                if (TxtSwapModeLabel != null) TxtSwapModeLabel.Text = "수업 맞바꾸기(스왑)";
                RenderTable();
            }
        }
        else
        {
            // Open Subject Edit Quick Popup
            ShowSubjectEditModal(dayKey, lessonIndex, subject, tag, periodNumber);
        }
    }

    private void SwapInWorkingTable(string dayA, int lessonA, string dayB, int lessonB)
    {
        var table = CurrentTable;
        if (!table.ContainsKey(dayA)) table[dayA] = new();
        if (!table.ContainsKey(dayB)) table[dayB] = new();

        var listA = table[dayA];
        var listB = table[dayB];

        while (listA.Count <= lessonA) listA.Add(new() { ["subject"] = "", ["tag"] = "담임" });
        while (listB.Count <= lessonB) listB.Add(new() { ["subject"] = "", ["tag"] = "담임" });

        var temp = listA[lessonA];
        listA[lessonA] = listB[lessonB];
        listB[lessonB] = temp;
    }

    private void ShowSubjectEditModal(string dayKey, int lessonIndex, string currentSubject, string currentTag, int periodNumber)
    {
        int dayIdx = Array.IndexOf(DayKeys, dayKey);
        string dayName = dayIdx >= 0 ? DayNames[dayIdx] : dayKey;

        var win = new Window
        {
            Title = $"{dayName} {periodNumber}교시 과목 변경",
            Width = 380,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
            ResizeMode = ResizeMode.NoResize
        };

        var rootGrid = new Grid { Margin = new Thickness(16) };
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Title
        var titleBlock = new TextBlock
        {
            Text = $"✏️ {dayName} {periodNumber}교시 과목 선택",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(titleBlock, 0);
        rootGrid.Children.Add(titleBlock);

        // Input & Tag Row
        var inputStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var tbSubject = new TextBox
        {
            Text = currentSubject == "-" ? "" : currentSubject,
            Width = 170,
            Height = 32,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(6, 4, 6, 4),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        inputStack.Children.Add(tbSubject);

        var cbTag = new ComboBox { Width = 90, Height = 32, Margin = new Thickness(8, 0, 0, 0), FontSize = 12 };
        cbTag.Items.Add("담임");
        cbTag.Items.Add("전담");
        cbTag.Items.Add("외강");
        cbTag.SelectedItem = currentTag;
        inputStack.Children.Add(cbTag);

        Grid.SetRow(inputStack, 1);
        rootGrid.Children.Add(inputStack);

        // Quick Preset WrapPanel
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var wrap = new WrapPanel { Margin = new Thickness(0, 4, 0, 8) };

        foreach (var sub in SubjectPresets)
        {
            var btn = new Button
            {
                Content = sub,
                Width = 56,
                Height = 28,
                Margin = new Thickness(3),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += (s, e) => tbSubject.Text = sub;
            wrap.Children.Add(btn);
        }

        // Empty Button
        var btnEmpty = new Button
        {
            Content = "비우기(-)",
            Width = 66,
            Height = 28,
            Margin = new Thickness(3),
            FontSize = 11,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnEmpty.Click += (s, e) => tbSubject.Text = "";
        wrap.Children.Add(btnEmpty);

        scroll.Content = wrap;
        Grid.SetRow(scroll, 2);
        rootGrid.Children.Add(scroll);

        // Buttons
        var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var btnCancel = new Button { Content = "취소", Width = 70, Height = 32, Margin = new Thickness(0, 0, 6, 0), Background = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")) };
        btnCancel.Click += (s, e) => win.Close();

        var btnSave = new Button { Content = "적용", Width = 70, Height = 32, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")), Foreground = Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0) };
        btnSave.Click += (s, e) =>
        {
            string newSub = tbSubject.Text.Trim();
            string newTag = cbTag.SelectedItem?.ToString() ?? "담임";

            var table = CurrentTable;
            if (!table.ContainsKey(dayKey)) table[dayKey] = new();
            var list = table[dayKey];
            while (list.Count <= lessonIndex) list.Add(new() { ["subject"] = "", ["tag"] = "담임" });
            list[lessonIndex] = new Dictionary<string, string> { ["subject"] = newSub, ["tag"] = newTag };

            win.Close();
            RenderTable();
        };

        btnStack.Children.Add(btnCancel);
        btnStack.Children.Add(btnSave);
        Grid.SetRow(btnStack, 3);
        rootGrid.Children.Add(btnStack);

        win.Content = rootGrid;
        win.ShowDialog();
    }

    private void BtnToggleSwapMode_Click(object sender, RoutedEventArgs e)
    {
        _isSwapMode = BtnToggleSwapMode.IsChecked == true;
        _selectedForSwap = null;

        if (_isSwapMode)
        {
            BtnToggleSwapMode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
            BtnToggleSwapMode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            if (TxtSwapModeLabel != null) TxtSwapModeLabel.Text = "맞바꿀 첫 번째 수업을 클릭하세요...";
        }
        else
        {
            BtnToggleSwapMode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            BtnToggleSwapMode.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            if (TxtSwapModeLabel != null) TxtSwapModeLabel.Text = "수업 맞바꾸기(스왑)";
        }

        RenderTable();
    }

    private void BtnResetToBase_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "이번 주 임의 변경 내용을 모두 취소하고,\n학기 기본 표준 시간표로 되돌리시겠습니까?",
            "기본 시간표로 복원",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _workingWeekly = _timetableService.GetBaseTimetable();
            RenderTable();
            MessageBox.Show("기본 시간표로 복원되었습니다. [저장 및 적용]을 눌러 반영하세요.", "복원 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnSaveAsBase_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "현재 이번 주 시간표 배치를\n앞으로 적용될 '기본 표준 시간표'로 등록하시겠습니까?",
            "기본 시간표로 등록",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _workingBase = _timetableService.GetWeeklyTimetable();
            _timetableService.SaveBaseTimetable(_workingBase);
            MessageBox.Show("현재 시간표가 기본 표준 시간표로 안전하게 등록되었습니다.", "등록 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnApplyAndClose_Click(object sender, RoutedEventArgs e)
    {
        if (_isBaseMode)
        {
            _timetableService.SaveBaseTimetable(_workingBase);
        }
        else
        {
            _timetableService.SaveWeeklyTimetable(_workingWeekly);
        }

        DialogResult = true;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
