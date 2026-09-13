using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace KnolTeacher.Desktop.Views.Controls;

/// <summary>
/// Finds a classroom-friendly spawn position for a new NolBoard widget.
/// Empty space is preferred; when the board is full the position with the least overlap wins.
/// </summary>
public static class NolboardPlacementPlanner
{
    public static Point FindBestPosition(
        double canvasWidth,
        double canvasHeight,
        double widgetWidth,
        double widgetHeight,
        IEnumerable<Rect>? occupied,
        double margin = 16,
        double gap = 12,
        double step = 24)
    {
        canvasWidth = Math.Max(1, canvasWidth);
        canvasHeight = Math.Max(1, canvasHeight);
        margin = Math.Max(0, margin);
        gap = Math.Max(0, gap);
        step = Math.Max(8, step);

        double usableWidth = Math.Max(1, canvasWidth - (margin * 2));
        double usableHeight = Math.Max(1, canvasHeight - (margin * 2));
        widgetWidth = Math.Clamp(widgetWidth, 1, usableWidth);
        widgetHeight = Math.Clamp(widgetHeight, 1, usableHeight);

        var existing = (occupied ?? Array.Empty<Rect>())
            .Where(rect => rect.Width > 0 && rect.Height > 0)
            .ToArray();

        double maxX = Math.Max(margin, canvasWidth - margin - widgetWidth);
        double maxY = Math.Max(margin, canvasHeight - margin - widgetHeight);
        var bestPoint = new Point(margin, margin);
        double bestOverlap = double.MaxValue;

        foreach (double y in CandidateAxis(margin, maxY, step))
        {
            foreach (double x in CandidateAxis(margin, maxX, step))
            {
                var candidate = new Rect(x, y, widgetWidth, widgetHeight);
                double overlap = 0;

                foreach (var rect in existing)
                {
                    var padded = new Rect(
                        rect.X - gap,
                        rect.Y - gap,
                        rect.Width + (gap * 2),
                        rect.Height + (gap * 2));

                    Rect intersection = Rect.Intersect(candidate, padded);
                    if (!intersection.IsEmpty)
                    {
                        overlap += intersection.Width * intersection.Height;
                    }
                }

                if (overlap <= 0)
                {
                    return new Point(x, y);
                }

                if (overlap < bestOverlap)
                {
                    bestOverlap = overlap;
                    bestPoint = new Point(x, y);
                }
            }
        }

        return bestPoint;
    }

    private static IEnumerable<double> CandidateAxis(double start, double end, double step)
    {
        if (end <= start)
        {
            yield return start;
            yield break;
        }

        for (double value = start; value < end; value += step)
        {
            yield return value;
        }

        yield return end;
    }
}
