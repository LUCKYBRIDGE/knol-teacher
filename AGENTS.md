# AGENTS.md: knol-teacher

선생님을 위한 교실 수업 및 학급 경영 지원 Windows 데스크톱 앱 (WPF).

## 스택과 구조
- 스택: Windows 10/11 x64, C# / .NET 8 / WPF
- 주요 파일 및 폴더
  - `KnolTeacher.sln`: 솔루션 파일
  - `src/KnolTeacher.Desktop/`: 메인 WPF 데스크톱 애플리케이션 프로젝트
  - `tests/KnolTeacher.Tests/`: 단위 및 회귀 테스트 프로젝트
  - `Directory.Build.props`: 버전 SSOT (`KnolTeacherVersion`)
  - `release/release-version.txt`: 실제 Stable Release 버전 기록
  - `dist-net/놀티쳐.exe`: 로컬 배포용 단일 실행 파일 산출물
  - `publish.bat`: 단일 파일 패키징 스크립트

## 명령어
실제로 실행되는 명령만 적는다. PowerShell 기준이다.

| 목적 | 명령 | 비고 |
|---|---|---|
| 빌드 검증 | `dotnet build KnolTeacher.sln -c Release` | 완료 보고 전 반드시 실행 |
| 테스트 | `dotnet test tests/KnolTeacher.Tests/KnolTeacher.Tests.csproj -c Release` | 완료 보고 전 반드시 실행 |
| 단일 파일 패키징 | `cmd /c publish.bat` | 주의: 실행 중인 놀티쳐 프로세스 강제 종료 부작용 포함 |

## 규칙
이 프로젝트에만 해당하는 규칙이다. 한국어 응답, 안전, 완료 기준 같은 전역 규칙은 상위 헌법(`C:\ai_dev\AGENTS.md`, `.harness/global/AGENTS.md`)을 따른다.

### 제품 기준
- 공식 저장소: `LUCKYBRIDGE/knol-teacher`
- 안정 기준선: v1.0.0
- 현재 기능 개발선: v1.1.0
- 플랫폼: Windows 10/11 x64, C# / .NET 8 / WPF
- 로컬 실행파일: `놀티쳐.exe`
- GitHub Release asset: `KnolTeacher.exe`

### 개인정보 (Local-Only)
- 학생·학급 식별 정보는 Local-Only가 기본이다.
- 핵심 기능은 학생 번호만으로 완전하게 사용할 수 있어야 한다.
- 이름·개인별 기록은 선택 기능이며 필요한 경우에만 로컬 저장한다.
- 학생 정보를 자동으로 클라우드, 서버, AI 서비스에 전송하지 않는다.

### 개발 및 코드 품질
- 초기 저장소 구성 이후 `main` 직접 수정 금지. branch → PR → CI → merge를 따른다.
- Build, 자동 테스트, single-file package 검증을 통과한 변경만 병합한다.
- 대규모 재작성보다 점진적 개선을 우선한다.
- 저장 형식 변경에는 기존 데이터 보존과 복구 경로를 둔다.
- 사용자 데이터 파일은 `SafeLocalJsonStore`/`SafeLocalFileStore`로만 저장한다. 다른 서비스가 메모리에 들고 있는 파일(예: 시간표)은 파일을 직접 쓰지 말고 그 서비스 API로 갱신한다.
- 공유 서비스의 전역 상태(예: `ISoundService.IsMuted`/`MasterVolume`)를 개별 창·위젯이 바꾸지 않는다. 도구별 음량은 `volumeScale` 인자로 전달하고, 자신이 재생한 소리만 멈춘다.
- 전역 단축키는 `App`에 처리기가 있는 동작(`DefaultHotkeys.SupportedActions`)만 등록하고, 단축키 안내 문구는 현재 설정에서 생성한다.
- 정적 필드 초기화로 클래스 핸들러를 등록하는 partial 클래스에는 명시적 static 생성자를 둔다(beforefieldinit 지연 초기화 방지).

### 버전과 Release
- `Directory.Build.props`의 `KnolTeacherVersion`이 현재 개발 소스 버전의 SSOT이다.
- `release/release-version.txt`는 실제 Stable Release로 승격된 버전만 기록한다.
- 공개 버전 이후 desktop 코드 변경 시 다음 개발 버전으로 올린다.
- 로컬 publish는 정확히 `놀티쳐.exe` 한 파일이어야 한다.
- GitHub Release에는 정확히 `KnolTeacher.exe` 한 asset만 공개한다.
- 업데이트는 이 저장소의 HTTPS Release, 크기, SHA-256, embedded FileVersion을 검증한다.

### 공개 안내와 자산
- 사용자 업데이트 안내는 교사가 실제로 체감하는 변화만 설명한다.
- 개발 과정과 내부 구현 용어는 사용자용 안내에 노출하지 않는다.
- 외부 제품·서비스를 기능 또는 화면 구성의 구현 출처나 유사성 근거로 제시하지 않는다.
- 프로젝트 시각 자산은 프로젝트를 위해 제작 또는 생성한 자산을 사용한다.
- 권리가 확인되지 않은 외부 이미지·음원·폰트를 저장소에 추가하지 않는다.
- 제3자 라이브러리는 해당 라이선스를 존중한다.

## 완료 기준
1. 요청한 동작이 구현되었다.
2. `dotnet build KnolTeacher.sln -c Release` 빌드가 경고/오류 없이 통과한다.
3. `dotnet test tests/KnolTeacher.Tests/KnolTeacher.Tests.csproj -c Release` 단위 및 회귀 테스트를 모두 통과한다.
4. UI 변경 시 화면 렌더링 또는 동작을 확인했거나, 확인하지 못했음을 명시한다.
5. 설정, 규칙, 가이드 등 지속 보존해야 할 정보가 변경된 경우 관련 문서를 갱신한다.

## 참고
- 상위 작업실 헌법: `C:\ai_dev\AGENTS.md`
- UI/UX 디자인 가이드: `docs/UI_UX_DESIGN_GUIDELINES.md`
- 개발 마스터 플랜: `docs/DEVELOPMENT_MASTER_PLAN.md`
- 프로젝트 컨텍스트: `docs/PROJECT_CONTEXT.md`
- 릴리스 런북: `docs/V1_1_RC_RUNBOOK.md`
- 릴리스 회귀 체크리스트: `docs/V1_1_RELEASE_REGRESSION_CHECKLIST.md`
- 인수인계 메모: `docs/tasks/current.md` (있으면 먼저 읽는다)
