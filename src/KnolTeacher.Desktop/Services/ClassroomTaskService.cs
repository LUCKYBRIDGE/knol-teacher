using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using KnolTeacher.Desktop.Models;

namespace KnolTeacher.Desktop.Services;

public class ClassroomTaskService : IClassroomTaskService
{
    private readonly IConfigService _configService;
    private readonly object _syncLock = new();
    private List<ClassroomTaskItem> _tasks = new();
    private bool _isLoaded = false;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public event Action? TasksChanged;

    public ClassroomTaskService(IConfigService configService)
    {
        _configService = configService;
    }

    private string FilePath => Path.Combine(_configService.ConfigDir, "classroom_tasks.json");

    private void EnsureLoaded()
    {
        lock (_syncLock)
        {
            if (_isLoaded) return;
            LoadInternal();
            _isLoaded = true;
        }
    }

    private void LoadInternal()
    {
        try
        {
            if (SafeLocalJsonStore.TryLoad<List<ClassroomTaskItem>>(FilePath, JsonOptions, out var loaded) && loaded != null)
            {
                _tasks = loaded;
                return;
            }
        }
        catch
        {
            // fallback to default
        }

        // 초기 기본 샘플 태스크 세팅
        _tasks = CreateDefaultSampleTasks();
        SaveInternal();
    }

    private void SaveInternal()
    {
        try
        {
            SafeLocalJsonStore.TrySave(FilePath, _tasks, JsonOptions);
        }
        catch
        {
            // file save exception safety
        }
    }

    private static List<ClassroomTaskItem> CreateDefaultSampleTasks()
    {
        return new List<ClassroomTaskItem>
        {
            new ClassroomTaskItem
            {
                Text = "수학익힘책 42~45쪽 풀어서 4교시 전 제출",
                Category = "Today",
                Tag = "과제",
                DueDateText = "4교시 전까지",
                IsCompleted = false
            },
            new ClassroomTaskItem
            {
                Text = "미술 준비물 (물감, 파레트, 붓 세트)",
                Category = "Today",
                Tag = "준비물",
                DueDateText = "오늘 5교시",
                IsCompleted = false
            },
            new ClassroomTaskItem
            {
                Text = "현장체험학습 희망 설문지 담임선생님께 제출",
                Category = "Today",
                Tag = "제출",
                DueDateText = "하교 전까지",
                IsCompleted = true
            },
            new ClassroomTaskItem
            {
                Text = "주말 독서록 1편 정성껏 작성하기",
                Category = "Weekly",
                Tag = "과제",
                DueDateText = "금요일 아침까지",
                IsCompleted = false
            },
            new ClassroomTaskItem
            {
                Text = "과학 페트병(500ml) 1개씩 챙겨오기",
                Category = "Weekly",
                Tag = "준비물",
                DueDateText = "목요일 2교시까지",
                IsCompleted = false
            }
        };
    }

    public IReadOnlyList<ClassroomTaskItem> GetAllTasks()
    {
        EnsureLoaded();
        lock (_syncLock)
        {
            return _tasks.ToList();
        }
    }

    public IReadOnlyList<ClassroomTaskItem> GetTodayTasks()
    {
        EnsureLoaded();
        lock (_syncLock)
        {
            return _tasks.Where(t => string.Equals(t.Category, "Today", StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    public IReadOnlyList<ClassroomTaskItem> GetWeeklyTasks()
    {
        EnsureLoaded();
        lock (_syncLock)
        {
            return _tasks.Where(t => string.Equals(t.Category, "Weekly", StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    public ClassroomTaskItem AddTask(string text, string category = "Today", string tag = "과제", string dueDateText = "")
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Task text cannot be empty.", nameof(text));

        EnsureLoaded();
        var item = new ClassroomTaskItem
        {
            Text = text.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Today" : category.Trim(),
            Tag = string.IsNullOrWhiteSpace(tag) ? "과제" : tag.Trim(),
            DueDateText = dueDateText?.Trim() ?? string.Empty,
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        lock (_syncLock)
        {
            _tasks.Add(item);
            SaveInternal();
        }

        TasksChanged?.Invoke();
        return item;
    }

    public void UpdateTask(ClassroomTaskItem task)
    {
        if (task == null) return;

        EnsureLoaded();
        lock (_syncLock)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                existing.Text = task.Text;
                existing.Category = task.Category;
                existing.Tag = task.Tag;
                existing.DueDateText = task.DueDateText;
                existing.IsCompleted = task.IsCompleted;
                SaveInternal();
            }
        }

        TasksChanged?.Invoke();
    }

    public bool RemoveTask(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        EnsureLoaded();
        bool removed = false;
        lock (_syncLock)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == id);
            if (existing != null)
            {
                removed = _tasks.Remove(existing);
                if (removed)
                {
                    SaveInternal();
                }
            }
        }

        if (removed)
        {
            TasksChanged?.Invoke();
        }

        return removed;
    }

    public bool ToggleTaskCompletion(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        EnsureLoaded();
        bool newState = false;
        bool found = false;

        lock (_syncLock)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == id);
            if (existing != null)
            {
                existing.IsCompleted = !existing.IsCompleted;
                newState = existing.IsCompleted;
                found = true;
                SaveInternal();
            }
        }

        if (found)
        {
            TasksChanged?.Invoke();
        }

        return newState;
    }

    public int ClearCompletedTasks(string? category = null)
    {
        EnsureLoaded();
        int count = 0;

        lock (_syncLock)
        {
            var toRemove = _tasks
                .Where(t => t.IsCompleted && (category == null || string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            count = toRemove.Count;
            if (count > 0)
            {
                foreach (var item in toRemove)
                {
                    _tasks.Remove(item);
                }
                SaveInternal();
            }
        }

        if (count > 0)
        {
            TasksChanged?.Invoke();
        }

        return count;
    }

    public void ResetSampleTasks()
    {
        EnsureLoaded();
        lock (_syncLock)
        {
            _tasks = CreateDefaultSampleTasks();
            SaveInternal();
        }

        TasksChanged?.Invoke();
    }
}
