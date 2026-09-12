using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
        string script = BuildUpdaterScript(
            @"C:\Temp\놀티쳐.exe",
            @"C:\Apps\놀티쳐.exe",
            @"C:\Apps\놀티쳐.exe",
            @"C:\Users\Teacher\.knol_teacher_desk\update_completed.txt",
            @"C:\Users\Teacher\.knol_teacher_desk\update_failed.txt",
            new string('A', 64),
            "v1.1.0",
            1234,
            @"C:\Temp\knol_updater_test.ps1");

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

    [Fact]
    public async Task UpdaterScript_EndToEnd_ReplacesTargetAndWritesSuccessMarker()
    {
        if (!OperatingSystem.IsWindows()) return;

        string root = CreateTestDirectory();
        try
        {
            string source = Path.Combine(root, "source.exe");
            string target = Path.Combine(root, UpdateService.LocalExecutableName);
            string successMarker = Path.Combine(root, "state", "update_completed.txt");
            string failureMarker = Path.Combine(root, "state", "update_failed.txt");
            string diagnosticsPath = Path.Combine(root, "attempt-errors.txt");
            string scriptPath = Path.Combine(root, "updater.ps1");

            File.Copy(GetSystemExecutable("whoami.exe"), source);
            File.Copy(GetSystemExecutable("where.exe"), target);

            string expectedHash = ComputeSha256Hex(source);
            string originalHash = ComputeSha256Hex(target);
            string script = BuildUpdaterScript(
                source,
                target,
                target,
                successMarker,
                failureMarker,
                expectedHash,
                "v1.1.0",
                int.MaxValue,
                scriptPath);
            script = AddAttemptDiagnostics(script, diagnosticsPath);

            File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await RunPowerShellScriptAsync(scriptPath);

            string actualHash = ComputeSha256Hex(target);
            string diagnostics = File.Exists(diagnosticsPath)
                ? File.ReadAllText(diagnosticsPath)
                : "(no replacement-attempt diagnostics were recorded)";
            Assert.True(string.Equals(expectedHash, actualHash, StringComparison.Ordinal),
                $"Verified replacement hash mismatch. Expected={expectedHash}, Actual={actualHash}, Original={originalHash}. Attempt errors: {diagnostics}");
            Assert.NotEqual(originalHash, actualHash);
            Assert.True(File.Exists(successMarker));
            Assert.Equal("v1.1.0", File.ReadAllText(successMarker).Trim());
            Assert.False(File.Exists(failureMarker));
            Assert.False(File.Exists(source));
            Assert.False(File.Exists(target + ".knol-update-new"));
            Assert.False(File.Exists(target + ".knol-update-backup"));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    [Fact]
    public async Task UpdaterScript_EndToEnd_RestoresBackupWhenReplacementCannotStart()
    {
        if (!OperatingSystem.IsWindows()) return;

        string root = CreateTestDirectory();
        try
        {
            string source = Path.Combine(root, "source.exe");
            string target = Path.Combine(root, UpdateService.LocalExecutableName);
            string successMarker = Path.Combine(root, "state", "update_completed.txt");
            string failureMarker = Path.Combine(root, "state", "update_failed.txt");
            string scriptPath = Path.Combine(root, "updater.ps1");

            File.WriteAllText(source, "This is deliberately not a Windows executable.", Encoding.UTF8);
            File.Copy(GetSystemExecutable("where.exe"), target);

            string replacementHash = ComputeSha256Hex(source);
            string originalHash = ComputeSha256Hex(target);
            string script = BuildUpdaterScript(
                source,
                target,
                target,
                successMarker,
                failureMarker,
                replacementHash,
                "v1.1.0",
                int.MaxValue,
                scriptPath);

            File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await RunPowerShellScriptAsync(scriptPath);

            Assert.Equal(originalHash, ComputeSha256Hex(target));
            Assert.NotEqual(replacementHash, ComputeSha256Hex(target));
            Assert.False(File.Exists(successMarker));
            Assert.True(File.Exists(failureMarker));
            Assert.Equal("replacement_failed", File.ReadAllText(failureMarker).Trim());
            Assert.False(File.Exists(target + ".knol-update-new"));
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    private static string BuildUpdaterScript(
        string source,
        string target,
        string runningExe,
        string successMarker,
        string failureMarker,
        string expectedHash,
        string expectedVersion,
        int currentPid,
        string scriptPath)
    {
        MethodInfo? method = typeof(UpdateService).GetMethod(
            "BuildPowerShellUpdaterScript",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return Assert.IsType<string>(method.Invoke(null, new object?[]
        {
            source,
            target,
            runningExe,
            successMarker,
            failureMarker,
            expectedHash,
            expectedVersion,
            currentPid,
            scriptPath
        }));
    }

    private static string AddAttemptDiagnostics(string script, string diagnosticsPath)
    {
        const string marker = "        catch {\n            if (Test-Path -LiteralPath $backup) {";
        string replacement =
            "        catch {\n" +
            $"            Add-Content -LiteralPath '{EscapePowerShellLiteral(diagnosticsPath)}' -Value ($_.Exception.GetType().FullName + ': ' + $_.Exception.Message)\n" +
            "            if (Test-Path -LiteralPath $backup) {";

        Assert.Contains(marker, script, StringComparison.Ordinal);
        return script.Replace(marker, replacement, StringComparison.Ordinal);
    }

    private static string EscapePowerShellLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private static string CreateTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"KnolTeacherUpdaterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetSystemExecutable(string fileName)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), fileName);
        Assert.True(File.Exists(path), $"Required Windows system executable was not found: {path}");
        return path;
    }

    private static string ComputeSha256Hex(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using SHA256 sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    private static async Task RunPowerShellScriptAsync(string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Windows PowerShell for updater integration test.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("Updater PowerShell integration test exceeded 30 seconds.");
        }

        string output = await standardOutput;
        string error = await standardError;
        Assert.True(process.ExitCode == 0,
            $"Updater PowerShell script exited with code {process.ExitCode}. Output: {output} Error: {error}");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
