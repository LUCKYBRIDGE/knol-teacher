# 놀티쳐 개발 마스터 플랜
v1.0.0은 새 공식 저장소의 안정 기준선이다. 현재 기능 개발선은 v1.1.0이다.

1. 교실에서 즉시 이해할 수 있는 UX를 우선한다.
2. 학생 데이터는 번호 중심, Local-Only, 최소 저장 원칙을 유지한다.
3. 놀보드 위젯과 독립 Window의 동작을 명확히 구분한다.
4. 저장은 원자적 저장과 복구 가능한 backup을 우선한다.
5. 대형 UI 구조는 기능 단위로 점진 분리한다.
6. 모든 변경은 branch → PR → CI → merge 순서를 따른다.
7. PR CI에서 restore, Release build, 자동 테스트, single-file package, embedded version, SHA-256 검증을 통과해야 한다.
8. Stable Release 승격 전에는 실제 교실 사용 흐름을 기준으로 핵심 UI/듀얼 모니터/판서/시간표 회귀 점검을 수행하며, `docs/V1_1_RELEASE_REGRESSION_CHECKLIST.md`를 현재 v1.1.0 수동 검증 기준으로 사용한다.
9. 사용자 업데이트 안내는 교사가 체감하는 변화만 설명한다.

v1.1.0은 현재까지 반영된 판서·놀보드·듀얼 모니터·시간표 개선을 포함하는 다음 기능 릴리스 후보로 관리한다. 긴급 오류 수정은 공개된 Stable Release를 기준으로 patch 버전을 사용한다.
