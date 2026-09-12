using System.Diagnostics;
using System.Text;
using Xunit;

namespace KnolTeacher.Tests;

public class PowerShellFileReplaceTests
{
    [Fact]
    public async Task FileReplace_WorksThroughWindowsPowerShellOnRunnerTempVolume()
    {
        if (!OperatingSystem.IsWindows()) return;

        string root = Path.Combine(Path.GetTempPath(), $"KnolTeacherFileReplaceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            string source = Path.Combine(root, "source.txt");
            string target = Path.Combine(root, "target.txt");
            string backup = Path.Combine(root, "backup.txt");
            string scriptPath = Path.Combine(root, "replace.ps1");

            File.WriteAllText(source, "new-content", Encoding.UTF8);
            File.WriteAllText(target, "old-content", Encoding.UTF8);

            string script = $@"$ErrorActionPreference = 'Stop'
[System.IO.File]::Replace('{EscapePowerShellLiteral(source)}', '{EscapePowerShellLiteral(target)}', '{EscapePowerShellLiteral(backup)}', $true)
";
            File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

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
                ?? throw new InvalidOperationException("Failed to start Windows PowerShell for File.Replace test.");
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new TimeoutException("PowerShell File.Replace probe exceeded 15 seconds.");
            }

            string output = await standardOutput;
            string error = await standardError;
            Assert.True(process.ExitCode == 0,
                $"PowerShell File.Replace failed with code {process.ExitCode}. Output: {output} Error: {error}");
            Assert.Equal("new-content", File.ReadAllText(target));
            Assert.Equal("old-content", File.ReadAllText(backup));
            Assert.False(File.Exists(source));
        }
        finally
        {
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static string EscapePowerShellLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);
}
