using System.Collections.Generic;

namespace KnolTeacher.Desktop.Models;

public enum RaceMapVisualRole
{
    Decoration,
    Landmark,
    GameplayObstacleVisual,
    Foreground
}

public enum RaceMapInteractionRole
{
    None,
    StaticBumper,
    Breakable,
    StaticCollider,
    DynamicObstacle
}

public enum RaceColliderShape
{
    None,
    Circle,
    Capsule,
    Composite
}

public sealed record RaceMapProp(
    string Key,
    string AssetName,
    double X,
    double Y,
    double Width,
    double Height,
    int ZIndex,
    RaceMapVisualRole Role,
    bool FlipX = false,
    double Opacity = 1.0,
    string? Label = null,
    string? Period = null,
    RaceMapInteractionRole Interaction = RaceMapInteractionRole.None,
    string? GameplayColliderKey = null);

public sealed record InteractiveRelicRule(
    string Key,
    string AssetName,
    string Label,
    string Period,
    RaceMapInteractionRole Interaction,
    RaceColliderShape ColliderShape,
    string BehaviorKey);

/// <summary>
/// One concrete occurrence of an interactive relic on the race course.
/// The collider remains deliberately simple while VisualWidth/VisualHeight may
/// be larger than the hit circle so the transparent PNG stays readable.
/// </summary>
public sealed record InteractiveRelicPlacement(
    string Key,
    string RuleKey,
    double X,
    double Y,
    double ColliderRadius,
    double VisualWidth,
    double VisualHeight,
    int ZIndex = -1);

/// <summary>
/// Declares the visual-to-simulation binding for the four large static rock
/// obstacles. The actual lightweight resolution remains in the existing race
/// simulation; these values make the intended collider footprint testable
/// without treating PNG bounds as physics.
/// </summary>
public sealed record RaceStaticColliderBinding(
    string Key,
    double CenterX,
    double StartY,
    double EndY,
    RaceColliderShape Shape);

/// <summary>
/// Contract for a historical illustration that still needs final production.
/// Source dimensions are production targets, not runtime decode sizes. The
/// renderer still caps decoded bitmap width independently for low-spec PCs.
/// </summary>
public sealed record PendingRaceArtAsset(
    string Key,
    string FileName,
    string Label,
    string Period,
    string Purpose,
    int SourceWidth = 1024,
    int SourceHeight = 1024,
    double TransparentPaddingRatio = 0.10,
    RaceMapInteractionRole IntendedInteraction = RaceMapInteractionRole.None);

/// <summary>
/// Lightweight visual-map contract for the vertical prehistoric picker race.
///
/// Visual composition and simulation are separate. Static map props use bitmap
/// art and never rely on WPF hit-testing. When a visual must correspond to a
/// physical obstacle (for example the river rocks), the mapping is explicit.
/// Historical relics that are intentionally part of gameplay are described by
/// InteractiveRelics plus InteractiveRelicPlacements so a relic can be a
/// decorative landmark in one place and an obstacle in another without
/// conflating its PNG rectangle with its hitbox.
///
/// Final map art must be transparent PNG/WebP. Do not add SVG/Path substitutes
/// for missing historical assets; add the real asset to assets/race instead.
/// Missing final art may still have a reserved RaceMapProp slot: the renderer
/// skips it until the bitmap exists, so no fake visual fallback is introduced.
/// </summary>
public static class PrehistoryRaceMapV2
{
    public const string AssetRoot = "assets/race/";

