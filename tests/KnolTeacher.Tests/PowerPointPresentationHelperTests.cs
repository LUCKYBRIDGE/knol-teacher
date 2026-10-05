using System.IO;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public class PowerPointPresentationHelperTests
{
    [Fact]
    public void PpShowTypeWindow_IsConstantTwo()
    {
        // PowerPoint COM 명세에서 ppShowTypeWindow는 정확히 2여야 합니다 (창 모드 슬라이드 쇼).
        Assert.Equal(2, PowerPointPresentationHelper.PpShowTypeWindow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StartNonIntrusiveSlideShow_WithEmptyPath_ReturnsFailure(string? path)
    {
        var (success, message) = PowerPointPresentationHelper.StartNonIntrusiveSlideShow(path!);
        Assert.False(success);
        Assert.Contains("지정되지 않았습니다", message);
    }

    [Fact]
    public void StartNonIntrusiveSlideShow_WithNonExistentFile_ReturnsFailure()
    {
        string fakePath = Path.Combine(Path.GetTempPath(), "definitely_non_existent_file_12345.pptx");
        var (success, message) = PowerPointPresentationHelper.StartNonIntrusiveSlideShow(fakePath);
        Assert.False(success);
        Assert.Contains("찾을 수 없습니다", message);
    }

    [Fact]
    public void StartNonIntrusiveSlideShow_WithInvalidExtension_ReturnsFailure()
    {
        string tempTxtFile = Path.GetTempFileName();
        try
        {
            var (success, message) = PowerPointPresentationHelper.StartNonIntrusiveSlideShow(tempTxtFile);
            Assert.False(success);
            Assert.Contains("지원되지 않는 파일 형식", message);
        }
        finally
        {
            if (File.Exists(tempTxtFile)) File.Delete(tempTxtFile);
        }
    }

    [Fact]
    public void IsPowerPointInstalled_DoesNotThrow()
    {
        // 예외 없이 bool 값을 반환해야 함
        var ex = Record.Exception(() =>
        {
            bool installed = PowerPointPresentationHelper.IsPowerPointInstalled();
        });

        Assert.Null(ex);
    }
}
