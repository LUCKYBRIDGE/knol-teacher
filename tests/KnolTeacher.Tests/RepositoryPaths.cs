using System;
using System.IO;
using System.Linq;
using Xunit;

namespace KnolTeacher.Tests;

/// <summary>
/// Resolves files in the repository for source/XAML contract tests.
/// The test output folder depth depends on configuration and runtime identifier
/// (e.g. bin/Release/net8.0-windows/win-x64), so the repository root is found by
/// walking up to KnolTeacher.sln instead of using a fixed number of "..".
/// A missing file fails the test instead of silently skipping the assertions.
/// </summary>
internal static class RepositoryPaths
{
    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KnolTeacher.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("KnolTeacher.sln was not found above the test output directory.");
    }

    public static string GetFile(params string[] relativeParts)
    {
        string path = Path.Combine(new[] { GetRepositoryRoot() }.Concat(relativeParts).ToArray());
        Assert.True(File.Exists(path), $"Expected repository file is missing: {string.Join("/", relativeParts)}");
        return path;
    }
}