    public static IReadOnlyList<RaceMapProp> Props { get; } = new[]
    {
        new RaceMapProp(
            "paleo-root-left",
            "giant_root.png",
            -18, 205, 205, 178, -18,
            RaceMapVisualRole.Decoration,
            Opacity: 0.94),
        new RaceMapProp(
            "paleo-handaxe",
            "cartoon_handaxe.png",
            548, 420, 100, 100, -7,
            RaceMapVisualRole.Landmark,
            Label: "주먹도끼",
            Period: "구석기"),
        new RaceMapProp(
            "paleo-chopper",
            "cartoon_chipped_stone.png",
            22, 505, 96, 96, -7,
            RaceMapVisualRole.Landmark,
            Label: "찍개",
            Period: "구석기"),

        new RaceMapProp(
            "paleo-fallen-log-left",
            "fallen_log.png",
            -34, 690, 220, 130, -16,
            RaceMapVisualRole.Decoration,
            Opacity: 0.96),
        new RaceMapProp(
            "paleo-bone-needle-slot",
            "prehistoric_bone_needle.png",
            18, 870, 96, 130, -7,
            RaceMapVisualRole.Landmark,
            Label: "뼈바늘",
            Period: "구석기"),
        new RaceMapProp(
            "paleo-stump-right",
            "cartoon_wood_stump.png",
            510, 895, 150, 150, -15,
            RaceMapVisualRole.Decoration,
            FlipX: true,
            Opacity: 0.96),
        new RaceMapProp(
            "paleo-branch-right",
            "wood_branch_right.png",
            500, 1080, 200, 120, -16,
            RaceMapVisualRole.Decoration,
            Opacity: 0.92),

        // These PNG stones intentionally correspond to the existing lightweight
        // static-island collision geometry. The image bounds themselves are never
        // used for collision.
        new RaceMapProp(
            "river-rock-main",
            "river_stone.png",
            235, 1360, 210, 280, -4,
            RaceMapVisualRole.GameplayObstacleVisual,
            Interaction: RaceMapInteractionRole.StaticCollider,
            GameplayColliderKey: "river-rock-main"),
        new RaceMapProp(
            "river-rock-upper",
            "river_stone.png",
            296, 1680, 88, 130, -4,
            RaceMapVisualRole.GameplayObstacleVisual,
            Interaction: RaceMapInteractionRole.StaticCollider,
            GameplayColliderKey: "river-rock-upper"),
        new RaceMapProp(
            "river-rock-lower-left",
            "river_stone.png",
            185, 1830, 90, 145, -4,
            RaceMapVisualRole.GameplayObstacleVisual,
            Interaction: RaceMapInteractionRole.StaticCollider,
            GameplayColliderKey: "river-rock-lower-left"),
        new RaceMapProp(
            "river-rock-lower-right",
            "river_stone.png",
            405, 1830, 90, 145, -4,
            RaceMapVisualRole.GameplayObstacleVisual,
            FlipX: true,
            Interaction: RaceMapInteractionRole.StaticCollider,
            GameplayColliderKey: "river-rock-lower-right"),
        new RaceMapProp(
            "neo-polished-stone",
            "cartoon_polished_stone.png",
            555, 1265, 94, 94, -7,
            RaceMapVisualRole.Landmark,
            Label: "간석기",
            Period: "신석기"),
        new RaceMapProp(
            "neo-spindle-whorl-slot",
            "prehistoric_spindle_whorl.png",
            548, 1510, 100, 100, -7,
            RaceMapVisualRole.Landmark,
            Label: "가락바퀴",
            Period: "신석기"),

        // This is a map-side landmark. The actual breakable pottery obstacles are
        // gameplay objects created by the simulation and use the same art family.
        new RaceMapProp(
            "neo-pottery-left",
            "cartoon_comb_pottery.png",
            18, 2060, 110, 135, -9,
            RaceMapVisualRole.Landmark,
            Label: "빗살무늬 토기",
            Period: "신석기"),
        new RaceMapProp(
            "neo-shell-mask-slot",
            "prehistoric_shell_mask.png",
            548, 2130, 105, 118, -7,
            RaceMapVisualRole.Landmark,
            Label: "조개 껍데기 가면",
            Period: "신석기"),
        new RaceMapProp(
            "neo-log-right",
            "fallen_log_right.png",
            505, 2240, 205, 118, -15,
            RaceMapVisualRole.Decoration,
            Opacity: 0.95),

        new RaceMapProp(
            "bronze-half-moon-knife-slot",
            "prehistoric_half_moon_stone_knife.png",
            540, 2470, 120, 100, -7,
            RaceMapVisualRole.Landmark,
            Label: "반달 돌칼",
            Period: "청동기"),
        new RaceMapProp(
            "bronze-plain-pottery-slot",
            "prehistoric_plain_pottery.png",
            18, 2700, 112, 142, -7,
            RaceMapVisualRole.Landmark,
            Label: "민무늬 토기",
            Period: "청동기"),
        new RaceMapProp(
            "bronze-dagger-slot",
            "prehistoric_bronze_dagger.png",
            535, 2915, 120, 155, -7,
            RaceMapVisualRole.Landmark,
            Label: "비파형 동검",
            Period: "청동기"),
        new RaceMapProp(
            "bronze-dolmen-left",
            "cartoon_dolmen.png",
            5, 3005, 220, 170, -6,
            RaceMapVisualRole.Landmark,
            Label: "고인돌",
            Period: "청동기"),
        new RaceMapProp(
            "bronze-dolmen-right",
            "cartoon_dolmen.png",
            482, 3205, 178, 138, -6,
            RaceMapVisualRole.Decoration,
            FlipX: true,
            Opacity: 0.92),

        new RaceMapProp(
            "foreground-branch-left",
            "wood_branch.png",
            -70, 2460, 190, 115, 4,
            RaceMapVisualRole.Foreground,
            Opacity: 0.82),
        new RaceMapProp(
            "foreground-branch-right",
            "wood_branch_right.png",
            560, 2790, 190, 115, 4,
            RaceMapVisualRole.Foreground,
            Opacity: 0.82)
    };

