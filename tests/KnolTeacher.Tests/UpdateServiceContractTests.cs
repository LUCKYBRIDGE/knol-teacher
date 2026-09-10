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
}
