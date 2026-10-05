using System;
using System.Collections.Generic;
using KnolTeacher.Desktop.Models;

namespace KnolTeacher.Desktop.Services;

public interface IClassroomTaskService
{
    IReadOnlyList<ClassroomTaskItem> GetAllTasks();
    IReadOnlyList<ClassroomTaskItem> GetTodayTasks();
    IReadOnlyList<ClassroomTaskItem> GetWeeklyTasks();
    ClassroomTaskItem AddTask(string text, string category = "Today", string tag = "과제", string dueDateText = "");
    void UpdateTask(ClassroomTaskItem task);
    bool RemoveTask(string id);
    bool ToggleTaskCompletion(string id);
    int ClearCompletedTasks(string? category = null);
    void ResetSampleTasks();
    event Action? TasksChanged;
}