    /// <summary>
    /// Relics that are intentionally allowed to affect the race. These rules do
    /// not make every occurrence interactive; level placement decides where the
    /// mechanic is actually instantiated.
    /// </summary>
    public static IReadOnlyList<InteractiveRelicRule> InteractiveRelics { get; } = new[]
    {
        new InteractiveRelicRule(
            "handaxe-bumper",
            "cartoon_handaxe.png",
            "주먹도끼",
            "구석기",
            RaceMapInteractionRole.StaticBumper,
            RaceColliderShape.Circle,
            "stone-tool-bumper"),
        new InteractiveRelicRule(
            "chopper-bumper",
            "cartoon_chipped_stone.png",
            "찍개",
            "구석기",
            RaceMapInteractionRole.StaticBumper,
            RaceColliderShape.Circle,
            "stone-tool-bumper"),
        new InteractiveRelicRule(
            "polished-stone-bumper",
            "cartoon_polished_stone.png",
            "간석기",
            "신석기",
            RaceMapInteractionRole.StaticBumper,
            RaceColliderShape.Circle,
            "stone-tool-bumper"),
        new InteractiveRelicRule(
            "comb-pottery-breakable",
            "cartoon_comb_pottery.png",
            "빗살무늬 토기",
            "신석기",
            RaceMapInteractionRole.Breakable,
            RaceColliderShape.Circle,
            "breakable-pottery")
    };

