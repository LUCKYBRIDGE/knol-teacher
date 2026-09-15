using System;
using System.IO;
using System.Linq;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Views.Windows;
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
    public void InteractiveMapProps_ExplicitlyDeclareTheirGameplayContract()
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
    public void StaticColliderVisuals_HaveMatchingCompositeBindings()
    {
        var bindings = PrehistoryRaceMapV2.StaticColliderBindings
            .ToDictionary(binding => binding.Key, StringComparer.Ordinal);
        var props = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Interaction == RaceMapInteractionRole.StaticCollider)
            .ToArray();

        Assert.Equal(props.Length, bindings.Count);

        foreach (RaceMapProp prop in props)
        {
            Assert.NotNull(prop.GameplayColliderKey);
            Assert.True(
                bindings.TryGetValue(prop.GameplayColliderKey!, out RaceStaticColliderBinding? binding),
                $"{prop.Key} is missing a static-collider binding");
            Assert.NotNull(binding);
            Assert.Equal(RaceColliderShape.Composite, binding!.Shape);
            Assert.InRange(binding.CenterX, prop.X, prop.X + prop.Width);
            Assert.InRange(binding.StartY, prop.Y, prop.Y + prop.Height);
            Assert.InRange(binding.EndY, prop.Y, prop.Y + prop.Height);
        }
    }

    [Fact]
    public void InteractiveRelics_CanBeBumpersOrBreakables()
    {
        Assert.NotEmpty(PrehistoryRaceMapV2.InteractiveRelics);

        InteractiveRelicRule pottery = Assert.Single(
            PrehistoryRaceMapV2.InteractiveRelics,
            rule => rule.Key == "comb-pottery-breakable");
        Assert.Equal(RaceMapInteractionRole.Breakable, pottery.Interaction);
        Assert.Equal(RaceColliderShape.Circle, pottery.ColliderShape);
        Assert.Equal("breakable-pottery", pottery.BehaviorKey);

        string[] bumperRelics = PrehistoryRaceMapV2.InteractiveRelics
            .Where(rule => rule.Interaction == RaceMapInteractionRole.StaticBumper)
            .Select(rule => rule.Label)
            .ToArray();

        Assert.Contains("주먹도끼", bumperRelics);
        Assert.Contains("찍개", bumperRelics);
        Assert.Contains("뼈바늘", bumperRelics);
        Assert.Contains("간석기", bumperRelics);
        Assert.Contains("가락바퀴", bumperRelics);
        Assert.Contains("조개 껍데기 가면", bumperRelics);
        Assert.Contains("반달 돌칼", bumperRelics);
        Assert.Contains("비파형 동검", bumperRelics);
        Assert.Contains("고인돌", bumperRelics);

        string[] breakableRelics = PrehistoryRaceMapV2.InteractiveRelics
            .Where(rule => rule.Interaction == RaceMapInteractionRole.Breakable)
            .Select(rule => rule.Label)
            .ToArray();

        Assert.Contains("빗살무늬 토기", breakableRelics);
        Assert.Contains("민무늬 토기", breakableRelics);

        Assert.All(
            PrehistoryRaceMapV2.InteractiveRelics,
            rule => Assert.NotEqual(RaceColliderShape.None, rule.ColliderShape));
    }

    [Fact]
    public void InteractiveRelics_RepresentAllThreeErasOnTheCourse()
    {
        var periods = PrehistoryRaceMapV2.InteractiveRelics
            .Select(rule => rule.Period)
            .ToHashSet();

        Assert.Contains("구석기", periods);
        Assert.Contains("신석기", periods);
        Assert.Contains("청동기", periods);

        var placedPeriods = PrehistoryRaceMapV2.InteractiveRelicPlacements
            .Select(placement =>
            {
                var rule = PrehistoryRaceMapV2.InteractiveRelics.First(r => r.Key == placement.RuleKey);
                return rule.Period;
            })
            .ToHashSet();

        Assert.Contains("구석기", placedPeriods);
        Assert.Contains("신석기", placedPeriods);
        Assert.Contains("청동기", placedPeriods);
    }

    [Fact]
    public void InteractiveRelicPlacements_ReferenceKnownRules_AndStayInTheirEra()
    {
        var rules = PrehistoryRaceMapV2.InteractiveRelics
            .ToDictionary(rule => rule.Key, StringComparer.Ordinal);

        Assert.NotEmpty(PrehistoryRaceMapV2.InteractiveRelicPlacements);
        Assert.Equal(
            PrehistoryRaceMapV2.InteractiveRelicPlacements.Count,
            PrehistoryRaceMapV2.InteractiveRelicPlacements.Select(placement => placement.Key).Distinct().Count());

        foreach (InteractiveRelicPlacement placement in PrehistoryRaceMapV2.InteractiveRelicPlacements)
        {
            Assert.True(
                rules.TryGetValue(placement.RuleKey, out InteractiveRelicRule? rule),
                $"{placement.Key} references unknown rule {placement.RuleKey}");

            PrehistoryRaceZone? zone = PrehistoryRaceThemeSpec.Zones
                .SingleOrDefault(candidate =>
                    placement.Y >= candidate.StartY && placement.Y < candidate.EndY);

            Assert.NotNull(zone);
            Assert.NotNull(rule);
            Assert.Equal(rule!.Period, zone!.Period);
            Assert.True(placement.ColliderRadius > 0);
            Assert.True(placement.VisualWidth > placement.ColliderRadius * 2.0);
            Assert.True(placement.VisualHeight > placement.ColliderRadius * 2.0);
        }
    }

    [Fact]
    public void BreakableCombPottery_RemainsGameplay_ButDoesNotPlugBronzeFinishChute()
    {
        InteractiveRelicRule potteryRule = Assert.Single(
            PrehistoryRaceMapV2.InteractiveRelics,
            rule => rule.Key == "comb-pottery-breakable");

        InteractiveRelicPlacement[] potteryPlacements = PrehistoryRaceMapV2.InteractiveRelicPlacements
            .Where(placement => placement.RuleKey == potteryRule.Key)
            .ToArray();

        Assert.InRange(potteryPlacements.Length, 3, 6);
        Assert.All(potteryPlacements, placement => Assert.InRange(placement.Y, 1200.0, 2399.999));
        Assert.DoesNotContain(potteryPlacements, placement => placement.Y >= 3260.0);
    }

    [Fact]
    public void MapSidePotteryLandmark_DoesNotPretendItsImageBoundsAreTheHitbox()
    {
        RaceMapProp potteryLandmark = Assert.Single(
            PrehistoryRaceMapV2.Props,
            prop => prop.Key == "neo-pottery-left");

        Assert.Equal(RaceMapVisualRole.Landmark, potteryLandmark.Role);
        Assert.Equal(RaceMapInteractionRole.None, potteryLandmark.Interaction);
        Assert.Null(potteryLandmark.GameplayColliderKey);

        Assert.Contains(
            PrehistoryRaceMapV2.InteractiveRelics,
            rule => rule.Label == "빗살무늬 토기" && rule.Interaction == RaceMapInteractionRole.Breakable);
    }

    [Fact]
    public void PendingPlainPottery_IsPlannedAsBreakable_ButReservedLandmarkIsVisualOnly()
    {
        PendingRaceArtAsset plainPottery = Assert.Single(
            PrehistoryRaceMapV2.PendingArtAssets,
            asset => asset.Key == "plain-pottery");
        Assert.Equal(RaceMapInteractionRole.Breakable, plainPottery.IntendedInteraction);

        RaceMapProp reservedLandmark = Assert.Single(
            PrehistoryRaceMapV2.Props,
            prop => prop.AssetName == plainPottery.FileName);
        Assert.Equal(RaceMapVisualRole.Landmark, reservedLandmark.Role);
        Assert.Equal(RaceMapInteractionRole.None, reservedLandmark.Interaction);
        Assert.Null(reservedLandmark.GameplayColliderKey);
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
    public void PendingHistoricalArt_HasReservedNonInteractiveLandmarkSlots()
    {
        foreach (PendingRaceArtAsset asset in PrehistoryRaceMapV2.PendingArtAssets)
        {
            RaceMapProp slot = Assert.Single(
                PrehistoryRaceMapV2.Props,
                prop => prop.AssetName.Equals(asset.FileName, StringComparison.OrdinalIgnoreCase));

            Assert.Equal(RaceMapVisualRole.Landmark, slot.Role);
            Assert.Equal(RaceMapInteractionRole.None, slot.Interaction);
            Assert.Null(slot.GameplayColliderKey);
            Assert.Equal(asset.Label, slot.Label);
            Assert.Equal(asset.Period, slot.Period);
            Assert.True(slot.Width > 0);
            Assert.True(slot.Height > 0);

            PrehistoryRaceZone? zone = PrehistoryRaceThemeSpec.Zones
                .SingleOrDefault(candidate => slot.Y >= candidate.StartY && slot.Y < candidate.EndY);

            Assert.NotNull(zone);
            Assert.Equal(asset.Period, zone!.Period);
        }
    }

    [Fact]
    public void PendingHistoricalArt_HasProductionReadyTransparentPngSpecs()
    {
        Assert.Equal(6, PrehistoryRaceMapV2.PendingArtAssets.Count);

        Assert.All(
            PrehistoryRaceMapV2.PendingArtAssets,
            asset =>
            {
                Assert.Equal(1024, asset.SourceWidth);
                Assert.Equal(1024, asset.SourceHeight);
                Assert.InRange(asset.TransparentPaddingRatio, 0.08, 0.12);
                Assert.EndsWith(".png", asset.FileName, StringComparison.OrdinalIgnoreCase);
            });
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

    [Fact]
    public void ProducedRelicPngAssets_ExistOnDiskAndAreValidPng()
    {
        string raceDir = ResolveAssetRaceDirectory();
        Assert.True(Directory.Exists(raceDir), $"assets/race directory must exist at {raceDir}");

        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        foreach (PendingRaceArtAsset asset in PrehistoryRaceMapV2.PendingArtAssets)
        {
            string filePath = Path.Combine(raceDir, asset.FileName);
            Assert.True(File.Exists(filePath), $"Relic asset file {asset.FileName} must exist on disk");

            var fileInfo = new FileInfo(filePath);
            Assert.True(fileInfo.Length > 0, $"Relic asset {asset.FileName} must not be empty");

            byte[] header = new byte[8];
            using (var stream = File.OpenRead(filePath))
            {
                int read = stream.Read(header, 0, header.Length);
                Assert.Equal(8, read);
            }

            Assert.Equal(pngHeader, header);
        }
    }

    [Fact]
    public void LandmarkProps_StayOutsideDrivableRoad()
    {
        var landmarks = PrehistoryRaceMapV2.Props
            .Where(prop => prop.Role == RaceMapVisualRole.Landmark)
            .ToArray();

        Assert.NotEmpty(landmarks);

        foreach (RaceMapProp prop in landmarks)
        {
            double centerY = prop.Y + prop.Height / 2.0;
            double centerX = prop.X + prop.Width / 2.0;

            StudentPickerWindow.GetTrackBoundaries(centerY, out double left, out double right);
            Assert.True(
                centerX < left || centerX > right,
                $"{prop.Key} center ({centerX:0.##}) must remain outside drivable road [{left:0.##}, {right:0.##}] at Y={centerY:0.##}");
        }
    }

    [Fact]
    public void InteractivePlacements_StayWithinDrivableRoad()
    {
        Assert.NotEmpty(PrehistoryRaceMapV2.InteractiveRelicPlacements);

        foreach (InteractiveRelicPlacement placement in PrehistoryRaceMapV2.InteractiveRelicPlacements)
        {
            StudentPickerWindow.GetTrackBoundaries(placement.Y, out double left, out double right);
            Assert.True(
                placement.X >= left && placement.X <= right,
                $"{placement.Key} ({placement.X:0.##}) must stay within drivable road [{left:0.##}, {right:0.##}] at Y={placement.Y:0.##}");
        }
    }

    private static string ResolveAssetRaceDirectory()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "src", "KnolTeacher.Desktop", "assets", "race");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            string directCandidate = Path.Combine(dir, "assets", "race");
            if (Directory.Exists(directCandidate))
            {
                return directCandidate;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("Could not locate assets/race directory.");
    }
}

