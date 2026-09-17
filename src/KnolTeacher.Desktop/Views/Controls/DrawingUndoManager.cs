using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Ink;

namespace KnolTeacher.Desktop.Views.Controls;

/// <summary>
/// 판서 드로잉 실행 취소(Undo) 액션 인터페이스.
/// </summary>
public interface IDrawingUndoAction
{
    void Undo(InkCanvas inkCanvas);
}

/// <summary>
/// 새로 그린 획을 취소(삭제)하는 액션.
/// </summary>
public sealed class StrokeAddedUndoAction : IDrawingUndoAction
{
    private readonly Stroke _stroke;

    public StrokeAddedUndoAction(Stroke stroke)
    {
        _stroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
    }

    public void Undo(InkCanvas inkCanvas)
    {
        if (inkCanvas == null) return;
        if (inkCanvas.Strokes.Contains(_stroke))
        {
            inkCanvas.Strokes.Remove(_stroke);
        }
        else if (inkCanvas.Strokes.Count > 0)
        {
            inkCanvas.Strokes.RemoveAt(inkCanvas.Strokes.Count - 1);
        }
    }
}

/// <summary>
/// 구역 지우개, 영역 지우개, 획 지우개 등으로 삭제된 복수의 획들을 원상 복원하는 액션.
/// </summary>
public sealed class StrokesRemovedUndoAction : IDrawingUndoAction
{
    private readonly List<Stroke> _removedStrokes;

    public StrokesRemovedUndoAction(IEnumerable<Stroke> removedStrokes)
    {
        _removedStrokes = removedStrokes?.ToList() ?? new List<Stroke>();
    }

    public void Undo(InkCanvas inkCanvas)
    {
        if (inkCanvas == null || _removedStrokes.Count == 0) return;

        foreach (var stroke in _removedStrokes)
        {
            if (!inkCanvas.Strokes.Contains(stroke))
            {
                inkCanvas.Strokes.Add(stroke);
            }
        }
    }
}

/// <summary>
/// 판서 캔버스 전용 Undo 관리자.
/// </summary>
public class DrawingUndoManager
{
    private readonly Stack<IDrawingUndoAction> _undoStack = new();

    public int Count => _undoStack.Count;
    public bool CanUndo => _undoStack.Count > 0;

    public void PushAdded(Stroke stroke)
    {
        if (stroke == null) return;
        _undoStack.Push(new StrokeAddedUndoAction(stroke));
    }

    public void PushRemoved(IEnumerable<Stroke> strokes)
    {
        var list = strokes?.ToList();
        if (list == null || list.Count == 0) return;
        _undoStack.Push(new StrokesRemovedUndoAction(list));
    }

    public bool Undo(InkCanvas inkCanvas)
    {
        if (inkCanvas == null || _undoStack.Count == 0) return false;

        var action = _undoStack.Pop();
        action.Undo(inkCanvas);
        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
    }
}
