using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using KnolTeacher.Desktop.Views.Controls;
using Xunit;

namespace KnolTeacher.Tests;

public class DrawingEraserHelperTests
{
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
            throw new AggregateException("STA thread failed", threadEx);
        }
    }

    private static Stroke CreateSampleStroke(double x, double y)
    {
        var pts = new StylusPointCollection
        {
            new StylusPoint(x, y),
            new StylusPoint(x + 10, y + 10),
            new StylusPoint(x + 20, y + 20)
        };
        return new Stroke(pts);
    }

    [Fact]
    public void DrawingUndoManager_PushAdded_And_Undo_RemovesStroke()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var undoMgr = new DrawingUndoManager();
            var stroke = CreateSampleStroke(50, 50);

            canvas.Strokes.Add(stroke);
            undoMgr.PushAdded(stroke);

            Assert.True(undoMgr.CanUndo);
            Assert.Equal(1, undoMgr.Count);

            bool undone = undoMgr.Undo(canvas);
            Assert.True(undone);
            Assert.Empty(canvas.Strokes);
            Assert.False(undoMgr.CanUndo);
        });
    }

    [Fact]
    public void DrawingUndoManager_PushRemoved_And_Undo_RestoresStrokes()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var undoMgr = new DrawingUndoManager();
            var s1 = CreateSampleStroke(10, 10);
            var s2 = CreateSampleStroke(30, 30);
            var s3 = CreateSampleStroke(60, 60);

            canvas.Strokes.Add(s1);
            canvas.Strokes.Add(s2);
            canvas.Strokes.Add(s3);

            // Simulate box/lasso eraser removing s1 and s2
            var removed = new StrokeCollection { s1, s2 };
            canvas.Strokes.Remove(removed);
            undoMgr.PushRemoved(removed);

            Assert.Single(canvas.Strokes);
            Assert.Equal(s3, canvas.Strokes[0]);

            // Undo should restore s1 and s2
            bool undone = undoMgr.Undo(canvas);
            Assert.True(undone);
            Assert.Equal(3, canvas.Strokes.Count);
            Assert.Contains(s1, canvas.Strokes);
            Assert.Contains(s2, canvas.Strokes);
            Assert.Contains(s3, canvas.Strokes);
        });
    }

    [Fact]
    public void DrawingEraserHelper_ModeSwitching_SetsEditingModesAndCursorsCorrectly()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var previewCanvas = new Canvas();
            var undoMgr = new DrawingUndoManager();
            var helper = new DrawingEraserHelper(canvas, previewCanvas, undoMgr);

            // 1. Pen
            helper.SetToolMode(EraserToolMode.Pen, 5);
            Assert.Equal(EraserToolMode.Pen, helper.CurrentMode);
            Assert.Equal(InkCanvasEditingMode.Ink, canvas.EditingMode);
            Assert.Equal(Cursors.Pen, canvas.Cursor);
            Assert.False(canvas.DefaultDrawingAttributes.IsHighlighter);
            Assert.Equal(5, canvas.DefaultDrawingAttributes.Width);

            // 2. Highlighter
            helper.SetToolMode(EraserToolMode.Highlighter);
            Assert.Equal(EraserToolMode.Highlighter, helper.CurrentMode);
            Assert.Equal(InkCanvasEditingMode.Ink, canvas.EditingMode);
            Assert.True(canvas.DefaultDrawingAttributes.IsHighlighter);

            // 3. Point Eraser
            helper.SetToolMode(EraserToolMode.Point);
            Assert.Equal(EraserToolMode.Point, helper.CurrentMode);
            Assert.Equal(InkCanvasEditingMode.EraseByPoint, canvas.EditingMode);
            Assert.Equal(Cursors.Cross, canvas.Cursor);

            // 4. Stroke Eraser
            helper.SetToolMode(EraserToolMode.Stroke);
            Assert.Equal(EraserToolMode.Stroke, helper.CurrentMode);
            Assert.Equal(InkCanvasEditingMode.EraseByStroke, canvas.EditingMode);
            Assert.Equal(Cursors.Cross, canvas.Cursor);

            // 5. Box Eraser
            helper.SetToolMode(EraserToolMode.Box);
            Assert.Equal(EraserToolMode.Box, helper.CurrentMode);
            Assert.Equal(InkCanvasEditingMode.None, canvas.EditingMode);
            Assert.Equal(Cursors.Cross, canvas.Cursor);

            // 6. Lasso Eraser
            helper.SetToolMode(EraserToolMode.Lasso);
            Assert.Equal(EraserToolMode.Lasso, helper.CurrentMode);
            Assert.Equal(InkCanvasEditingMode.None, canvas.EditingMode);
            Assert.Equal(Cursors.Cross, canvas.Cursor);
        });
    }

    [Fact]
    public void DrawingEraserHelper_ClearAll_ClearsStrokesAndUndo()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var previewCanvas = new Canvas();
            var undoMgr = new DrawingUndoManager();
            var helper = new DrawingEraserHelper(canvas, previewCanvas, undoMgr);

            var s1 = CreateSampleStroke(10, 10);
            canvas.Strokes.Add(s1);
            undoMgr.PushAdded(s1);

            Assert.Single(canvas.Strokes);
            Assert.True(undoMgr.CanUndo);

            helper.ClearAll();

            Assert.Empty(canvas.Strokes);
            Assert.False(undoMgr.CanUndo);
        });
    }
}
