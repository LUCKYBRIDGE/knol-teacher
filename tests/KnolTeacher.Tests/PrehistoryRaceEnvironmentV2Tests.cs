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
}
