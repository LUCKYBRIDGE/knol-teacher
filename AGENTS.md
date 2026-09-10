# KnolTeacher Repository Rules

## 제품 기준
- 공식 저장소: `LUCKYBRIDGE/knol-teacher`
- 안정 기준선: v1.0.0
- 현재 기능 개발선: v1.1.0
- 플랫폼: Windows 10/11 x64, C# / .NET 8 / WPF
- 로컬 실행파일: `놀티쳐.exe`
- GitHub Release asset: `KnolTeacher.exe`

## 개인정보
- 학생·학급 식별 정보는 Local-Only가 기본이다.
- 핵심 기능은 학생 번호만으로 완전하게 사용할 수 있어야 한다.
- 이름·개인별 기록은 선택 기능이며 필요한 경우에만 로컬 저장한다.
- 학생 정보를 자동으로 클라우드, 서버, AI 서비스에 전송하지 않는다.

## 개발
- 초기 저장소 구성 이후 `main` 직접 수정 금지. branch → PR → CI → merge를 따른다.
- Build, 자동 테스트, single-file package 검증을 통과한 변경만 병합한다.
- 대규모 재작성보다 점진적 개선을 우선한다.
- 저장 형식 변경에는 기존 데이터 보존과 복구 경로를 둔다.

## 버전과 Release
- `Directory.Build.props`의 `KnolTeacherVersion`이 현재 개발 소스 버전의 SSOT이다.
- `release/release-version.txt`는 실제 Stable Release로 승격된 버전만 기록한다.
- 공개 버전 이후 desktop 코드 변경 시 다음 개발 버전으로 올린다.
- 로컬 publish는 정확히 `놀티쳐.exe` 한 파일이어야 한다.
- GitHub Release에는 정확히 `KnolTeacher.exe` 한 asset만 공개한다.
- 업데이트는 이 저장소의 HTTPS Release, 크기, SHA-256, embedded FileVersion을 검증한다.

## 공개 안내와 자산
- 사용자 업데이트 안내는 교사가 실제로 체감하는 변화만 설명한다.
- 개발 과정과 내부 구현 용어는 사용자용 안내에 노출하지 않는다.
- 외부 제품·서비스를 기능 또는 화면 구성의 구현 출처나 유사성 근거로 제시하지 않는다.
- 프로젝트 시각 자산은 프로젝트를 위해 제작 또는 생성한 자산을 사용한다.
- 권리가 확인되지 않은 외부 이미지·음원·폰트를 저장소에 추가하지 않는다.
- 제3자 라이브러리는 해당 라이선스를 존중한다.
