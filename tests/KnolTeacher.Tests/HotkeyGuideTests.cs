using System.Collections.Generic;
using System.IO;
using System.Linq;
using KnolTeacher.Desktop.Models;
using Xunit;

namespace KnolTeacher.Tests;

public class HotkeyGuideTests
{
    [Theory]
    [InlineData("magnifier")]
    [InlineData("recorder")]
    [InlineData("snip")]
    [InlineData("")]
    [InlineData(null)]
    public void UnimplementedActions_AreNeverRegisteredAsGlobalHotkeys(string? action)
        => Assert.False(DefaultHotkeys.IsSupportedAction(action));

    [Fact]
    public void EveryEnabledDefaultHotkey_IsAnImplementedAction()
    {
        foreach (var item in DefaultHotkeys.GetDefaults().Where(h => h.Enabled))
        {
            Assert.True(DefaultHotkeys.IsSupportedAction(item.Action), $"Enabled default hotkey has no handler: {item.Action}");
        }
    }

    [Fact]
    public void EverySupportedAction_HasAHandlerInTheAppHotkeySwitch()
    {
        string appSource = File.ReadAllText(RepositoryPaths.GetFile("src", "KnolTeacher.Desktop", "App.xaml.cs"));

        foreach (string action in DefaultHotkeys.SupportedActions)
        {
            Assert.Contains($"case \"{action}\":", appSource);
        }
    }

    [Fact]
    public void Guide_ListsOnlyEnabledImplementedShortcuts_FromCurrentSettings()
    {
        var hotkeys = new List<HotkeyItem>
        {
            new() { Id = 1, Action = "magnifier", Name = "화면 돋보기", Modifier = "Alt", Key = "1", Enabled = true },
            new() { Id = 8, Action = "picker", Name = "뽑기 레이스", Modifier = "Alt", Key = "8", Enabled = true },
            new() { Id = 10, Action = "board_f2", Name = "놀보드 (F2)", Modifier = "None", Key = "F2", Enabled = true },
            new() { Id = 13, Action = "noise", Name = "교실 소음 신호등", Modifier = "Ctrl+Alt", Key = "N", Enabled = true },
            new() { Id = 14, Action = "soundboard", Name = "교실 효과음 보드", Modifier = "Alt", Key = "B", Enabled = false }
        };

        var lines = DefaultHotkeys.BuildGuideLines(hotkeys);

        Assert.Collection(
            lines,
            line => Assert.Equal("• [Alt + 8] 뽑기 레이스", line),
            line => Assert.Equal("• [F2] 놀보드 (F2)", line),
            line => Assert.Equal("• [Ctrl + Alt + N] 교실 소음 신호등", line));
    }

    [Fact]
    public void Guide_FallsBackToDefaults_WhenSettingsAreMissing()
    {
        var lines = DefaultHotkeys.BuildGuideLines(null);

        Assert.Contains("• [Alt + 8] 뽑기 레이스", lines);
        Assert.Contains("• [F2] 놀보드 (F2)", lines);
        Assert.DoesNotContain(lines, line => line.Contains("화면 돋보기"));
    }
}
