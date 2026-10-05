using System;
using System.IO;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public sealed class StudentManagerServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "KnolTeacher.Student.Tests",
        Guid.NewGuid().ToString("N"));

    public StudentManagerServiceTests()
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
    public void ConfigureNumberOnlyRoster_FiresRosterChangedEvent()
    {
        var config = new ConfigService(_root);
        var manager = new StudentManagerService(config);

        bool eventFired = false;
        manager.RosterChanged += () => eventFired = true;

        manager.ConfigureNumberOnlyRoster(15);

        Assert.True(eventFired);
        Assert.Equal(15, manager.Students.Count);
    }

    [Fact]
    public void SaveRoster_FiresRosterChangedEvent()
    {
        var config = new ConfigService(_root);
        var manager = new StudentManagerService(config);

        bool eventFired = false;
        manager.RosterChanged += () => eventFired = true;

        manager.SaveRoster();

        Assert.True(eventFired);
    }

    [Fact]
    public void UpdateStudentAvatar_FiresRosterChangedEvent()
    {
        var config = new ConfigService(_root);
        var manager = new StudentManagerService(config);
        manager.ConfigureNumberOnlyRoster(10);

        bool eventFired = false;
        manager.RosterChanged += () => eventFired = true;

        manager.UpdateStudentAvatar(1, "avatar_05");

        Assert.True(eventFired);
        Assert.Equal("avatar_05", manager.Students[0].AvatarId);
    }
}
