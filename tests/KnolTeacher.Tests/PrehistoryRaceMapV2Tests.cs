using System;
using System.Linq;
using KnolTeacher.Desktop.Models;
using Xunit;

namespace KnolTeacher.Tests;

public class PrehistoryRaceMapV2Tests
{
    [Fact]
    public void Props_UseBitmapAssetsOnly()
    {
        Assert.NotEmpty(PrehistoryRaceMapV2.Props);

        foreach (RaceMapProp prop in PrehistoryRaceMapV2.Props)
        {
            string extension = System.IO.Path.GetExtension(prop.AssetName);
            Assert.True(
                extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".webp", StringComparison.OrdinalIgnoreCase),
                $"{prop.Key} must use PNG/WebP final art, found {prop.AssetName}");

            Assert.False(
                extension.Equals(".svg", StringComparison.OrdinalIgnoreCase),
                $"{prop.Key} must not use SVG scenery");
        }
    }

    [Fact]
    public void NonInteractiveProps_DoNotOwnColliderKeys()
    {
        var purelyVisual = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Interaction == RaceMapInteractionRole.None)
            .ToArray();

        Assert.NotEmpty(purelyVisual);
        Assert.All(purelyVisual, prop => Assert.Null(prop.GameplayColliderKey));
    }

    [Fact]
    public void InteractiveProps_ExplicitlyDeclareTheirGameplayContract()
    {
        var interactive = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Interaction != RaceMapInteractionRole.None)
            .ToArray();

        Assert.NotEmpty(interactive);
        Assert.All(interactive, prop => Assert.False(string.IsNullOrWhiteSpace(prop.GameplayColliderKey)));

        var riverColliders = interactive
            .Where(prop => prop.Interaction == RaceMapInteractionRole.StaticCollider)
            .Select(prop => prop.GameplayColliderKey)
            .ToArray();

        Assert.Contains("river-rock-main", riverColliders);
        Assert.Contains("river-rock-upper", riverColliders);
        Assert.Contains("river-rock-lower-left", riverColliders);
        Assert.Contains("river-rock-lower-right", riverColliders);
    }

    [Fact]
    public void BreakablePottery_RemainsAnInteractiveRelic()
    {
        RaceMapProp pottery = Assert.Single(
            PrehistoryRaceMapV2.Props.Where(prop => prop.Key == "neo-pottery-left"));

        Assert.Equal(RaceMapVisualRole.Landmark, pottery.Role);
        Assert.Equal(RaceMapInteractionRole.Breakable, pottery.Interaction);
        Assert.Equal("breakable-pottery", pottery.GameplayColliderKey);

        PendingRaceArtAsset plainPottery = Assert.Single(
            PrehistoryRaceMapV2.PendingArtAssets.Where(asset => asset.Key == "plain-pottery"));
        Assert.Equal(RaceMapInteractionRole.Breakable, plainPottery.IntendedInteraction);
    }

    [Fact]
    public void Landmarks_StillRepresentAllThreeEras()
    {
        var periods = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Role == RaceMapVisualRole.Landmark)
            .Select(prop => prop.Period)
            .Where(period => period != null)
            .ToHashSet();

        Assert.Contains("구석기", periods);
        Assert.Contains("신석기", periods);
        Assert.Contains("청동기", periods);
    }

    [Fact]
    public void MissingHistoricalArt_IsExplicitInsteadOfVectorFallback()
    {
        string[] expected =
        {
            "뼈바늘",
            "가락바퀴",
            "조개 껍데기 가면",
            "반달 돌칼",
            "민무늬 토기",
            "비파형 동검"
        };

        var actual = PrehistoryRaceMapV2.PendingArtAssets
            .Select(asset => asset.Label)
            .ToHashSet();

        Assert.Equal(expected.Length, actual.Count);
        foreach (string label in expected)
        {
            Assert.Contains(label, actual);
        }

        Assert.All(
            PrehistoryRaceMapV2.PendingArtAssets,
            asset => Assert.EndsWith(".png", asset.FileName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ForegroundProps_AreSparseAndVisualOnly()
    {
        var foreground = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Role == RaceMapVisualRole.Foreground)
            .ToArray();

        Assert.InRange(foreground.Length, 1, 4);
        Assert.All(foreground, prop => Assert.True(prop.ZIndex > 0));
        Assert.All(foreground, prop => Assert.Equal(RaceMapInteractionRole.None, prop.Interaction));
        Assert.All(foreground, prop => Assert.Null(prop.GameplayColliderKey));
    }
}
