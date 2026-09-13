using System.Collections.Generic;

namespace KnolTeacher.Desktop.Models;

/// <summary>
/// Production contract for one large environment illustration that still needs
/// final transparent bitmap art. Environment art is deliberately visual-only:
/// it may frame the road, but it never contributes WPF hit testing or gameplay
/// collision by itself.
/// </summary>
public sealed record PendingRaceEnvironmentAsset(
    string Key,
    string FileName,
    string Period,
    string Purpose,
    string ReservedPropKey,
    int SourceWidth = 1536,
    int SourceHeight = 1024,
    double TransparentPaddingRatio = 0.08);

/// <summary>
/// Reserved large-scale scenery for the prehistoric race map.
///
/// These props are intentionally kept separate from historical relic landmarks.
/// They establish the visual identity of each era while remaining edge-anchored,
/// non-interactive scenery. Missing files are skipped by the renderer; do not
/// replace them with C# Path/SVG programmer art.
/// </summary>
public static class PrehistoryRaceEnvironmentV2
{
    public static IReadOnlyList<RaceMapProp> Props { get; } = new[]
    {
        // Paleolithic: cave / rocky hunting landscape.
        new RaceMapProp(
            "paleo-cave-entrance-slot",
            "prehistoric_paleo_cave_entrance.png",
            -130, 120, 250, 220, -22,
            RaceMapVisualRole.Decoration,
            Opacity: 0.98,
            Period: "구석기"),
        new RaceMapProp(
            "paleo-rock-cliff-slot",
            "prehistoric_paleo_rock_cliff.png",
            540, 620, 210, 260, -21,
            RaceMapVisualRole.Decoration,
            Opacity: 0.96,
            Period: "구석기"),

        // Neolithic: settled life beside water.
        new RaceMapProp(
            "neo-hut-slot",
            "prehistoric_neolithic_hut.png",
            -65, 1240, 225, 190, -22,
            RaceMapVisualRole.Decoration,
            Opacity: 0.98,
            Period: "신석기"),
        new RaceMapProp(
            "neo-reeds-slot",
            "prehistoric_neolithic_reeds.png",
            535, 2010, 180, 240, -21,
            RaceMapVisualRole.Decoration,
            Opacity: 0.94,
            Period: "신석기"),

        // Bronze Age: expanded agriculture and larger settlement context.
        new RaceMapProp(
            "bronze-field-slot",
            "prehistoric_bronze_field.png",
            -85, 2430, 240, 210, -22,
            RaceMapVisualRole.Decoration,
            Opacity: 0.96,
            Period: "청동기"),
        new RaceMapProp(
            "bronze-settlement-slot",
            "prehistoric_bronze_settlement.png",
            515, 2860, 230, 220, -21,
            RaceMapVisualRole.Decoration,
            Opacity: 0.97,
            Period: "청동기")
    };

    public static IReadOnlyList<PendingRaceEnvironmentAsset> PendingArtAssets { get; } = new[]
    {
        new PendingRaceEnvironmentAsset(
            "paleo-cave-entrance",
            "prehistoric_paleo_cave_entrance.png",
            "구석기",
            "구석기 시작부를 프레이밍하는 자연 동굴 입구",
            "paleo-cave-entrance-slot"),
        new PendingRaceEnvironmentAsset(
            "paleo-rock-cliff",
            "prehistoric_paleo_rock_cliff.png",
            "구석기",
            "구석기 사냥 지형의 바위 절벽/암벽 군집",
            "paleo-rock-cliff-slot"),
        new PendingRaceEnvironmentAsset(
            "neolithic-hut",
            "prehistoric_neolithic_hut.png",
            "신석기",
            "정착 생활을 보여 주는 신석기 움집",
            "neo-hut-slot"),
        new PendingRaceEnvironmentAsset(
            "neolithic-reeds",
            "prehistoric_neolithic_reeds.png",
            "신석기",
            "강가 정착 환경을 보여 주는 갈대와 수변 식생",
            "neo-reeds-slot"),
        new PendingRaceEnvironmentAsset(
            "bronze-field",
            "prehistoric_bronze_field.png",
            "청동기",
            "농경 확대를 보여 주는 곡식밭과 밭두렁",
            "bronze-field-slot"),
        new PendingRaceEnvironmentAsset(
            "bronze-settlement",
            "prehistoric_bronze_settlement.png",
            "청동기",
            "청동기 마을 규모감을 보여 주는 취락 가장자리",
            "bronze-settlement-slot")
    };
}
