# Prehistory Race Map v2 — Final Art Specification

이 문서는 놀티쳐 뽑기 레이스 Map v2에서 아직 제작되지 않은 선사시대 역사 자산의 최종 아트 규격이다.

코드와 물리는 `PrehistoryRaceMapV2`가 담당하고, 이 문서는 실제 PNG 제작 기준을 고정한다. 없는 자산을 C# `Path`, `Polygon`, `Rectangle`, SVG 등으로 임시 대체하지 않는다.

## 1. 공통 제작 규격

- 마스터 파일: **1024 × 1024 RGBA PNG**
- 배경: 완전 투명
- 오브젝트 바깥 투명 여백: 각 방향 약 **8~12%**
- 사각 배경, 카드 프레임, 설명 문구, 배경색 박스 금지
- 그림 안에 런타임용 텍스트를 포함하지 않는다.
- 오브젝트 실루엣이 작은 화면에서도 빠르게 식별되어야 한다.
- 시점: 정면 완전 평면보다는 **아주 약한 3/4 상단 시점**을 기본으로 하되 자산 사이 시점을 통일한다.
- 스타일: 중립적인 만화/웹툰풍, 한국 초등 역사 삽화보다 약간 게임스럽고 세련된 수준
- 외곽선: 중간 굵기, 지나치게 검거나 두껍지 않게
- 명암: 약한 셀 셰이딩 중심
- 색: 흙·돌·뼈·청동의 자연색 중심, 과도한 파스텔·고채도·판타지 광택 금지
- 그림자: 사각형으로 구워진 배경 그림자 금지. 필요한 경우 오브젝트 바로 아래의 아주 약한 접지 그림자만 투명 알파로 사용한다.
- 실제 PNG의 투명 사각 bounds는 물리 판정으로 사용하지 않는다.
- WPF에서는 이미지를 `IsHitTestVisible=false`로 렌더링하고 실제 판정은 별도의 단순 collider가 담당한다.

현재 정적 Map v2 renderer는 source PNG가 커도 `DecodePixelWidth=512`로 디코딩한다. 따라서 1024px 마스터는 편집·재사용 여유를 확보하기 위한 제작 규격이며 런타임 메모리를 그대로 두 배 사용한다는 의미가 아니다.

## 2. 미완성 역사 자산 6종

### `prehistoric_bone_needle.png` — 뼈바늘

- 시대: 구석기
- 예약 위치: 좌측, Y≈870
- 표시 크기: 약 96 × 130
- 역할: `Landmark / None`
- 형태: 동물 뼈를 길고 가늘게 다듬은 바늘, 한쪽 끝에 실을 통과시키는 구멍이 분명히 보여야 함
- 피해야 할 표현: 현대 금속 바늘처럼 지나치게 매끈하거나 반짝이는 표현
- gameplay collider 없음

### `prehistoric_spindle_whorl.png` — 가락바퀴

- 시대: 신석기
- 예약 위치: 우측, Y≈1510
- 표시 크기: 약 100 × 100
- 역할: `Landmark / None`
- 형태: 돌 또는 토제로 만든 원반형 가락바퀴, 중앙 구멍이 명확해야 함
- 단순한 장신구·동전처럼 보이지 않도록 두께와 재질감을 약하게 표현
- gameplay collider 없음

### `prehistoric_shell_mask.png` — 조개 껍데기 가면

- 시대: 신석기
- 예약 위치: 우측, Y≈2130
- 표시 크기: 약 105 × 118
- 역할: `Landmark / None`
- 형태: 조개 껍데기를 가공한 얼굴형 유물로 인식 가능하게 구성
- 현대식 가면·판타지 마스크처럼 과장하지 않음
- gameplay collider 없음

### `prehistoric_half_moon_stone_knife.png` — 반달 돌칼

- 시대: 청동기
- 예약 위치: 우측, Y≈2470
- 표시 크기: 약 120 × 100
- 역할: 우선 `Landmark / None`
- 형태: 곡식 이삭을 자르는 데 사용한 반달형 석도라는 점이 드러나는 넓고 완만한 반달 실루엣
- 검·도끼처럼 날이 길게 돌출된 형태로 만들지 않음
- 초기 버전에서는 gameplay collider 없음

