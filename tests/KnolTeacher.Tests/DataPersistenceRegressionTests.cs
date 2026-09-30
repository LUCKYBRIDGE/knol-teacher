using System;
using System.IO;
using System.Linq;
using System.Text;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public sealed class DataPersistenceRegressionTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "KnolTeacher.DataPersistence.Tests",
        Guid.NewGuid().ToString("N"));

    public DataPersistenceRegressionTests()
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
    public void ImportTimetableCsv_UpdatesTheRunningService_AndSurvivesTheNextEdit()
    {
        var config = new ConfigService(_root);
        var timetable = new TimetableService(config);
        var share = new DataShareService(config, new StudentManagerService(config), timetable, new AcademicCalendarService(config));

        string csvPath = Path.Combine(_root, "timetable.csv");
        File.WriteAllText(
            csvPath,
            "교시,월,화,수,목,금\n1,코딩,국어,수학,영어,사회\n2,미술,수학,국어,과학,체육\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        bool changed = false;
        timetable.OnTimetableChanged += () => changed = true;

        var result = share.ImportTimetableCsv(csvPath);

        Assert.True(result.Success, result.Message);
        Assert.True(changed, "Screens must be told to refresh after an import.");
        Assert.Equal("코딩", timetable.GetWeeklyTimetable()["mon"][0]["subject"]);

        // The import used to write the file behind the service's back: the next edit through the
        // service then overwrote the imported timetable with the stale in-memory copy.
        timetable.UpdatePeriodSubject("tue", 0, "도덕", "담임");

        var reloaded = new TimetableService(new ConfigService(_root));
        Assert.Equal("코딩", reloaded.GetWeeklyTimetable()["mon"][0]["subject"]);
        Assert.Equal("도덕", reloaded.GetWeeklyTimetable()["tue"][0]["subject"]);
    }

    [Fact]
    public void DeletingACumulativeRecord_DoesNotLeaveItInTheLocalBackup()
    {
        const string deletedMarker = "DELETED-NOTE-7f3a";
        var config = new ConfigService(_root);
        var records = new ClassroomRecordService(config);

        records.AddCumulativeRecord(3, string.Empty, DateTime.Today, "관찰", deletedMarker, "칭찬");
        records.AddCumulativeRecord(4, string.Empty, DateTime.Today, "관찰", "KEPT-NOTE", "칭찬");

        string id = records.CumulativeRecords.First(r => r.Content == deletedMarker).Id;
        records.DeleteCumulativeRecord(id);

        string path = Path.Combine(config.ConfigDir, "student_cumulative_records.json");
        string backupPath = SafeLocalFileStore.BackupPath(path);

        Assert.True(File.Exists(backupPath), "Records keep a local recovery copy.");
        Assert.DoesNotContain(deletedMarker, File.ReadAllText(path));
        Assert.DoesNotContain(deletedMarker, File.ReadAllText(backupPath));
        Assert.Contains("KEPT-NOTE", File.ReadAllText(path));
    }

    [Fact]
    public void CumulativeRecords_CorruptPrimary_RecoverFromBackup()
    {
        var config = new ConfigService(_root);
        var records = new ClassroomRecordService(config);
        records.AddCumulativeRecord(1, string.Empty, DateTime.Today, "관찰", "FIRST-NOTE", "칭찬");
        records.AddCumulativeRecord(2, string.Empty, DateTime.Today, "관찰", "SECOND-NOTE", "칭찬");

        string path = Path.Combine(config.ConfigDir, "student_cumulative_records.json");
        File.WriteAllText(path, "{ truncated");

        var recovered = new ClassroomRecordService(new ConfigService(_root));

        Assert.Contains(recovered.CumulativeRecords, r => r.Content == "FIRST-NOTE");
    }
}
