using System;
using System.Collections.Generic;

namespace KnolTeacher.Desktop.Views.Controls;

/// <summary>
/// Single registry for NolBoard workspace metadata.
/// Keep dimensions here instead of scattering them across toolbar, restore and preset paths.
/// </summary>
public static class WidgetRegistry
{
    private static readonly IReadOnlyDictionary<string, WidgetDefinition> Definitions =
        new Dictionary<string, WidgetDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            // Defaults intentionally target classroom displays. StudentDisplayWindow clamps
            // them to the available board surface on smaller screens.
            ["timer"] = new("timer", "⏱️ 수업 타이머", 680, 480, 300, 220),
            ["picker"] = new("picker", "🎯 발표자 추첨", 720, 560, 320, 240),
            ["dice"] = new("dice", "🎲 스마트 주사위 & 통계", 960, 580, 360, 240),
            ["wheel"] = new("wheel", "🎡 회전 돌림판", 680, 540, 300, 230),
            ["score"] = new("score", "🏆 모둠 점수판", 720, 540, 320, 240),
            ["drawing"] = new("drawing", "✏️ 판서 메모장", 800, 620, 320, 260),
            ["blackboard"] = new("blackboard", "🟩 칠판", 900, 620, 420, 300),
            ["timetable"] = new("timetable", "📅 오늘의 시간표", 640, 880, 280, 340),
            ["meal"] = new("meal", "🍱 오늘의 급식", 640, 880, 280, 340),
            ["memo"] = new("memo", "📝 학급 알림장", 720, 680, 300, 260),
            ["checklist"] = new("checklist", "📋 과제 체크리스트", 720, 720, 300, 280),
            ["qr"] = new("qr", "📱 실시간 수업 QR코드", 640, 720, 280, 320),
            ["weather"] = new("weather", "☀️ 오늘의 날씨 & 미세먼지", 680, 580, 300, 250),
            ["dday"] = new("dday", "🎯 학급 D-Day", 680, 480, 300, 210)
        };

    public static bool TryGet(string? type, out WidgetDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(type) && Definitions.TryGetValue(type, out var found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }

    public static WidgetDefinition? GetOrDefault(string? type)
        => TryGet(type, out var definition) ? definition : null;
}