### `prehistoric_plain_pottery.png` — 민무늬 토기

- 시대: 청동기
- 예약 landmark 위치: 좌측, Y≈2700
- 표시 크기: 약 112 × 142
- 현재 landmark 역할: `Landmark / None`
- 향후 gameplay 역할: 별도 배치에서 `Breakable` 후보
- 형태: 무늬가 거의 없는 청동기 시대 토기. 빗살무늬 토기와 한눈에 구별되어야 함
- 빗살·기하학 패턴을 장식처럼 추가하지 않는다.
- 나중에 깨지는 장애물로도 사용할 수 있도록 실루엣 중심이 안정적이고 파편 연출에 어울리는 형태로 제작한다.
- **같은 PNG를 landmark로 사용하는 것과 gameplay breakable로 사용하는 것은 별개의 배치 계약**이다.

### `prehistoric_bronze_dagger.png` — 비파형 동검

- 시대: 청동기
- 예약 위치: 우측, Y≈2915
- 표시 크기: 약 120 × 155
- 역할: `Landmark / None`
- 형태: 비파형 동검 특유의 넓게 벌어진 날 몸통과 청동 재질이 명확해야 함
- 서양 판타지 단검, 일본도, 현대 칼처럼 보이지 않도록 함
- 청동 광택은 절제하고 녹청 표현은 필요할 경우 매우 약하게 사용
- 초기 버전에서는 gameplay collider 없음

## 3. 기존 재사용 자산과의 관계

현재 Map v2에서 재사용하는 주요 역사 PNG는 다음과 같다.

- `cartoon_handaxe.png` — 주먹도끼
- `cartoon_chipped_stone.png` — 찍개
- `cartoon_polished_stone.png` — 간석기
- `cartoon_comb_pottery.png` — 빗살무늬 토기
- `cartoon_dolmen.png` — 고인돌

일반 환경 요소로는 `river_stone.png`, `giant_root.png`, `fallen_log*.png`, `wood_branch*.png`, `cartoon_wood_stump.png` 등을 재사용한다.

기존 파일이 있다는 이유만으로 최종 아트에 반드시 사용하지 않는다. 실제 Windows 화면에서 화풍·시점·투명 가장자리·크기 감각을 확인한 뒤 최종 유지 여부를 결정한다.

## 4. Visual / Gameplay 계약

역사 유물의 종류만 보고 충돌 여부를 결정하지 않는다.

- 주먹도끼: 지도 가장자리 `Landmark / None` 가능, 별도 레이스 배치 `StaticBumper` 가능
- 찍개: `Landmark / None` 또는 `StaticBumper`
- 간석기: `Landmark / None` 또는 `StaticBumper`
- 빗살무늬 토기: `Landmark / None` 또는 `Breakable`
- 민무늬 토기: 초기 landmark는 `None`, 최종 PNG 검증 후 별도 `Breakable` 배치 가능
- 뼈바늘·조개 껍데기 가면 등은 억지로 장애물로 만들지 않는다.

PNG의 투명 영역까지 충돌시키지 않는다. 범퍼/토기 판정은 이미지보다 작은 Circle collider를 기본으로 하고, 구조물은 필요한 경우 몇 개의 단순 collider로 근사한다.

## 5. 최종 수용 조건

새 PNG를 저장소에 추가하기 전에 최소한 다음을 확인한다.

1. RGBA 투명 배경이며 사각 배경이 없음
2. 카드·프레임·텍스트가 그림에 포함되지 않음
3. 같은 세트 안에서 시점·외곽선·명암 스타일이 통일됨
4. 각 시대의 실제 유물과 혼동될 만한 형태 오류가 없음
5. Map v2 예약 위치에서 캐릭터와 겹쳐도 실루엣이 읽힘
6. gameplay 자산일 경우 visual 크기와 collider 크기가 독립적으로 조정 가능함
7. 저사양 학교 PC를 위해 불필요하게 초대형/다중 프레임 자산을 만들지 않음
8. Windows 실기기에서 최종 화면을 직접 확인하기 전에는 “시각 검증 완료”로 처리하지 않음
