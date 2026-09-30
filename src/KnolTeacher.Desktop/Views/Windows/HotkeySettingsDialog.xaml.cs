using System;
using System.Collections.Generic;
using System.Windows;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Services;

namespace KnolTeacher.Desktop.Views.Windows;

public partial class HotkeySettingsDialog : Window
{
    private readonly IConfigService _configService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private List<HotkeyItem> _editingList;

    // Entries for tools that are not available yet are kept in the settings file unchanged but not
    // shown, so the list only offers shortcuts that actually do something.
    private List<HotkeyItem> _reservedItems;

    public HotkeySettingsDialog(IConfigService configService, IGlobalHotkeyService hotkeyService)
    {
        _configService = configService;
        _hotkeyService = hotkeyService;
        InitializeComponent();

        var all = CloneList(_configService.Hotkeys);
        _editingList = all.Where(item => DefaultHotkeys.IsSupportedAction(item.Action)).ToList();
        _reservedItems = all.Where(item => !DefaultHotkeys.IsSupportedAction(item.Action)).ToList();
        ListHotkeys.ItemsSource = _editingList;
    }

    private List<HotkeyItem> CloneList(List<HotkeyItem> source)
    {
        var result = new List<HotkeyItem>();
        foreach (var item in source)
        {
            result.Add(new HotkeyItem
            {
                Id = item.Id,
                Action = item.Action,
                Name = item.Name,
                Description = item.Description,
                Modifier = item.Modifier,
                Key = item.Key,
                Enabled = item.Enabled
            });
        }
        return result;
    }

    private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "단축키를 초기 기본값(Alt+숫자, F2 등)으로 복원하시겠습니까?", "초기화 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var defaults = DefaultHotkeys.GetDefaults();
            _editingList = defaults.Where(item => DefaultHotkeys.IsSupportedAction(item.Action)).ToList();
            _reservedItems = defaults.Where(item => !DefaultHotkeys.IsSupportedAction(item.Action)).ToList();
            ListHotkeys.ItemsSource = null;
            ListHotkeys.ItemsSource = _editingList;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _configService.Hotkeys = _editingList.Concat(_reservedItems).OrderBy(item => item.Id).ToList();
        _configService.SaveHotkeys();
        _hotkeyService.ReloadHotkeys();

        HudNotificationWindow.Instance.ShowToast("⌨️", "단축키 설정이 성공적으로 저장 및 반영되었습니다.");
        DialogResult = true;
        Close();
    }
}
