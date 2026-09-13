using System.Linq;
using KnolTeacher.Desktop.Models;
using Xunit;

namespace KnolTeacher.Tests;

public class PrehistoryRaceThemeSpecTests
{
    [Fact]
    public void Zones_CoverWholeTrackInOrder()
    {
        var zones = PrehistoryRaceThemeSpec.Zones;

        Assert.Equal(5, zones.Count);
        Assert.Equal(0, zones[0].StartY);
        Assert.Equal(PrehistoryRaceThemeSpec.TrackHeight, zones[^1].EndY);

        for (int i = 1; i < zones.Count; i++)
        {
            Assert.Equal(zones[i - 1].EndY, zones[i].StartY);
            Assert.True(zones[i].EndY > zones[i].StartY);
        }
    }

    [Fact]
    public void Artifacts_ContainAllSixTextbookLandmarks()
    {
        string[] expected =
        {
            "뼈바늘",
            "가락바퀴",
            "주먹도끼",
            "간석기",
            "조개 껍데기 가면",
            "찍개"
        };

        var actual = PrehistoryRaceThemeSpec.Artifacts.Select(item => item.Name).ToHashSet();
        Assert.Equal(expected.Length, actual.Count);
        foreach (string name in expected)
        {
            Assert.Contains(name, actual);
        }
    }

    [Theory]
    [InlineData("찍개", "구석기")]
    [InlineData("주먹도끼", "구석기")]
    [InlineData("뼈바늘", "구석기")]
    [InlineData("간석기", "신석기")]
    [InlineData("가락바퀴", "신석기")]
    [InlineData("조개 껍데기 가면", "신석기")]
    public void Artifacts_AreMappedToIntendedPeriod(string name, string period)
    {
        var artifact = Assert.Single(PrehistoryRaceThemeSpec.Artifacts, item => item.Name == name);
        Assert.Equal(period, artifact.Period);
    }

    [Fact]
    public void Theme_DoesNotUseBronzeAgeDolmenAsZoneConcept()
    {
        Assert.DoesNotContain(PrehistoryRaceThemeSpec.Zones, zone => zone.Title.Contains("고인돌"));
        Assert.DoesNotContain(PrehistoryRaceThemeSpec.Artifacts, artifact => artifact.Name.Contains("고인돌"));
    }
}
