using KnolTeacher.Desktop.Views.Controls;
using Xunit;

namespace KnolTeacher.Tests;

public class NolboardWidgetRegistryTests
{
    private static readonly string[] WidgetTypes =
    {
        "timer",
        "picker",
        "dice",
        "wheel",
        "score",
        "drawing",
        "blackboard",
        "timetable",
        "meal",
        "memo",
        "checklist",
        "qr",
        "weather",
        "dday"
    };

    [Fact]
    public void Registry_ContainsExactlyTheSupportedInCanvasWidgetKinds()
    {
        foreach (string type in WidgetTypes)
        {
            Assert.True(WidgetRegistry.TryGet(type, out var definition));
            Assert.Equal(type, definition.Type);
            Assert.True(definition.DefaultWidth >= definition.MinWidth);
            Assert.True(definition.DefaultHeight >= definition.MinHeight);
            Assert.True(definition.CanResize);
        }
    }

    [Theory]
    [InlineData("timer", 680, 480)]
    [InlineData("picker", 720, 560)]
    [InlineData("drawing", 800, 620)]
    [InlineData("qr", 640, 720)]
    [InlineData("weather", 680, 580)]
    public void ClassroomWidgets_OpenAtLargeReadableDefaults(string type, double expectedWidth, double expectedHeight)
    {
        Assert.True(WidgetRegistry.TryGet(type, out var definition));
        Assert.Equal(expectedWidth, definition.DefaultWidth);
        Assert.Equal(expectedHeight, definition.DefaultHeight);
    }

    [Fact]
    public void Pinball_IsASeparateWindowTool_NotANolboardWidget()
    {
        Assert.False(WidgetRegistry.TryGet("pinball", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void UnknownWidgetKinds_AreRejected(string? type)
    {
        Assert.False(WidgetRegistry.TryGet(type, out _));
    }
}
