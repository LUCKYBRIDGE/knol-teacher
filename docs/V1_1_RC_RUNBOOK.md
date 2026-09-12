# 놀티쳐 v1.1.0 Release Candidate Runbook

이 문서는 v1.1.0을 Stable Release로 승격하기 전, 실제 Windows 환경에서 검증할 Release Candidate(RC)를 만드는 절차와 판정 기준을 정의한다.

RC는 Stable Release가 아니다. RC 생성만으로 `release/release-version.txt`를 변경하거나 GitHub Release를 게시하지 않는다.

## 1. RC 생성

GitHub Actions에서 `KnolTeacher v1.1 Release Candidate` 워크플로를 `main` 브랜치에서 수동 실행한다.

워크플로는 다음을 다시 검증한다.

1. 실행 ref가 `main`인지 확인
2. `Directory.Build.props`의 개발 버전이 현재 Stable Release보다 높은지 확인
3. restore
4. Release build with warnings as errors
5. 전체 자동 테스트
6. single-file 압축 정책
7. 실제 `publish.bat` 실행
8. `놀티쳐.exe` 단일 산출물 확인
9. embedded FileVersion / ProductVersion 확인
10. SHA-256 및 파일 크기 기록
11. RC artifact 업로드

RC artifact에는 다음 두 파일만 포함한다.

- `놀티쳐.exe`
- `RC_MANIFEST.txt`

`RC_MANIFEST.txt`에는 개발 버전, Stable 기준 버전, commit SHA, workflow run, 파일 크기, SHA-256이 기록된다.

## 2. 수동 회귀 검증

RC artifact의 `놀티쳐.exe`를 다운로드하여 `docs/V1_1_RELEASE_REGRESSION_CHECKLIST.md` 전체를 수행한다.

최소한 다음 환경 특성을 기록한다.

- 테스트 날짜
- 테스트한 commit SHA
- Windows 버전 및 x64 여부
- 디스플레이 개수
- 각 디스플레이의 배율(DPI scaling)
- 마우스/펜/터치 사용 여부
- 멀티터치 가능 여부
- 테스트한 RC의 SHA-256

자동 테스트가 PASS여도 다음은 실제 Windows 환경 검증 없이는 PASS로 간주하지 않는다.

- 스플래시 및 체감 시작 속도
- single-instance 기존 창 앞으로 가져오기
- 듀얼 모니터 창 배치/재호출
- 모니터 연결/해제 후 창 위치
- 멀티터치 동시 판서
- 화면 판서 투명도/ESC 종료
- 자/삼각자/각도기 이동과 회전
- 실물화상기 등 장치 의존 기능
- 여러 지연 로딩 도구의 첫 호출/재호출

## 3. RC 판정

다음 조건을 모두 만족해야 Release 승격 PR을 만들 수 있다.

1. RC workflow 전체 성공
2. `docs/V1_1_RELEASE_REGRESSION_CHECKLIST.md` 필수 항목 전체 PASS
3. 치명적/높음 수준 회귀 버그 0건
4. 테스트에 사용한 EXE의 SHA-256과 `RC_MANIFEST.txt` 일치
5. 실제 테스트한 commit과 Release 승격 대상 commit이 일치하거나, 이후 변경이 문서/릴리스 메타데이터에만 한정됨

기능 코드가 RC 검증 뒤 변경되면 새 RC를 다시 만든다.

## 4. Stable Release 승격 PR

수동 검증이 완료된 뒤 별도 branch/PR에서 다음을 수행한다.

- `release/release-version.txt`를 `1.1.0`으로 변경
- `release/user-facing-notes.md`를 v1.1.0 교사용 변경 내용으로 확정
- Release 대상 commit이 검증된 RC와 일치하는지 확인
- PR CI 전체 PASS 확인

그 뒤 GitHub Stable Release `v1.1.0`을 만들고 asset은 정확히 `KnolTeacher.exe` 한 파일만 게시한다.

RC artifact ZIP이나 `RC_MANIFEST.txt`는 Stable Release asset으로 게시하지 않는다.

## 5. main 보호 설정

현재 repository-level 기술 규칙은 branch → PR → CI → merge를 요구한다. 이 규칙을 GitHub 설정에서도 강제하기 위해 Stable Release 전에 `main` branch protection 또는 ruleset을 활성화한다.

권장 최소 설정은 다음과 같다.

- pull request를 거쳐야 merge 가능
- `KnolTeacher CI / Build, test, and verify package` 성공 필수
- merge 전에 branch가 최신 `main`과 동기화되어 있어야 함
- 강제 push 금지
- branch 삭제 금지

GitHub 관리 설정은 저장소 파일이 아니므로 이 문서/CI만으로 직접 강제할 수 없다. 보호 설정이 활성화되기 전까지는 `AGENTS.md`의 branch → PR → CI → merge 규칙을 운영상 계속 준수한다.
