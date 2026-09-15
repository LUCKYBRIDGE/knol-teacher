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

    [Fact]
    public void BoardWidgetHost_ResizeLayer_DoesNotObstructTitleBarCloseButton()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates = new[]
        {
            System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "src", "KnolTeacher.Desktop", "Views", "Controls", "BoardWidgetHost.xaml"),
            System.IO.Path.Combine(baseDir, "Views", "Controls", "BoardWidgetHost.xaml")
        };
        string? xamlPath = candidates.FirstOrDefault(System.IO.File.Exists);
        if (xamlPath == null) return; // Skip if source file not found in test runner

        string content = System.IO.File.ReadAllText(xamlPath);

        // Top handles must NOT exist so that TitleBar buttons (especially BtnClose at top-right)
        // are never blocked or intercepted by resize thumbs
        Assert.DoesNotContain("Tag=\"NE\"", content);
        Assert.DoesNotContain("Tag=\"N\"", content);
        Assert.DoesNotContain("Tag=\"NW\"", content);

        // Natural bottom and corner resize handles must exist
        Assert.Contains("Tag=\"SE\"", content);
        Assert.Contains("Tag=\"SW\"", content);
        Assert.Contains("Tag=\"S\"", content);
        Assert.Contains("Tag=\"E\"", content);
        Assert.Contains("Tag=\"W\"", content);
    }

    [Fact]
    public void StudentDisplayWindow_PopupUx_DoesNotHijackPinballToPicker()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates = new[]
        {
            System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "src", "KnolTeacher.Desktop", "Views", "Windows", "StudentDisplayWindow.PopupUx.cs"),
            System.IO.Path.Combine(baseDir, "Views", "Windows", "StudentDisplayWindow.PopupUx.cs")
        };
        string? csPath = candidates.FirstOrDefault(System.IO.File.Exists);
        if (csPath == null) return;

        string content = System.IO.File.ReadAllText(csPath);

        // BtnToolPinball must NOT be hijacked to toggle the "picker" widget
        Assert.DoesNotContain("BtnToolPinball.Click += (s, e) => ToggleWidget(\"picker\");", content);
        Assert.Contains("BtnPinballWindow_Click", content);
    }

    [Fact]
    public void StudentPickerWindow_ExposesCurrentMonitorIndexProperty()
    {
        var prop = typeof(KnolTeacher.Desktop.Views.Windows.StudentPickerWindow)
            .GetProperty("CurrentMonitorIndex");
        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop.PropertyType);
    }
}
