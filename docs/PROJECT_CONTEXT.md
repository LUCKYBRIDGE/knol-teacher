# 놀티쳐 프로젝트 컨텍스트
- 공식 저장소: `LUCKYBRIDGE/knol-teacher`
- 안정 기준선: v1.0.0
- 현재 기능 개발선: v1.1.0
- 플랫폼: Windows 10/11 x64
- 앱: C# / .NET 8 / WPF
- 로컬 실행파일: `놀티쳐.exe`
- GitHub Release asset: `KnolTeacher.exe`

놀티쳐는 교사가 수업 중 자주 사용하는 타이머, 일정, 판서, QR, 학생 선택, 놀보드 위젯과 학급 운영 기능을 한 앱에서 빠르게 사용하는 것을 목표로 한다. 학생 데이터는 번호 중심·Local-Only가 기본이다.

v1.0.0은 새 공식 저장소의 안정 기준선이다. 현재 기능 개발은 v1.1.0 개발선에서 진행하며, `Directory.Build.props`의 `KnolTeacherVersion`을 개발 버전 SSOT로 사용한다.

실제 Stable Release로 승격할 때만 `release/release-version.txt`와 사용자용 업데이트 안내를 해당 버전에 맞춰 변경한다. 앱 업데이트는 이 저장소의 Stable Release만 확인하며, 검증된 파일만 로컬 `놀티쳐.exe`를 교체하고 재실행한다.
