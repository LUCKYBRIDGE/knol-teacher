using System;
using System.Linq;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Views.Windows;
using Xunit;

namespace KnolTeacher.Tests;

public class PrehistoryRaceEnvironmentV2Tests
{
    [Fact]
    public void PendingEnvironmentArt_HasReservedNonInteractiveBitmapProps()
    {
        Assert.Equal(6, PrehistoryRaceEnvironmentV2.PendingArtAssets.Count);
        Assert.Equal(6, PrehistoryRaceEnvironmentV2.Props.Count);

        foreach (PendingRaceEnvironmentAsset asset in PrehistoryRaceEnvironmentV2.PendingArtAssets)
        {
            RaceMapProp prop = Assert.Single(
                PrehistoryRaceEnvironmentV2.Props,
                candidate => candidate.Key == asset.ReservedPropKey);

            Assert.Equal(asset.FileName, prop.AssetName);
            Assert.Equal(asset.Period, prop.Period);
            Assert.Equal(RaceMapVisualRole.Decoration, prop.Role);
            Assert.Equal(RaceMapInteractionRole.None, prop.Interaction);
            Assert.Null(prop.GameplayColliderKey);
            Assert.Null(prop.Label);

            Assert.EndsWith(".png", asset.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1536, asset.SourceWidth);
            Assert.Equal(1024, asset.SourceHeight);
            Assert.InRange(asset.TransparentPaddingRatio, 0.06, 0.12);
        }
    }

    [Fact]
    public void EnvironmentSlots_StayInTheirEra_AndFrameTheRoadFromOutside()
    {
        foreach (RaceMapProp prop in PrehistoryRaceEnvironmentV2.Props)
        {
            double centerY = prop.Y + prop.Height / 2.0;
            double centerX = prop.X + prop.Width / 2.0;

            PrehistoryRaceZone? zone = PrehistoryRaceThemeSpec.Zones
                .SingleOrDefault(candidate =>
                    centerY >= candidate.StartY && centerY < candidate.EndY);

            Assert.NotNull(zone);
            Assert.Equal(prop.Period, zone!.Period);

            StudentPickerWindow.GetTrackBoundaries(centerY, out double left, out double right);
            Assert.True(
                centerX < left || centerX > right,
                $"{prop.Key} center must remain outside the drivable road at Y={centerY:0.##}");

            Assert.True(prop.ZIndex < 0, $"{prop.Key} must stay behind racers/UI");
        }
    }

    [Fact]
    public void EnvironmentArt_CoversAllThreeEras_WithTwoAnchorsEach()
    {
        var counts = PrehistoryRaceEnvironmentV2.PendingArtAssets
            .GroupBy(asset => asset.Period)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(3, counts.Count);
        Assert.Equal(2, counts["구석기"]);
        Assert.Equal(2, counts["신석기"]);
        Assert.Equal(2, counts["청동기"]);
    }

    [Fact]
    public void EnvironmentSlots_HaveBoundedProjectorScaleFootprints()
    {
        foreach (RaceMapProp prop in PrehistoryRaceEnvironmentV2.Props)
        {
            Assert.InRange(prop.Width, 160.0, 260.0);
            Assert.InRange(prop.Height, 180.0, 280.0);
            Assert.InRange(prop.Opacity, 0.90, 1.0);
        }
    }

    [Fact]
    public void ProducedEnvironmentPngAssets_ExistOnDiskAndAreValidPng()
    {
        string raceDir = ResolveAssetRaceDirectory();
        Assert.True(System.IO.Directory.Exists(raceDir), $"assets/race directory must exist at {raceDir}");

        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        foreach (PendingRaceEnvironmentAsset asset in PrehistoryRaceEnvironmentV2.PendingArtAssets)
        {
            string filePath = System.IO.Path.Combine(raceDir, asset.FileName);
            Assert.True(System.IO.File.Exists(filePath), $"Environment asset file {asset.FileName} must exist on disk");

            var fileInfo = new System.IO.FileInfo(filePath);
            Assert.True(fileInfo.Length > 0, $"Environment asset {asset.FileName} must not be empty");

            byte[] header = new byte[8];
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                int read = stream.Read(header, 0, header.Length);
                Assert.Equal(8, read);
            }

            Assert.Equal(pngHeader, header);
        }
    }

    private static string ResolveAssetRaceDirectory()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            string candidate = System.IO.Path.Combine(dir, "src", "KnolTeacher.Desktop", "assets", "race");
            if (System.IO.Directory.Exists(candidate))
            {
                return candidate;
            }

            string directCandidate = System.IO.Path.Combine(dir, "assets", "race");
            if (System.IO.Directory.Exists(directCandidate))
            {
                return directCandidate;
            }

            dir = System.IO.Path.GetDirectoryName(dir);
        }

        throw new System.IO.DirectoryNotFoundException("Could not locate assets/race directory.");
    }
}

