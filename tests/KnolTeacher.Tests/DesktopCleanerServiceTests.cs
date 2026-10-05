using System;
using System.Collections.Generic;
using System.IO;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public sealed class DesktopCleanerServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "KnolTeacher.Cleaner.Tests",
        Guid.NewGuid().ToString("N"));

    public DesktopCleanerServiceTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }
        catch { }
    }

    [Fact]
    public void UndoOrganize_WhenNoHistoryExists_ReturnsZeroWithoutCrash()
    {
        var config = new ConfigService(_root);
        var cleaner = new DesktopCleanerService(config);

        var result = cleaner.UndoOrganize();

        Assert.False(result.Success);
        Assert.Equal(0, result.RestoredCount);
        Assert.Contains("되돌릴 직전 정리 기록이 없습니다", result.Message);
    }

    [Fact]
    public void CorruptedHistoryFile_LoadsSafelyWithoutException()
    {
        string historyPath = Path.Combine(_root, "desktop_organize_history.json");
        File.WriteAllText(historyPath, "{ broken json content !!");

        var config = new ConfigService(_root);
        var cleaner = new DesktopCleanerService(config);

        var result = cleaner.UndoOrganize();
        Assert.False(result.Success);
        Assert.Equal(0, result.RestoredCount);
    }
}
