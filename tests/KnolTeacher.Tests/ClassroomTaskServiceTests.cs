using System;
using System.IO;
using System.Linq;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Services;
using Xunit;

namespace KnolTeacher.Tests;

public sealed class ClassroomTaskServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "KnolTeacher.Task.Tests",
        Guid.NewGuid().ToString("N"));

    public ClassroomTaskServiceTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }
        catch { }
    }

    [Fact]
    public void InitialLoad_CreatesSampleTasks_AndCategorizesCorrectly()
    {
        var config = new ConfigService(_root);
        var service = new ClassroomTaskService(config);

        var all = service.GetAllTasks();
        var today = service.GetTodayTasks();
        var weekly = service.GetWeeklyTasks();

        Assert.NotEmpty(all);
        Assert.NotEmpty(today);
        Assert.NotEmpty(weekly);
        Assert.Equal(all.Count, today.Count + weekly.Count);
        Assert.True(File.Exists(Path.Combine(_root, "classroom_tasks.json")));
    }

    [Fact]
    public void AddTask_TodayAndWeekly_FiresTasksChangedEvent()
    {
        var config = new ConfigService(_root);
        var service = new ClassroomTaskService(config);

        int eventFiredCount = 0;
        service.TasksChanged += () => eventFiredCount++;

        var itemToday = service.AddTask("국어 활동지 제출", "Today", "제출", "3교시 전");
        var itemWeekly = service.AddTask("영어 단어 암기", "Weekly", "과제", "금요일까지");

        Assert.Equal(2, eventFiredCount);
        Assert.Equal("국어 활동지 제출", itemToday.Text);
        Assert.Equal("Today", itemToday.Category);
        Assert.Equal("Weekly", itemWeekly.Category);

        var todayList = service.GetTodayTasks();
        var weeklyList = service.GetWeeklyTasks();

        Assert.Contains(todayList, t => t.Id == itemToday.Id);
        Assert.Contains(weeklyList, t => t.Id == itemWeekly.Id);
    }

    [Fact]
    public void ToggleTaskCompletion_TogglesStateAndFiresEvent()
    {
        var config = new ConfigService(_root);
        var service = new ClassroomTaskService(config);

        var item = service.AddTask("테스트 과제", "Today", "과제");
        Assert.False(item.IsCompleted);

        bool eventFired = false;
        service.TasksChanged += () => eventFired = true;

        bool newState = service.ToggleTaskCompletion(item.Id);
        Assert.True(newState);
        Assert.True(eventFired);

        var updated = service.GetAllTasks().First(t => t.Id == item.Id);
        Assert.True(updated.IsCompleted);
    }

    [Fact]
    public void RemoveTask_DeletesItem_AndFiresEvent()
    {
        var config = new ConfigService(_root);
        var service = new ClassroomTaskService(config);

        var item = service.AddTask("삭제할 과제", "Today");
        bool eventFired = false;
        service.TasksChanged += () => eventFired = true;

        bool removed = service.RemoveTask(item.Id);
        Assert.True(removed);
        Assert.True(eventFired);

        Assert.DoesNotContain(service.GetAllTasks(), t => t.Id == item.Id);
    }

    [Fact]
    public void ClearCompletedTasks_ClearsTargetCategoryOrAll()
    {
        var config = new ConfigService(_root);
        var service = new ClassroomTaskService(config);

        int initialCompletedToday = service.GetTodayTasks().Count(t => t.IsCompleted);

        var t1 = service.AddTask("오늘 완료", "Today");
        var t2 = service.AddTask("오늘 미완료", "Today");
        var w1 = service.AddTask("이번주 완료", "Weekly");
        service.ToggleTaskCompletion(t1.Id);
        service.ToggleTaskCompletion(w1.Id);

        // 오늘 것만 완료 정리
        int clearedToday = service.ClearCompletedTasks("Today");
        Assert.Equal(initialCompletedToday + 1, clearedToday);
        Assert.DoesNotContain(service.GetTodayTasks(), t => t.Id == t1.Id);
        Assert.Contains(service.GetTodayTasks(), t => t.Id == t2.Id);
        Assert.Contains(service.GetWeeklyTasks(), t => t.Id == w1.Id);

        // 전체 완료 정리
        int clearedRemaining = service.ClearCompletedTasks(null);
        Assert.Equal(1, clearedRemaining);
        Assert.DoesNotContain(service.GetWeeklyTasks(), t => t.Id == w1.Id);
    }

    [Fact]
    public void UpdateTask_ModifiesProperties_AndFiresEvent()
    {
        var config = new ConfigService(_root);
        var service = new ClassroomTaskService(config);

        var item = service.AddTask("기존 과제", "Today", "과제", "3교시");
        bool eventFired = false;
        service.TasksChanged += () => eventFired = true;

        item.Text = "수정된 과제";
        item.Tag = "준비물";
        item.DueDateText = "4교시까지";
        service.UpdateTask(item);

        Assert.True(eventFired);
        var updated = service.GetTodayTasks().First(t => t.Id == item.Id);
        Assert.Equal("수정된 과제", updated.Text);
        Assert.Equal("준비물", updated.Tag);
        Assert.Equal("4교시까지", updated.DueDateText);
    }
}
