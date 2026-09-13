using System.Collections.Generic;
using System.Windows;
using KnolTeacher.Desktop.Views.Controls;
using Xunit;

namespace KnolTeacher.Tests;

public class NolboardPlacementPlannerTests
{
    [Fact]
    public void EmptyBoard_StartsAtTopLeftMargin()
    {
        Point point = NolboardPlacementPlanner.FindBestPosition(
            1600, 900, 680, 480, System.Array.Empty<Rect>());

        Assert.Equal(new Point(16, 16), point);
    }

    [Fact]
    public void ExistingWidget_MakesNextWidgetUseFreeSpaceFirst()
    {
        var occupied = new List<Rect>
        {
            new(16, 16, 680, 480)
        };

        Point point = NolboardPlacementPlanner.FindBestPosition(
            1600, 900, 680, 360, occupied);

        var candidate = new Rect(point.X, point.Y, 680, 360);
        Assert.False(candidate.IntersectsWith(occupied[0]));
    }

    [Fact]
    public void FullBoard_ReturnsPositionInsideCanvas()
    {
        var occupied = new List<Rect>
        {
            new(0, 0, 1200, 700)
        };

        Point point = NolboardPlacementPlanner.FindBestPosition(
            1200, 700, 600, 400, occupied);

        Assert.InRange(point.X, 16, 584);
        Assert.InRange(point.Y, 16, 284);
    }

    [Fact]
    public void OversizedWidget_IsStillPositionedSafely()
    {
        Point point = NolboardPlacementPlanner.FindBestPosition(
            800, 500, 1600, 1200, System.Array.Empty<Rect>());

        Assert.Equal(new Point(16, 16), point);
    }
}
