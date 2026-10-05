# 현재 작업 메모 (knol-teacher)

- **작업 일시**: 2026-10-05
- **기준 브랜치**: `feat/student-dashboard-and-classroom-tasks` (기반 커밋: `8e5298f`)
- **버전**: v1.1.0 (개발선) / v1.0.0 (안정선)

---

## 1. 진행 및 완료 내역

### 1) 학급 과제 및 제출/할 일 시스템 전면 구축 (`IClassroomTaskService`, `ClassroomTaskService`)
- **데이터 모델 (`ClassroomTaskItem.cs`)**:
  - `Id`, `Text`, `Category` ("Today" | "Weekly"), `Tag` (과제, 준비물, 제출, 안내), `DueDateText` (마감 힌트), `IsCompleted`, `CreatedAt`.
  - `INotifyPropertyChanged`를 통한 UI 실시간 바인딩.
- **전담 서비스 (`ClassroomTaskService.cs`)**:
  - `SafeLocalJsonStore` 기반의 원자적 저장 (`classroom_tasks.json`).
  - 오늘 제출/할 일(`GetTodayTasks()`), 이번 주 제출/할 일(`GetWeeklyTasks()`) 분리 조회 및 추가/수정/삭제/완료 토글.
  - 완료 항목 일괄 정리 (`ClearCompletedTasks(category)`).
  - 실시간 이벤트 `event Action? TasksChanged;`로 메인화면과 학생 화면(놀보드 대시보드) 간 완벽한 양방향 동기화.
- **의존성 주입 (`App.xaml.cs`)**:
  - `IClassroomTaskService` 싱글톤 등록.

### 2) 교사용 메인화면 UI/UX 개편 (`MainWindow.xaml`, `MainWindow.xaml.cs`)
- **4단 세그먼트 전환**:
  - `[📌 오늘 제출·할일]` (학생 대시보드 실시간 연동)
  - `[🗓️ 이번주 제출·할일]` (학생 대시보드 실시간 연동)
  - `[📢 학급 알림장]` (기존 줄글 알림장 호환)
  - `[🔒 교사 메모]` (선생님 개인 비공개 체크리스트)
- **과제 리스트 및 퀵 추가 UI**:
  - 태그 배지(`[과제]`, `[준비물]`, `[제출]`, `[안내]`), 마감 안내 뱃지, 완료 취소선, 원클릭 완료 토글 체크박스, 삭제 버튼.
  - 하단 빠른 추가 바: 태그 선택 콤보 + 과제 텍스트 + 마감 힌트 + [추가] (Enter 키 지원).
  - 옵션 메뉴(`···`): 완료 과제 일괄 정리, 기본 교실 예시 과제 복원, 놀보드 대시보드 바로 열기(F2).

### 3) 놀보드 학생 화면 "스마트 학급 대시보드" 모드 구현 (`StudentDisplayWindow.xaml`, `StudentDisplayWindow.xaml.cs`)
- **상단 툴바 모드 전환 세그먼트**:
  - `[🏫 학급 대시보드]` (기본 모드) ↔ `[🧩 자유 위젯]` (기존 캔버스 모드)
- **스마트 학급 대시보드 레이아웃 (Clean Dark Slate Studio Theme)**:
  - **헤더 바**: 🏫 "우리 반 스마트 교실" 배지 + 실시간 교시 상태 캡슐 (`🔔 2교시 수학 수업 중` / `☕ 쉬는 시간`) + 날짜/요일 + 대형 실시간 디지털 시계 (`HH:mm:ss`).
  - **1열 (오늘의 시간표)**:
    - 1~6교시 과목 목록 및 시간대 표시.
    - 현재 교시 선명한 아쿠아 블루 테두리 & `[수업 중]` 배지 하이라이트.
    - 요일 변경 시 자동 갱신 및 [시간표 편집] 퀵 링크.
  - **2열 (제출 & 할 일 집중 존 - 메인 영역)**:
    - 📌 **오늘 제출 & 할 일**: 큼직하고 시원한 텍스트(15pt), 태그 배지, 마감 배지, 대형 터치 체크박스, 완료 통계 뱃지.
    - 🗓️ **이번 주 제출 & 할 일**: 이번 주 챙겨야 할 장기 과제/준비물, 마감 요일 뱃지.
    - 교사용 메인화면과 실시간 양방향 동기화.
  - **3열 (오늘의 급식 & D-Day / 알림장)**:
    - 🍱 **오늘의 맛있는 급식**: 비동기 나이스 식단표 연동, 열량(kcal) 뱃지, [이전/오늘/다음] 날짜 탐색.
    - 🎯 **D-Day & 알림장**: 학급 목표 D-Day 카운트다운 캡슐 + 메인화면과 연동되는 실시간 알림장 메모 요약.
- **전자칠판 펜 필기(판서)와의 완전한 호환**:
  - 대시보드 뷰 위에서도 [✏️ 보드 필기] 활성화 시 `BoardInkCanvas`가 대시보드 위에 투명 오버레이되어 바로 판서/첨삭 가능.

### 4) 선별적 멀티터치 및 교탁 PC 보호막 구축 (`StudentDisplayWindow`)
- **선별적 멀티터치 판서 (`MultiTouchInkHelper`)**:
  - `MultiTouchInkHelper`를 `StudentDisplayWindow`의 판서 엔진으로 연동하여 2명 이상의 학생이 전자칠판에서 독립된 스트로크로 동시 필기 가능.
  - 판서한 각 획은 `DrawingUndoManager`에 정상 등록되어 되돌리기(Undo) 완벽 지원.
  - 지우개 도구(부분/획/구역/영역) 선택 시 정밀 지우개 엔진과 원활하게 전환.
