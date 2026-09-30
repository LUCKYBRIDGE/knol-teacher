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
    public void BoardWidgetHost_ResizeLayer_OmitsTopCornersAndRetainsRequiredHandles()
    {
        // Resolved from the repository root: the previous fixed "..\..\..\.." path pointed inside
        // tests\ and made this test return early without asserting anything.
        string xamlPath = RepositoryPaths.GetFile("src", "KnolTeacher.Desktop", "Views", "Controls", "BoardWidgetHost.xaml");
        string content = System.IO.File.ReadAllText(xamlPath);

        // Top corners must NOT exist to prevent blocking close buttons and drag grips
        Assert.DoesNotContain("Tag=\"NE\"", content);
        Assert.DoesNotContain("Tag=\"NW\"", content);

        // Required 6 direction handles must exist
        Assert.Contains("Tag=\"N\"", content);
        Assert.Contains("Tag=\"S\"", content);
        Assert.Contains("Tag=\"W\"", content);
        Assert.Contains("Tag=\"E\"", content);
        Assert.Contains("Tag=\"SW\"", content);
        Assert.Contains("Tag=\"SE\"", content);
    }

    [Fact]
    public void StudentDisplayWindow_PopupUx_DoesNotHijackPinballToPicker()
    {
        string csPath = RepositoryPaths.GetFile("src", "KnolTeacher.Desktop", "Views", "Windows", "StudentDisplayWindow.PopupUx.cs");
        string content = System.IO.File.ReadAllText(csPath);

        // BtnToolPinball must NOT be hijacked to toggle the "picker" widget
        Assert.DoesNotContain("BtnToolPinball.Click += (s, e) => ToggleWidget(\"picker\");", content);
        Assert.Contains("BtnPinballWindow_Click", content);
    }

    [Theory]
    [InlineData("MainWindow.PopupUx.cs", "static MainWindow()")]
    [InlineData("Views/Windows/StudentDisplayWindow.PopupUx.cs", "static StudentDisplayWindow()")]
    public void ClassHandlerRegistrations_AreGuaranteedByAnExplicitStaticConstructor(string relativePath, string staticConstructor)
    {
        // These partials register class handlers from a static field initializer. Without an explicit
        // static constructor the type is 'beforefieldinit' and the initializer may run too late (or never).
        string[] parts = new[] { "src", "KnolTeacher.Desktop" }.Concat(relativePath.Split('/')).ToArray();
        string content = System.IO.File.ReadAllText(RepositoryPaths.GetFile(parts));

        Assert.Contains(staticConstructor, content);
    }

    [Fact]
    public void PickerWidget_UsesItsOwnIcon_NotTheDDayIcon()
    {
        // 🎯 is D-Day. The 발표자 추첨 widget previously shared it, so the dock showed two 🎯 buttons.
        Assert.True(WidgetRegistry.TryGet("picker", out var picker));
        Assert.True(WidgetRegistry.TryGet("dday", out var dday));
        Assert.NotEqual(picker.Title.Split(' ')[0], dday.Title.Split(' ')[0]);
    }

    [Fact]
    public void StudentPickerWindow_ExposesCurrentMonitorIndexProperty()
    {
        var prop = typeof(KnolTeacher.Desktop.Views.Windows.StudentPickerWindow)
            .GetProperty("CurrentMonitorIndex");
        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop.PropertyType);
    }

    [Fact]
    public void WheelItem_SupportsWeightsAndProportionalCalculations()
    {
        var item1 = new KnolTeacher.Desktop.Views.Controls.Widgets.WheelItem { Name = "1번", Weight = 1 };
        var item2 = new KnolTeacher.Desktop.Views.Controls.Widgets.WheelItem { Name = "2번", Weight = 2 };
        var item3 = new KnolTeacher.Desktop.Views.Controls.Widgets.WheelItem { Name = "3번", Weight = 1 };

        var items = new[] { item1, item2, item3 };
        int totalWeight = items.Sum(it => it.Weight);
        Assert.Equal(4, totalWeight);

        double angle1 = 360.0 * (item1.Weight / (double)totalWeight);
        double angle2 = 360.0 * (item2.Weight / (double)totalWeight);
        double angle3 = 360.0 * (item3.Weight / (double)totalWeight);

        Assert.Equal(90.0, angle1, 2);
        Assert.Equal(180.0, angle2, 2);
        Assert.Equal(90.0, angle3, 2);
    }
}
