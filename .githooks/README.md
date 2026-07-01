# Git hooks — CHANGELOG (월별 누적)

## 설치 (프로젝트당 1회)

```powershell
git config core.hooksPath .githooks
```

Python 3 필요 (`python` 또는 `py`).

## 동작 요약

`git push` **직전** (`pre-push`):

1. push 범위 `@{u}..HEAD`의 커밋·diff 수집
2. **`docs/changelog/YYYY-MM.md`** 맨 위에 날짜 블록 **추가** (누적, 최신이 위)
3. **`CHANGELOG.md`** 월별 링크 목록 갱신
4. 변경 있으면 `docs: changelog 갱신 (...)` **자동 커밋**
5. push 계속 → **코드·에셋·changelog 커밋 전부** remote로 업로드

push할 **새 커밋이 없으면** changelog 생략.

## 파일 구조

```
CHANGELOG.md                 ← 월별 목차 (링크)
docs/changelog/
  2026-07.md                 ← 7월 push 일지 (최신이 위)
  2026-08.md                 ← 8월 첫 push 때 자동 생성
.githooks/
  pre-push                   ← hook 진입점
  lib/update_changelog.py    ← 생성·분류·저장 로직
  preview-changelog          ← 미리보기
```

## 월별 블록 형식 (예)

```markdown
## 2026-07-01 14:32:05 KST

**브랜치:** `master` → `origin`
**범위:** `origin/master..HEAD` (`f3e7629`..`abc1234`)
**커밋 수:** 2

### 요약
- 걷기·대기 애니 재연결
- 이동 감속 적용

(커밋 본문 불릿 `영어 — 한국어`에서 ` — ` 뒤만 자동 추출. `commit-message` Cursor rule 참고.)

### 추가
- walk/idle 애니 재연결 (`abc1234`)

### 변경
- 이동 감속 적용 (`def5678`)

### 커밋
- feat: ... (`abc1234`)

### 변경 파일
- Assets/Game/...

### diff 요약
```
13 files changed, ...
```

---
```

## 커밋 메시지 분류 (Conventional Commits)

| 접두사 | CHANGELOG 섹션 |
|--------|----------------|
| `feat:` | 추가 |
| `fix:` | 수정 |
| `refactor:`, `perf:`, `change:` | 변경 |
| 그 외 | 기타 |

접두사 없으면 **기타** + **커밋** 목록에 그대로 표시.

## 한국어 요약 (`### 요약`)

기능 커밋 본문을 `commit-message` rule 형식으로 쓰면:

```
- ComboDefinition + FistCombo asset — 주먹 3연타 데이터(SO) 및 히트박스 설정
```

push hook이 ` — ` 뒤 한국어만 모아 월별 changelog **`### 요약`** 섹션에 넣는다. 별도 수동 작성 불필요.

## 에이전트 미리보기 (채팅)

Cursor Rule `push-changelog-confirm` — push 요청 시 **채팅에** changelog 초안 표시 후 확인.

hook과 동일한 블록이 필요할 때만 stdout (파일 생성 없음):

```powershell
py .githooks/lib/update_changelog.py --preview - --simulate   # 커밋 전
py .githooks/lib/update_changelog.py --preview -              # 커밋 후
```

## 비활성화 (1회)

```powershell
git push --no-verify
```

## hook 해제

```powershell
git config --unset core.hooksPath
```

## README 마커 (구버전)

이전 README 마커 방식은 **사용하지 않습니다**. `README.md`의 `git-hook:changelog` 블록은 수동으로 지우거나 그대로 둬도 push hook과 무관합니다.

## 나중에 확장 가능

- CHANGELOG 한 달 파일이 NKB 넘으면 **아카이브만** 옮기기 (같은 `docs/changelog/` 유지)
- LLM 요약: `update_changelog.py`의 `build_entry` 뒤에 API 호출 추가
- `post-push` + `last-push-sha`: upstream 없을 때 범위 보조 (현재는 `@{u}..HEAD` 우선)
