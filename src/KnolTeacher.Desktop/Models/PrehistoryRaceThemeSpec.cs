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
            "찍개와 주먹도끼로 사냥과 채집을 하며 이동하던 시기",
            0,
            600,
            "#35523F",
            "#E3B96D"),
        new PrehistoryRaceZone(
            "paleolithic-cave",
            "동굴과 바위 협곡",
            "구석기",
            "동굴 생활터와 뼈바늘을 지나며 구석기 생활을 살펴봐요",
            600,
            1200,
            "#4B3A32",
            "#E8C89B"),
        new PrehistoryRaceZone(
            "neolithic-river",
            "강가 정착지",
            "신석기",
            "강과 바닷가 주변에 정착하고 간석기를 사용하던 생활 공간",
            1200,
            1800,
            "#31514D",
            "#7DD3C7"),
        new PrehistoryRaceZone(
            "neolithic-village",
            "신석기 마을과 바닷가",
            "신석기",
            "가락바퀴와 조개 껍데기 가면, 움집이 보이는 정착 생활 공간",
            1800,
            2400,
            "#51593B",
            "#E6B86B"),
        new PrehistoryRaceZone(
            "bronze-farming-village",
            "청동기 농경 마을",
            "청동기",
            "벼농사가 발달하고 반달 돌칼과 민무늬 토기를 사용하던 마을",
            2400,
            2950,
            "#574735",
            "#D59A56"),
        new PrehistoryRaceZone(
            "bronze-dolmen-hill",
            "고인돌 언덕",
            "청동기",
            "비파형 동검과 고인돌을 통해 청동기 사회의 변화를 살펴봐요",
            2950,
            3500,
            "#403C45",
            "#C7A56A")
    };

    public static IReadOnlyList<PrehistoryArtifactLandmark> Artifacts { get; } = new[]
    {
        new PrehistoryArtifactLandmark(
            "chopper",
            "찍개",
            "구석기",
            "돌의 한쪽 면을 깨뜨려 날을 만든 도구",
            270,
            false),
        new PrehistoryArtifactLandmark(
            "handaxe",
            "주먹도끼",
            "구석기",
            "손에 쥐고 여러 용도로 사용한 대표적인 뗀석기",
            485,
            true),
        new PrehistoryArtifactLandmark(
            "bone-needle",
            "뼈바늘",
            "구석기",
            "동물의 뼈를 다듬어 옷이나 가죽을 꿰는 데 사용",
            890,
            false),
        new PrehistoryArtifactLandmark(
            "polished-stone",
            "간석기",
            "신석기",
            "돌의 표면을 갈고 다듬어 만든 석기",
            1360,
            true),
        new PrehistoryArtifactLandmark(
            "spindle-whorl",
            "가락바퀴",
            "신석기",
            "실을 뽑고 옷감을 만드는 생활 모습을 보여 주는 도구",
            1680,
            false),
        new PrehistoryArtifactLandmark(
            "shell-mask",
            "조개 껍데기 가면",
            "신석기",
            "바닷가 생활과 신석기인의 문화·의례를 보여 주는 유물",
            2140,
            true),
        new PrehistoryArtifactLandmark(
            "half-moon-stone-knife",
            "반달 돌칼",
            "청동기",
            "곡식의 이삭을 거두는 데 사용한 대표적인 농경 도구",
            2490,
            false),
        new PrehistoryArtifactLandmark(
            "plain-pottery",
            "민무늬 토기",
            "청동기",
            "무늬가 거의 없는 청동기 시대의 대표적인 토기",
            2710,
            true),
        new PrehistoryArtifactLandmark(
            "bronze-dagger",
            "비파형 동검",
            "청동기",
            "비파 모양의 날을 가진 청동기 시대의 대표적인 청동검",
            3050,
            false),
        new PrehistoryArtifactLandmark(
            "dolmen",
            "고인돌",
            "청동기",
            "큰 돌을 이용해 만든 무덤으로 당시 사회 모습을 보여 줌",
            3290,
            true)
    };
}
