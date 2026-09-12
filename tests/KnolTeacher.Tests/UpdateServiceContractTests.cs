using System.Reflection;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public class UpdateServiceContractTests
{
    [Theory]
    [InlineData("KnolTeacher.exe", true)]
    [InlineData("knolteacher.EXE", true)]
    [InlineData("놀티쳐.exe", false)]
    [InlineData("default.exe", false)]
    [InlineData("setup.exe", false)]
    [InlineData("", false)]
    public void AcceptedReleaseAssetNames_AreExplicitAndLimited(string assetName, bool expected)
        => Assert.Equal(expected, UpdateService.IsAcceptedReleaseAssetName(assetName));

    [Theory]
    [InlineData("v1.0.1", "v1.0.0", true)]
    [InlineData("v1.0.0", "v1.0.0", false)]
    [InlineData("v0.9.9", "v1.0.0", false)]
    [InlineData("v1.1.0", "v1.0.0", true)]
    [InlineData("v2.0.0", "v1.0.0", true)]
    [InlineData("v1.0.9", "v1.0.10", false)]
    public void VersionComparison_UsesSemanticVersionOrdering(string latest, string current, bool expected)
        => Assert.Equal(expected, UpdateService.IsNewerVersion(latest, current));

    [Theory]
    [InlineData("v1.0.0", "1.0.0.0", true)]
    [InlineData("1.0.1", "v1.0.1", true)]
    [InlineData("v1.1.0", "1.1.0.0", true)]
    [InlineData("v1.0.1", "v1.0.0", false)]
    [InlineData("invalid", "v1.0.0", false)]
    public void SameProductVersion_UsesMajorMinorBuild(string left, string right, bool expected)
        => Assert.Equal(expected, UpdateService.IsSameProductVersion(left, right));

    [Fact]
    public void UpdaterScript_StagesVerifiesAndRollsBackBeforeReportingSuccess()
    {
        MethodInfo? method = typeof(UpdateService).GetMethod(
            "BuildPowerShellUpdaterScript",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        string script = Assert.IsType<string>(method.Invoke(null, new object?[]
        {
            @"C:\Temp\놀티쳐.exe",
            @"C:\Apps\놀티쳐.exe",
            @"C:\Apps\놀티쳐.exe",
            @"C:\Users\Teacher\.knol_teacher_desk\update_completed.txt",
            @"C:\Users\Teacher\.knol_teacher_desk\update_failed.txt",
            new string('A', 64),
            "v1.1.0",
            1234,
            @"C:\Temp\knol_updater_test.ps1"
        }));

        Assert.Contains("$staged = $target + '.knol-update-new'", script, StringComparison.Ordinal);
        Assert.Contains("$backup = $target + '.knol-update-backup'", script, StringComparison.Ordinal);
        Assert.Contains("Copy-Item -LiteralPath $source -Destination $staged -Force", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Copy-Item -LiteralPath $source -Destination $target -Force", script, StringComparison.Ordinal);
        Assert.Contains("[System.IO.File]::Replace($staged, $target, $backup, $true)", script, StringComparison.Ordinal);
        Assert.Contains("Copy-Item -LiteralPath $backup -Destination $target -Force", script, StringComparison.Ordinal);

        int markerDirectoryIndex = script.IndexOf("New-Item -ItemType Directory -Path $markerDir -Force", StringComparison.Ordinal);
        int launchIndex = script.IndexOf("Start-Process -FilePath $target", StringComparison.Ordinal);
        int successMarkerIndex = script.IndexOf("Set-Content -LiteralPath $successMarker", StringComparison.Ordinal);
        Assert.True(markerDirectoryIndex >= 0 && markerDirectoryIndex < launchIndex,
            "Marker directory setup must fail before launching the replacement, not trigger rollback afterward.");
        Assert.True(launchIndex >= 0, "Updater must launch the verified replacement.");
        Assert.True(successMarkerIndex > launchIndex, "Success must only be recorded after the new executable starts.");
    }
}
