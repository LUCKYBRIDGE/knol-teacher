using System.Collections.Generic;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Views.Windows;
using Xunit;

namespace KnolTeacher.Tests;

public class PrehistoryRaceMapRendererTests
{
    [Fact]
    public void TrackSampleRows_IncludeExactStartAndFinishWithoutDuplicateRows()
    {
        IReadOnlyList<double> rows = PrehistoryRaceMapRenderer.BuildTrackSampleRows();

        Assert.NotEmpty(rows);
        Assert.Equal(0.0, rows[0]);
        Assert.Equal(PrehistoryRaceThemeSpec.TrackHeight, rows[^1]);

        for (int i = 1; i < rows.Count; i++)
        {
            double gap = rows[i] - rows[i - 1];
            Assert.True(gap > 0.0, $"sample rows must increase strictly at index {i}");
            Assert.True(gap <= 36.0, $"sample gap {gap:0.###} exceeded renderer step at index {i}");
        }
    }
}
