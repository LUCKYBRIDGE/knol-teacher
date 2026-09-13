using System.Collections.Generic;

namespace KnolTeacher.Desktop.Models;

public sealed record PrehistoryRaceZone(
    string Key,
    string Title,
    string Period,
    string Subtitle,
    double StartY,
    double EndY,
    string BackgroundHex,
    string AccentHex);

public sealed record PrehistoryArtifactLandmark(
    string Key,
    string Name,
    string Period,
    string Caption,
    double Y,
    bool AlignRight);

public static class PrehistoryRaceThemeSpec
{
    public const double TrackHeight = 3500.0;

    public static IReadOnlyList<PrehistoryRaceZone> Zones { get; } = new[]
    {
        new PrehistoryRaceZone(
            "paleolithic-field",
            "구석기 들판",
            "구석기",
            "찍고 깨뜨려 만든 석기로 사냥과 채집을 하던 시기",
            0,
            700,
            "#35523F",
            "#E3B96D"),
        new PrehistoryRaceZone(
            "paleolithic-cave",
            "동굴과 바위 협곡",
            "구석기",
            "동굴 생활터와 뼈바늘을 지나며 이동 생활을 살펴봐요",
            700,
            1400,
            "#4B3A32",
            "#E8C89B"),
        new PrehistoryRaceZone(
            "river-transition",
            "강가 이동길",
            "시대 전환",
            "물가의 풍부한 자원을 따라 정착 생활로 이어지는 길",
            1400,
            2100,
            "#31514D",
            "#7DD3C7"),
        new PrehistoryRaceZone(
            "neolithic-village",
            "신석기 마을",
            "신석기",
            "간석기와 가락바퀴, 움집이 보이는 정착 생활 공간",
            2100,
            2800,
            "#51593B",
            "#E6B86B"),
        new PrehistoryRaceZone(
            "neolithic-coast",
            "해안 의례터",
            "신석기",
            "조개 껍데기 가면과 바닷가 생활 문화를 만나는 결승 구간",
            2800,
            3500,
            "#315563",
            "#8ED6DB")
    };

    public static IReadOnlyList<PrehistoryArtifactLandmark> Artifacts { get; } = new[]
    {
        new PrehistoryArtifactLandmark(
            "chopper",
            "찍개",
            "구석기",
            "돌의 한쪽 면을 깨뜨려 날을 만든 도구",
            330,
            false),
        new PrehistoryArtifactLandmark(
            "handaxe",
            "주먹도끼",
            "구석기",
            "손에 쥐고 여러 용도로 사용한 대표적인 뗀석기",
            560,
            true),
        new PrehistoryArtifactLandmark(
            "bone-needle",
            "뼈바늘",
            "구석기",
            "동물의 뼈를 다듬어 옷이나 가죽을 꿰는 데 사용",
            1080,
            false),
        new PrehistoryArtifactLandmark(
            "polished-stone",
            "간석기",
            "신석기",
            "돌의 표면을 갈고 다듬어 만든 석기",
            2260,
            true),
        new PrehistoryArtifactLandmark(
            "spindle-whorl",
            "가락바퀴",
            "신석기",
            "실을 뽑고 옷감을 만드는 생활 모습을 보여 주는 도구",
            2570,
            false),
        new PrehistoryArtifactLandmark(
            "shell-mask",
            "조개 껍데기 가면",
            "신석기",
            "바닷가 생활과 신석기인의 문화·의례를 보여 주는 유물",
            3060,
            true)
    };
}
