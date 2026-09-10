using System;
using System.IO;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public sealed class TimetableServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "KnolTeacher.Timetable.Tests",
        Guid.NewGuid().ToString("N"));

    public TimetableServiceTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch { }
    }

    [Fact]
    public void BaseAndWeeklyTimetables_InitializeAndPersistSeparately()
    {
        var config = new ConfigService(_root);
        var timetable = new TimetableService(config);

        var weekly = timetable.GetWeeklyTimetable();
        var baseTable = timetable.GetBaseTimetable();

        Assert.NotEmpty(weekly);
        Assert.NotEmpty(baseTable);
        Assert.True(weekly.ContainsKey("mon"));
        Assert.True(baseTable.ContainsKey("mon"));

        // Modifying weekly should not affect base table
        timetable.UpdatePeriodSubject("mon", 0, "인공지능", "전담");

        var updatedWeekly = timetable.GetWeeklyTimetable();
        var currentBase = timetable.GetBaseTimetable();

        Assert.Equal("인공지능", updatedWeekly["mon"][0]["subject"]);
        Assert.NotEqual("인공지능", currentBase["mon"][0]["subject"]);
    }

    [Fact]
    public void ResetWeeklyToBase_RestoresOriginalBaseSchedule()
    {
        var config = new ConfigService(_root);
        var timetable = new TimetableService(config);

        var originalBase = timetable.GetBaseTimetable();
        string baseMon1 = originalBase["mon"][0]["subject"];

        timetable.UpdatePeriodSubject("mon", 0, "특별활동", "담임");
        Assert.Equal("특별활동", timetable.GetWeeklyTimetable()["mon"][0]["subject"]);

        timetable.ResetWeeklyToBase();
        Assert.Equal(baseMon1, timetable.GetWeeklyTimetable()["mon"][0]["subject"]);
    }

    [Fact]
    public void SwapPeriods_SwapsTwoLessonsInSameDay()
    {
        var config = new ConfigService(_root);
        var timetable = new TimetableService(config);

        timetable.UpdatePeriodSubject("mon", 0, "수학", "담임");
        timetable.UpdatePeriodSubject("mon", 1, "과학", "전담");

        timetable.SwapPeriods("mon", 0, 1);

        var weekly = timetable.GetWeeklyTimetable();
        Assert.Equal("과학", weekly["mon"][0]["subject"]);
        Assert.Equal("수학", weekly["mon"][1]["subject"]);
    }

    [Fact]
    public void SwapAcrossDays_SwapsLessonsAcrossDifferentDays()
    {
        var config = new ConfigService(_root);
        var timetable = new TimetableService(config);

        timetable.UpdatePeriodSubject("mon", 2, "미술", "전담");
        timetable.UpdatePeriodSubject("tue", 3, "체육", "외강");

        timetable.SwapAcrossDays("mon", 2, "tue", 3);

        var weekly = timetable.GetWeeklyTimetable();
        Assert.Equal("체육", weekly["mon"][2]["subject"]);
        Assert.Equal("미술", weekly["tue"][3]["subject"]);
    }
}
