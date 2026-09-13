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
    public void DecorativeVisuals_DoNotPretendToOwnPhysics()
    {
        var purelyVisual = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Role != RaceMapVisualRole.GameplayObstacleVisual)
            .ToArray();

        Assert.NotEmpty(purelyVisual);
        Assert.All(purelyVisual, prop => Assert.Null(prop.GameplayColliderKey));
    }

    [Fact]
    public void GameplayObstacleVisuals_ExplicitlyMapToPhysicsKeys()
    {
        var gameplayVisuals = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Role == RaceMapVisualRole.GameplayObstacleVisual)
            .ToArray();

        Assert.Equal(4, gameplayVisuals.Length);
        Assert.All(gameplayVisuals, prop => Assert.False(string.IsNullOrWhiteSpace(prop.GameplayColliderKey)));

        string[] expected =
        {
            "river-rock-main",
            "river-rock-upper",
            "river-rock-lower-left",
            "river-rock-lower-right"
        };

        Assert.Equal(
            expected.OrderBy(value => value),
            gameplayVisuals.Select(prop => prop.GameplayColliderKey!).OrderBy(value => value));
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
        Assert.All(foreground, prop => Assert.Null(prop.GameplayColliderKey));
    }
}
