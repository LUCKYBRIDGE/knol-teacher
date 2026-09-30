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

    [Fact]
    public void DrawingUndoManager_UndoPartialErase_RestoresOriginalInPlaceAndDropsFragments()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var undoMgr = new DrawingUndoManager();
            var before = CreateSampleStroke(10, 10);
            var original = CreateSampleStroke(40, 40);
            var after = CreateSampleStroke(80, 80);
            canvas.Strokes.Add(before);
            canvas.Strokes.Add(original);
            canvas.Strokes.Add(after);

            // What EraseByPoint does: replace the stroke with its remaining fragments at the same index.
            var fragmentA = CreateSampleStroke(40, 40);
            var fragmentB = CreateSampleStroke(55, 55);
            var fragments = new StrokeCollection { fragmentA, fragmentB };
            canvas.Strokes.Replace(original, fragments);
            undoMgr.PushReplaced(new[] { original }, fragments);

            Assert.True(undoMgr.Undo(canvas));

            Assert.Equal(3, canvas.Strokes.Count);
            Assert.Same(before, canvas.Strokes[0]);
            Assert.Same(original, canvas.Strokes[1]);
            Assert.Same(after, canvas.Strokes[2]);
            Assert.DoesNotContain(fragmentA, canvas.Strokes);
            Assert.DoesNotContain(fragmentB, canvas.Strokes);
        });
    }

    [Fact]
    public void DrawingUndoManager_StaleAddedAction_DoesNotDeleteAnUnrelatedStroke()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var undoMgr = new DrawingUndoManager();
            var drawn = CreateSampleStroke(10, 10);
            var other = CreateSampleStroke(60, 60);

            canvas.Strokes.Add(drawn);
            undoMgr.PushAdded(drawn);

            // The recorded stroke disappears outside the undo history, then another stroke appears.
            canvas.Strokes.Remove(drawn);
            canvas.Strokes.Add(other);

            Assert.True(undoMgr.Undo(canvas));
            Assert.Single(canvas.Strokes);
            Assert.Same(other, canvas.Strokes[0]);
        });
    }

    [Fact]
    public void DrawingUndoManager_Group_UndoesTheWholeEraserGestureInOneStep()
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

            undoMgr.BeginGroup();
            canvas.Strokes.Remove(s1);
            undoMgr.PushRemoved(new[] { s1 });
            canvas.Strokes.Remove(s2);
            undoMgr.PushRemoved(new[] { s2 });
            undoMgr.EndGroup();

            Assert.Equal(1, undoMgr.Count);
            Assert.True(undoMgr.Undo(canvas));
            Assert.Equal(3, canvas.Strokes.Count);
            Assert.False(undoMgr.CanUndo);
        });
    }

    [Fact]
    public void DrawingUndoManager_History_IsCapped()
    {
        var undoMgr = new DrawingUndoManager(maxHistory: 3);
        for (int i = 0; i < 5; i++)
        {
            undoMgr.PushAdded(CreateSampleStroke(i * 10, i * 10));
        }

        Assert.Equal(3, undoMgr.Count);
    }

    [Fact]
    public void DrawingEraserHelper_PointEraseSplit_IsRecordedAndUndoable()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var previewCanvas = new Canvas();
            var undoMgr = new DrawingUndoManager();
            var helper = new DrawingEraserHelper(canvas, previewCanvas, undoMgr);

            var original = CreateSampleStroke(40, 40);
            canvas.Strokes.Add(original);
            helper.SetToolMode(EraserToolMode.Point);

            var fragments = new StrokeCollection { CreateSampleStroke(40, 40), CreateSampleStroke(60, 60) };
            canvas.Strokes.Replace(original, fragments);

            Assert.True(helper.Undo());
            Assert.Single(canvas.Strokes);
            Assert.Same(original, canvas.Strokes[0]);
        });
    }

    [Fact]
    public void DrawingEraserHelper_UndoableClearAll_RestoresEveryStrokeInOrder()
    {
        RunInSta(() =>
        {
            var canvas = new InkCanvas();
            var previewCanvas = new Canvas();
            var undoMgr = new DrawingUndoManager();
            var helper = new DrawingEraserHelper(canvas, previewCanvas, undoMgr);

            var s1 = CreateSampleStroke(10, 10);
            var s2 = CreateSampleStroke(40, 40);
            canvas.Strokes.Add(s1);
            canvas.Strokes.Add(s2);

            helper.ClearAll(undoable: true);
            Assert.Empty(canvas.Strokes);
            Assert.True(undoMgr.CanUndo);

            Assert.True(helper.Undo());
            Assert.Equal(2, canvas.Strokes.Count);
            Assert.Same(s1, canvas.Strokes[0]);
            Assert.Same(s2, canvas.Strokes[1]);
        });
    }
}
