# 계획: 선별적 동시 터치, 무간섭 내장 브라우저 및 PPT 소리 끊김 방지

- **작성 일시**: 2026-10-05
- **목표**: 
  1. 전자칠판(모니터 2)에서 학생들의 핵심 조작(동시 판서, 과제 체크박스)에 대해 선별적 멀티터치 지원.
  2. 학생이 전자칠판을 터치해도 교탁 PC(모니터 1)의 마우스 커서가 납치되지 않고 나이스/문서 키보드 포커스를 뺏기지 않는 비간섭(No-Activate & Mouse Isolation) 보호막 구축.
  3. 크롬 100% 호환 및 넷플릭스 등 DRM 재생을 지원하는 "무간섭 스마트 내장 웹 브라우저" 서비스 구현.
  4. 모니터 1에서 다른 작업을 해도 모니터 2의 파워포인트 소리·영상이 멈추지 않는 PPT 재생 솔루션 구현.
- **완료 기준**:
  1. `MultiTouchInkHelper`가 `StudentDisplayWindow` 판서에 연결되어 2명 이상 동시 필기 가능.
  2. 과제 체크박스에 `PreviewTouchDown` 즉시 반응 연결로 동시 터치 체크 가능.
  3. `WS_EX_NOACTIVATE` 및 터치 격리 적용으로 전자칠판 조작 시 교탁 PC 마우스/포커스 보존.
  4. `ClassroomWebBrowserWindow` (WebView2 기반, PlayReady DRM, User-Agent, 로그인 유지) 구현.
  5. PPT 창 모드 실행 자동화 런처(`PowerPointPresentationHelper`) 구현.
  6. `dotnet build KnolTeacher.sln -c Release` 경고 0, 오류 0 통과.
  7. `dotnet test tests/KnolTeacher.Tests/KnolTeacher.Tests.csproj -c Release` 전체 통과.
  8. `cmd /c publish.bat` 올인원 단일 실행 파일 (`dist-net/놀티쳐.exe`) 패키징 성공.
- **하지 않는 것**:
  - 모든 콤보박스나 설정 다이얼로그에 무분별한 멀티터치를 붙이지 않음 (핵심 영역만 선별 적용).
  - 외부 상용 브라우저나 파워포인트 원본 바이너리를 강제 변조하지 않음.

---

## 영향 범위
- **새 파일**:
  - `src/KnolTeacher.Desktop/Views/Windows/ClassroomWebBrowserWindow.xaml`: 전자칠판 전용 무간섭 스마트 웹 브라우저 뷰.
  - `src/KnolTeacher.Desktop/Views/Windows/ClassroomWebBrowserWindow.xaml.cs`: WebView2 초기화 (PlayReady DRM, User-Agent, 백그라운드 미디어 지속, 마우스 납치 방지).
  - `src/KnolTeacher.Desktop/Services/PowerPointPresentationHelper.cs`: 파워포인트 창 모드(`ppShowTypeWindow`) 자동화 및 모니터 2 영역 맞춤 런처.
  - `tests/KnolTeacher.Tests/PowerPointPresentationHelperTests.cs`: PPT 런처 안전 가드 단위 테스트.
- **바꿀 파일**:
  - `src/KnolTeacher.Desktop/Views/Windows/StudentDisplayWindow.xaml`: 
    - 툴바에 [🌐 웹 브라우저], [📊 무간섭 PPT] 퀵 버튼 추가.
    - 대시보드 과제 체크박스에 `PreviewTouchDown` 핸들러 추가.
  - `src/KnolTeacher.Desktop/Views/Windows/StudentDisplayWindow.xaml.cs`: 
    - `MultiTouchInkHelper` 판서 연결 (동시 필기 활성화).
    - 창 생성 시 `WS_EX_NOACTIVATE` 및 스타일러스 딜레이 해제 적용.
    - 과제 체크박스 터치 즉각 반응 핸들러 추가.
  - `src/KnolTeacher.Desktop/MainWindow.xaml`:
    - 상단 액션 도크 또는 메뉴에 [🌐 전자칠판 웹], [📊 무간섭 PPT] 버튼 추가.
  - `src/KnolTeacher.Desktop/MainWindow.xaml.cs`:
    - 브라우저 및 PPT 런처 실행 핸들러 연결.
- **위험 및 대응**:
  - WebView2 런타임 미설치 PC: Windows 10/11은 기본 탑재되어 있으나, 미설치 시 기본 시스템 브라우저로 안전하게 폴백(Fallback) 안내.
  - 파워포인트 미설치 PC: Office 미설치 시 에러 없이 "웹 기반 파워포인트 또는 PDF로 열기" 안내 토스트 표시.

---

## 단계별 구현 계획

### 1단계: 놀보드 선별적 멀티터치 및 교탁 PC 보호막 구축
1. [x] `StudentDisplayWindow.xaml.cs`에 `MultiTouchInkHelper` 연결 (동시 판서 획 수집 + Undo 연동).
2. [x] `StudentDisplayWindow.xaml`의 대시보드 과제 체크박스에 `PreviewTouchDown` 이벤트 연결 및 즉시 토글 핸들러 구현.
3. [x] `StudentDisplayWindow`에 `WS_EX_NOACTIVATE` 확장 창 스타일 적용 (모니터 1 키보드 포커스 뺏김 방지) 및 터치 격리 (`e.Handled = true`).

### 2단계: 무간섭 스마트 내장 웹 브라우저 (`ClassroomWebBrowserWindow`) 구현
1. [x] `ClassroomWebBrowserWindow.xaml` 작성 (터치 친화적 네비게이션: 뒤로/앞으로/새로고침/홈/URL입력/확대축소/전체화면/북마크 바).
2. [x] `ClassroomWebBrowserWindow.xaml.cs` 작성:
   - `CoreWebView2EnvironmentOptions`: `--enable-features=msPlayReady`, `--disable-background-media-suspend`, `--autoplay-policy=no-user-gesture-required`.
   - User-Agent 정식 Microsoft Edge 문자열 설정.
   - `userDataFolder`: `_configService.ConfigDir/browser_profile` (구글/네이버 로그인 유지).
   - `WS_EX_NOACTIVATE` 스타일 적용 (교탁 PC 마우스 납치 및 키보드 포커스 스틸 원천 차단).
   - 새 창(`NewWindowRequested`) 및 다운로드(`DownloadStarting`) 처리.

### 3단계: 파워포인트(PPT) 무간섭 재생 런처 (`PowerPointPresentationHelper`) 구현
1. [x] `PowerPointPresentationHelper.cs` 작성:
   - 로컬 파워포인트 COM 리플렉션(`Type.GetTypeFromProgID("PowerPoint.Application")`).
   - 슬라이드 쇼 설정을 `ShowType = 2 (ppShowTypeWindow, 웹 형식 창 모드)`로 실행하고 모니터 2 작업 영역으로 자동 배치.
   - 파워포인트 미설치 시 안전 예외 방어 및 안내.
2. [x] `MainWindow` 및 `StudentDisplayWindow`에 PPT 파일 열기 다이얼로그 및 원클릭 런처 연동.

### 4단계: 테스트 및 빌드 검증
1. [x] 신규 기능 단위 테스트 작성 및 기존 134개 + 신규 8개 = 총 142개 테스트 회귀 검증 100% 통과.
2. [x] `dotnet build KnolTeacher.sln -c Release` 빌드 경고 0, 오류 0 통과.
3. [x] `cmd /c publish.bat` 단일 실행 파일 (`dist-net/놀티쳐.exe`) 패키징 통과.