- **과제 체크박스 즉시 터치 (`PreviewTouchDown`)**:
  - 학생 화면 대시보드의 오늘/이번 주 과제 체크박스에 `PreviewTouchDown` 핸들러를 적용하고 `e.Handled = true`로 처리하여, 동시 터치 체크 가능 및 터치가 마우스 클릭으로 승격되어 모니터 1의 마우스 커서가 칠판으로 순간이동(납치)하는 현상 원천 차단.
- **교탁 PC(모니터 1) 마우스/키보드 보호막 (`WS_EX_NOACTIVATE` & `WM_MOUSEACTIVATE`)**:
  - 전자칠판(모니터 2) 윈도우에 `WS_EX_NOACTIVATE` 확장 창 스타일 및 `WM_MOUSEACTIVATE -> MA_NOACTIVATE` 윈도우 프로시저 훅 적용.
  - 스타일러스/터치 프레스 앤 홀드, 플릭스, 탭 피드백 지연을 완전히 비활성화.
  - 학생이 전자칠판을 터치해도 교탁 PC에서 선생님이 나이스(NEIS) 입력이나 한글 문서 타이핑 중이던 키보드 포커스와 마우스 위치가 100% 안전하게 유지됨.

### 5) 크롬 호환 무간섭 스마트 내장 웹 브라우저 (`ClassroomWebBrowserWindow`)
- **Microsoft WebView2 기반 전용 브라우저 창 구현**:
  - **PlayReady DRM 지원**: `--enable-features=msPlayReady,MediaFoundationD3D11VideoCapture` 인수로 넷플릭스 등 DRM 콘텐츠 재생 지원.
  - **백그라운드 미디어 지속 재생**: `--disable-background-media-suspend` 및 `--autoplay-policy=no-user-gesture-required`로 교탁 PC에서 딴짓을 하거나 포커스를 잃어도 유튜브, 음악, 영상이 일시정지되지 않고 끊김 없이 재생.
  - **크롬/엣지 웹 표준 호환성**: 최신 Microsoft Edge User-Agent 적용으로 구글 클래스룸, 유튜브, 띵커벨, 패들렛 등에서 완벽한 웹 브라우징 환경 보장.
  - **영속적 프로필 디렉터리**: `browser_profile/` 디렉터리에 쿠키/로그인 세션을 안전하게 저장하여 구글/네이버 자동 로그인 유지.
  - **터치 친화적 UI**: 뒤로/앞으로/새로고침/홈/URL 주소창(검색 연동)/배율 조절/모니터 1↔2 이동/전체화면 + 유튜브, 구글 클래스룸, 띵커벨, 패들렛, 넷플릭스 등 주요 교육/미디어 원클릭 빠른 링크 바 제공.
  - **교탁 마우스 보호 모드 (`🛡️ 교탁 보호`)**: 브라우저 창 자체에도 `WS_EX_NOACTIVATE` 토글을 탑재하여 전자칠판에서 웹을 조작해도 교탁 PC 방해 차단.
  - 메인 화면 및 놀보드 상단 툴바에 원클릭 실행 버튼 (`[🌐 스마트 웹]`) 연동.

### 6) 파워포인트(PPT) 소리/영상 끊김 없는 무간섭 슬라이드 쇼 런처 (`PowerPointPresentationHelper`)
- **배경음악 및 동영상 멈춤 현상 원인 규명 및 해결**:
  - 파워포인트의 기본 전체화면 쇼(`ppShowTypeSpeaker`)는 포커스 상실 시 OS로부터 미디어를 강제 Pause 시킴.
  - 이를 `ppShowTypeWindow` (창 모드 슬라이드 쇼, `ShowType = 2`)로 실행하고 모니터 2 작업 영역에 꽉 차게 정렬하여, 교탁 모니터 1에서 다른 작업을 하더라도 파워포인트의 소리와 동영상이 절대 끊기지 않고 부드럽게 계속 재생되도록 구현.
- **경량 표준 COM 리플렉션**: 외부 거대 Office Interop 라이브러리 추가 없이 .NET 표준 COM 리플렉션으로 구현하여 단일 실행 파일 배포 무결성 보존.
- **안전한 실행 방어**: 파워포인트 미설치 시 기본 연결 프로그램으로 안전하게 실행 폴백.
- 메인 화면 및 놀보드 상단 툴바에 원클릭 실행 버튼 (`[📊 무간섭 PPT]`) 연동.

### 7) 품질 검증 및 단일 파일 배포 패키징
- **단위 테스트**: 
  - `ClassroomTaskServiceTests.cs` (6개)
  - `MultiTouchInkHelperTests.cs` (4개 - `IsEnabled` 및 터치 격리)
  - `PowerPointPresentationHelperTests.cs` (5개 - 입력 검증 및 창 모드 상수 검증)
  - 전체 단위/회귀 테스트: **총 142개 전체 통과 (0 실패)**.
- **빌드 검증**: `dotnet build KnolTeacher.sln -c Release` **경고 0개, 오류 0개** 통과.
- **단일 파일 배포 패키징**: `publish.bat` 실행 완료 -> `dist-net/놀티쳐.exe` (SHA-256: `02a93c2ee5ae9435c099a3c9ceac7b2c5ba9ffbbddccdb7c441acec5759dff56`) 단일 실행 파일 산출물 정상 생성.

---

## 2. 다음 작업 권장 사항
1. 브랜치 커밋 및 원격 저장소 PR 생성 (`feat/student-dashboard-and-classroom-tasks`).
2. 실제 교실 전자칠판 환경에서의 듀얼 모니터 멀티터치 감도 및 마우스 격리 동작 현장 테스트.
