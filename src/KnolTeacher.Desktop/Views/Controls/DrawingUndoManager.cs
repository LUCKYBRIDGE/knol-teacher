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

        // Remove only the exact stroke that was drawn. If it is already gone (erased, cleared),
        // undo must be a no-op instead of deleting some other, unrelated stroke.
        if (inkCanvas.Strokes.Contains(_stroke))
        {
            inkCanvas.Strokes.Remove(_stroke);
        }
    }
}

/// <summary>
/// 구역 지우개, 영역 지우개, 획 지우개, 전체 지우기 등으로 삭제된 복수의 획들을 원상 복원하는 액션.
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
/// 부분 지우개(EraseByPoint)처럼 원래 획이 잘린 조각들로 교체된 변경을 되돌리는 액션.
/// 조각을 제거하고 원래 획을 조각이 있던 위치(그리기 순서)에 복원합니다.
/// </summary>
public sealed class StrokesReplacedUndoAction : IDrawingUndoAction
{
    private readonly List<Stroke> _removedStrokes;
    private readonly List<Stroke> _addedStrokes;

    public StrokesReplacedUndoAction(IEnumerable<Stroke> removedStrokes, IEnumerable<Stroke> addedStrokes)
    {
        _removedStrokes = removedStrokes?.ToList() ?? new List<Stroke>();
        _addedStrokes = addedStrokes?.ToList() ?? new List<Stroke>();
    }

    public void Undo(InkCanvas inkCanvas)
    {
        if (inkCanvas == null) return;
        var strokes = inkCanvas.Strokes;

        // StrokeCollection.Replace inserts the fragments where the original stroke was.
        // Restore the original at the same index so the drawing order is preserved.
        int insertIndex = -1;
        foreach (var fragment in _addedStrokes)
        {
            int index = strokes.IndexOf(fragment);
            if (index >= 0 && (insertIndex < 0 || index < insertIndex))
            {
                insertIndex = index;
            }
        }

        foreach (var fragment in _addedStrokes)
        {
            if (strokes.Contains(fragment))
            {
                strokes.Remove(fragment);
            }
        }

        foreach (var original in _removedStrokes)
        {
            if (strokes.Contains(original)) continue;

            if (insertIndex >= 0 && insertIndex <= strokes.Count)
            {
                strokes.Insert(insertIndex, original);
                insertIndex++;
            }
            else
            {
                strokes.Add(original);
            }
        }
    }
}

/// <summary>
/// 한 번의 지우개 제스처처럼 여러 변경을 하나의 실행 취소 단계로 묶는 액션.
/// </summary>
public sealed class CompositeUndoAction : IDrawingUndoAction
{
    private readonly List<IDrawingUndoAction> _actions;

    public CompositeUndoAction(IEnumerable<IDrawingUndoAction> actions)
    {
        _actions = actions?.ToList() ?? new List<IDrawingUndoAction>();
    }

    public int Count => _actions.Count;

    public void Undo(InkCanvas inkCanvas)
    {
        // Undo in reverse order: later changes may depend on earlier ones (e.g. fragments of fragments).
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo(inkCanvas);
        }
    }
}

/// <summary>
/// 판서 캔버스 전용 Undo 관리자.
/// - 부분 지우개의 획 교체(Removed + Added)를 정확히 되돌립니다.
/// - BeginGroup/EndGroup으로 한 번의 지우개 제스처를 한 단계로 묶습니다.
/// - 하루 종일 열려 있는 판서 창에서도 메모리가 계속 늘지 않도록 이력 개수를 제한합니다.
/// </summary>
public class DrawingUndoManager
{
    public const int DefaultMaxHistory = 300;

    private readonly List<IDrawingUndoAction> _history = new();
    private readonly int _maxHistory;
    private List<IDrawingUndoAction>? _openGroup;

    public DrawingUndoManager(int maxHistory = DefaultMaxHistory)
    {
        _maxHistory = Math.Max(1, maxHistory);
    }

    public int Count => _history.Count + (_openGroup is { Count: > 0 } ? 1 : 0);
    public bool CanUndo => Count > 0;
    public bool IsGrouping => _openGroup != null;

    /// <summary>
    /// Starts collecting changes into one undo step. Any group that is still open is closed first,
    /// so calling this at the start of every gesture is safe.
    /// </summary>
    public void BeginGroup()
    {
        EndGroup();
        _openGroup = new List<IDrawingUndoAction>();
    }

    public void EndGroup()
    {
        var group = _openGroup;
        _openGroup = null;
        if (group == null || group.Count == 0) return;

        PushCore(group.Count == 1 ? group[0] : new CompositeUndoAction(group));
    }

    public void PushAdded(Stroke stroke)
    {
        if (stroke == null) return;
        Push(new StrokeAddedUndoAction(stroke));
    }

    public void PushRemoved(IEnumerable<Stroke> strokes)
    {
        var list = strokes?.ToList();
        if (list == null || list.Count == 0) return;
        Push(new StrokesRemovedUndoAction(list));
    }

    /// <summary>
    /// Records a replacement such as a point-erase split (original stroke → fragments).
    /// A replacement without fragments is a plain removal.
    /// </summary>
    public void PushReplaced(IEnumerable<Stroke> removedStrokes, IEnumerable<Stroke> addedStrokes)
    {
        var removed = removedStrokes?.ToList() ?? new List<Stroke>();
        var added = addedStrokes?.ToList() ?? new List<Stroke>();
        if (removed.Count == 0 && added.Count == 0) return;

        if (added.Count == 0)
        {
            Push(new StrokesRemovedUndoAction(removed));
            return;
        }

        Push(new StrokesReplacedUndoAction(removed, added));
    }

    public bool Undo(InkCanvas inkCanvas)
    {
        if (inkCanvas == null) return false;

        EndGroup();
        if (_history.Count == 0) return false;

        var action = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        action.Undo(inkCanvas);
        return true;
    }

    public void Clear()
    {
        _openGroup = null;
        _history.Clear();
    }

    private void Push(IDrawingUndoAction action)
    {
        if (_openGroup != null)
        {
            _openGroup.Add(action);
            return;
        }

        PushCore(action);
    }

    private void PushCore(IDrawingUndoAction action)
    {
        _history.Add(action);
        if (_history.Count > _maxHistory)
        {
            _history.RemoveRange(0, _history.Count - _maxHistory);
        }
    }
}
