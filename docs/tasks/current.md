# 현재 작업 메모 (knol-teacher)

- **작업 일시**: 2026-10-05
- **기준 브랜치**: `main` (origin/main 최신 동기화 완료: `3fa9af3`)
- **버전**: v1.1.0 (개발선) / v1.0.0 (안정선)

---

## 1. 진행 및 완료 내역

### 1) 화면 레이아웃 전면 개편 (좌측 사이드바 + 우측 주 화면)
- **레이아웃 구조**:
  - `MainWindow.xaml` 최상위 그리드를 `ColSidebar(230px)` + `RightMainWorkspace(*)`의 2열 구조로 개편.
  - 사용자가 선택한 **Clean Studio 스타일(모던 슬레이트 & 차분한 네이비/블루 포인트)** 적용.
- **좌측 사이드바 (`LeftSidebar`)**:
  - 브랜드 로고 및 교실 스튜디오 타이틀.
  - 6대 메인 탭 네비게이션 버튼 (`NavBtnToday`, `NavBtnTools`, `NavBtnSchedule`, `NavBtnZen`, `NavBtnSites`, `NavBtnNeis`).
  - 핑키네(Pinky-NE) 교실자료실 바로가기 배너.
  - 3종 실시간 테마 선택기 (스튜디오 / 다크 / 베이지).
  - 버전 및 제작사 정보.
- **우측 주 화면**:
  - **상단 액션 바**: 실시간 교시 상태 캡슐, 듀얼 모니터 감지 캡슐, 5대 핵심 도구 퀵 실행 화이트 액션 도크 (놀보드, 화면판서, 칠판보드, 타이머, 뽑기), 유틸리티 버튼군 (대시보드 복귀, 양식 공유, 튜토리얼, 교시 알람, 위젯 편집, 사이드바 접기/펼치기 토글 `BtnToggleDrawer`).
  - **중앙 작업 영역**: `MainTabs` TabControl (기존 6개 탭 뷰 100% 무손실 보존).
  - **양방향 선택 동기화**: `UpdateNavigationSelection(int index)`를 통해 탭 전환 시 사이드바 활성 스타일 및 타이틀/대시보드 복귀 버튼 상태 완벽 연동.

### 2) 기능 결함 및 동작 오류 개선
- **바탕화면 정리 히스토리 영속화 (`DesktopCleanerService.cs`)**:
  - 기존 메모리 변수(`_lastOrganizedRecords`)를 `SafeLocalJsonStore` 기반 `desktop_organize_history.json` 파일 저장으로 개선.
  - 앱을 껐다 켜더라도 직전 정리를 안전하게 실행 취소(Undo)할 수 있도록 복원 지원.
  - Explorer 새로고침 및 UI 문구 표준화.
- **학생 명렬표 실시간 동기화 (`IStudentManagerService`, `StudentManagerService.cs`)**:
  - `event Action? RosterChanged;` 신설.
  - 명렬표 저장, 재설정, 아바타 변경 시 이벤트 트리거.
  - `SmartSeatShuffleWindow`, `StudentPickerWindow`, `PickerWidgetView`에서 구독하여 명렬표 수정 즉시 열린 창에 자동 반영.
- **단일 모니터 우클릭 안전 가드 (`MainWindow.PopupUx.cs`)**:
  - 단일 모니터 환경에서 우클릭 팝업 실행 시 친절한 토스트 안내(`단일 모니터 환경: 기본 화면에 창을 엽니다`) 표시.

### 3) 단위 및 회귀 테스트 확충
- `StudentManagerServiceTests.cs` (3개 테스트: 명렬표 번호 재설정, 저장, 아바타 갱신 이벤트 트리거 검증).
- `DesktopCleanerServiceTests.cs` (2개 테스트: 비어있는 히스토리 안전 반환, 손상된 JSON 안전 복구 검증).
- **테스트 결과**: 128개 전체 통과 (0 실패).
- **빌드 결과**: `dotnet build KnolTeacher.sln -c Release` 경고 0, 오류 0 통과.

---

## 2. 형상 관리 상태
- 작업 트리 변경사항:
  - `AGENTS.md`, `CLAUDE.md`, `docs/`
  - `src/KnolTeacher.Desktop/MainWindow.xaml`, `MainWindow.xaml.cs`, `MainWindow.PopupUx.cs`
  - `src/KnolTeacher.Desktop/Services/DesktopCleanerService.cs`, `StudentManagerService.cs`
  - `src/KnolTeacher.Desktop/Views/Controls/Widgets/PickerWidgetView.xaml.cs`
  - `src/KnolTeacher.Desktop/Views/Windows/SmartSeatShuffleWindow.xaml.cs`, `StudentPickerWindow.xaml.cs`
  - `tests/KnolTeacher.Tests/StudentManagerServiceTests.cs`, `DesktopCleanerServiceTests.cs`
- `main` 브랜치 직접 커밋/푸시는 하네스 규칙에 따라 진행하지 않았으며, 사용자 요청 시 브랜치 생성 및 PR/커밋을 진행합니다.
