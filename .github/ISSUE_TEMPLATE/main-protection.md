---
name: main branch protection
about: Track the repository-admin step required before a Stable Release
labels: release, infrastructure
---

## 목적

Stable Release 전에 `main` 브랜치에 GitHub branch protection 또는 ruleset을 활성화한다.

## 필수 설정

- [ ] pull request를 거쳐야 merge 가능
- [ ] `KnolTeacher CI / Build, test, and verify package` 성공 필수
- [ ] merge 전에 branch 최신화 요구
- [ ] force push 금지
- [ ] branch 삭제 금지

## 확인

설정 후 `main` branch API에서 `protected: true`인지 확인한다.
