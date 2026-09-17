using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Ink;
using KnolTeacher.Desktop.Views.Controls;
using Xunit;

namespace KnolTeacher.Tests;

public class MultiTouchInkHelperTests
{
    [Fact]
    public void Constructor_WithNullInkCanvas_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MultiTouchInkHelper(null!));
    }

    [Fact]
    public void Constructor_DisablesStandardEditingMode_AndInitializesCorrectly()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var helper = new MultiTouchInkHelper(canvas);

            Assert.Equal(InkCanvasEditingMode.None, canvas.EditingMode);
            Assert.False(helper.IsEraserMode);
        });
    }

    [Fact]
    public void IsEraserMode_CanBeToggled()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var helper = new MultiTouchInkHelper(canvas);

            helper.IsEraserMode = true;
            Assert.True(helper.IsEraserMode);

            helper.IsEraserMode = false;
            Assert.False(helper.IsEraserMode);
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new AggregateException("STA thread execution failed", threadEx);
        }
    }
}
