using KnolTeacher.Desktop.Views.Controls.Widgets;
using Xunit;

namespace KnolTeacher.Tests;

public class QrWidgetContentTests
{
    [Theory]
    [InlineData("naver.com", "https://naver.com")]
    [InlineData("www.example.com/path?q=1", "https://www.example.com/path?q=1")]
    [InlineData("https://example.com/a", "https://example.com/a")]
    [InlineData("http://example.com/a", "http://example.com/a")]
    [InlineData("수업 준비물: 색연필", "수업 준비물: 색연필")]
    public void NormalizeQrContent_PreservesTextAndCompletesWebAddresses(string raw, string expected)
    {
        Assert.Equal(expected, QrWidgetView.NormalizeQrContent(raw));
    }

    [Fact]
    public void NormalizeQrContent_BlankUsesSafeDefault()
    {
        Assert.Equal("https://pinky-ne.com/", QrWidgetView.NormalizeQrContent("   "));
    }

    [Fact]
    public void LongContentWarning_OnlyAppearsForLongPayloads()
    {
        Assert.False(QrWidgetView.ShouldWarnAboutLongContent(new string('a', 139)));
        Assert.True(QrWidgetView.ShouldWarnAboutLongContent(new string('a', 140)));
    }
}