    /// <summary>
    /// Concrete gameplay occurrences. Their eras intentionally match the map
    /// zones: Paleolithic tools stay above Y=1200, while polished stone and comb
    /// pottery stay in Neolithic zones. No comb pottery is used to plug the
    /// 80-pixel Bronze Age finish chute.
    /// </summary>
    public static IReadOnlyList<InteractiveRelicPlacement> InteractiveRelicPlacements { get; } = new[]
    {
        new InteractiveRelicPlacement(
            "paleo-handaxe-bumper-1", "handaxe-bumper",
            340, 330, 15, 48, 48),
        new InteractiveRelicPlacement(
            "paleo-chopper-bumper-1", "chopper-bumper",
            180, 410, 15, 48, 48),
        new InteractiveRelicPlacement(
            "paleo-handaxe-bumper-2", "handaxe-bumper",
            210, 760, 15, 48, 48),
        new InteractiveRelicPlacement(
            "paleo-chopper-bumper-2", "chopper-bumper",
            460, 1040, 15, 48, 48),

        new InteractiveRelicPlacement(
            "neo-polished-bumper-1", "polished-stone-bumper",
            500, 1280, 15, 48, 48),
        new InteractiveRelicPlacement(
            "neo-pottery-breakable-1", "comb-pottery-breakable",
            220, 1660, 16, 50, 62),
        new InteractiveRelicPlacement(
            "neo-pottery-breakable-2", "comb-pottery-breakable",
            465, 1740, 16, 50, 62),
        new InteractiveRelicPlacement(
            "neo-pottery-breakable-3", "comb-pottery-breakable",
            250, 2030, 16, 50, 62),
        new InteractiveRelicPlacement(
            "neo-pottery-breakable-4", "comb-pottery-breakable",
            440, 2030, 16, 50, 62),
        new InteractiveRelicPlacement(
            "neo-polished-bumper-2", "polished-stone-bumper",
            255, 2260, 15, 48, 48)
    };

    /// <summary>
    /// Explicit bindings for the four large PNG rock obstacles. The simulation
    /// currently resolves these with its existing composite island watchdogs.
    /// Keeping the footprints here lets tests catch visual/physics drift while
    /// avoiding a second heavyweight collision system.
    /// </summary>
    public static IReadOnlyList<RaceStaticColliderBinding> StaticColliderBindings { get; } = new[]
    {
        new RaceStaticColliderBinding(
            "river-rock-main", 340, 1360, 1640, RaceColliderShape.Composite),
        new RaceStaticColliderBinding(
            "river-rock-upper", 340, 1680, 1810, RaceColliderShape.Composite),
        new RaceStaticColliderBinding(
            "river-rock-lower-left", 230, 1830, 1975, RaceColliderShape.Composite),
        new RaceStaticColliderBinding(
            "river-rock-lower-right", 450, 1830, 1975, RaceColliderShape.Composite)
    };

    /// <summary>
    /// Historical assets intentionally left as explicit art slots. They are not
    /// replaced with programmer-drawn SVG/Path approximations. The reserved prop
    /// slots above define where each illustration will appear once produced.
    /// </summary>
    public static IReadOnlyList<PendingRaceArtAsset> PendingArtAssets { get; } = new[]
    {
        new PendingRaceArtAsset(
            "bone-needle",
            "prehistoric_bone_needle.png",
            "뼈바늘",
            "구석기",
            "구석기 생활 랜드마크"),
        new PendingRaceArtAsset(
            "spindle-whorl",
            "prehistoric_spindle_whorl.png",
            "가락바퀴",
            "신석기",
            "신석기 직조 생활 랜드마크"),
        new PendingRaceArtAsset(
            "shell-mask",
            "prehistoric_shell_mask.png",
            "조개 껍데기 가면",
            "신석기",
            "신석기 해안 문화 랜드마크"),
        new PendingRaceArtAsset(
            "half-moon-stone-knife",
            "prehistoric_half_moon_stone_knife.png",
            "반달 돌칼",
            "청동기",
            "청동기 농경 랜드마크"),
        new PendingRaceArtAsset(
            "plain-pottery",
            "prehistoric_plain_pottery.png",
            "민무늬 토기",
            "청동기",
            "청동기 생활 랜드마크",
            IntendedInteraction: RaceMapInteractionRole.Breakable),
        new PendingRaceArtAsset(
            "bronze-dagger",
            "prehistoric_bronze_dagger.png",
            "비파형 동검",
            "청동기",
            "청동기 대표 유물 랜드마크")
    };
}
