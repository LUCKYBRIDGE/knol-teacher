using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KnolTeacher.Desktop.Models;

public class ClassroomTaskItem : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _text = string.Empty;
    private string _category = "Today"; // "Today" or "Weekly"
    private string _tag = "과제"; // "과제", "준비물", "제출", "안내"
    private string _dueDateText = string.Empty;
    private bool _isCompleted;
    private DateTime _createdAt = DateTime.Now;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    /// <summary>
    /// 카테고리: "Today" (오늘 제출&할일) 또는 "Weekly" (이번주 제출&할일)
    /// </summary>
    public string Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    /// <summary>
    /// 유형 태그: 과제, 준비물, 제출, 안내 등
    /// </summary>
    public string Tag
    {
        get => _tag;
        set => SetField(ref _tag, value);
    }

    /// <summary>
    /// 마감 기한 또는 요일 안내 문구 (예: "금요일 1교시까지", "하교 전까지")
    /// </summary>
    public string DueDateText
    {
        get => _dueDateText;
        set => SetField(ref _dueDateText, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetField(ref _isCompleted, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetField(ref _createdAt, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
