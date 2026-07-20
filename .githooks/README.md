# Git hooks — CHANGELOG (월별 누적)

## 설치 (프로젝트당 1회, 각 개발자)

```powershell
git config core.hooksPath .githooks
git config alias.ship '!sh .githooks/ship'
```

Python 3 필요 (`python` 또는 `py`).

## 동작 요약 — `git ship` 으로 push

changelog는 **push 직전에 커밋**되어 **단 한 번의 push**로 코드·에셋과 함께 나갑니다.

```powershell
git ship                 # 현재 upstream 으로 push
git ship origin master   # 인자는 그대로 git push 로 전달
```

`git ship` 이 하는 일:

1. push 범위 `@{u}..HEAD`의 커밋·diff 수집
2. **`docs/changelog/YYYY-MM.md`** 맨 위에 날짜 블록 **추가** (누적, 최신이 위)
3. **`CHANGELOG.md`** 월별 링크 목록 갱신
4. 변경 있으면 `docs: changelog 갱신 (...)` **자동 커밋** (push 전)
5. **단일 `git push`** → 코드·에셋·changelog 커밋 전부 remote로 업로드

push할 **새 커밋이 없으면** changelog 생략하고 push만 진행.

### 왜 `git push` 가 아니라 `git ship` 인가

이전에는 `pre-push` 훅이 changelog 커밋을 만든 뒤 **스스로 push** 했다.
그러면 바깥 `git push` 가 이미 옮겨진 ref를 못 맞춰 매번
`remote rejected / cannot lock ref` 에러를 뱉었다 (내용은 나갔지만 혼란).

이제 `git ship` 이 changelog를 **push 전에** 커밋하므로 한 번의 push로 전부
나가고, 훅은 아무 것도 하지 않아 **에러가 없다**.

### 일반 `git push` 를 하면

`pre-push` 가드가 **changelog 없이 나가지 않도록 안내 후 차단**한다.
정말 changelog 없이 push하려면:

```powershell
git push --no-verify      # 또는
$env:SHIP_PUSH=1; git push
```

## 파일 구조

```
CHANGELOG.md                 ← 월별 목차 (링크)
docs/changelog/
  2026-07.md                 ← 7월 push 일지 (최신이 위)
  2026-08.md                 ← 8월 첫 push 때 자동 생성
.githooks/
  ship                       ← git ship 진입점 (changelog 커밋 + 단일 push)
  pre-push                   ← push 가드 (git ship 아니면 차단)
  lib/update_changelog.py    ← 생성·분류·저장 로직
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
- Assets/_MainGame/...

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

` — ` 뒤 한국어만 모아 월별 changelog **`### 요약`** 섹션에 넣는다. 별도 수동 작성 불필요.

## 미리보기 (파일 생성 없음, stdout)

```powershell
py .githooks/lib/update_changelog.py --preview - --simulate   # 커밋 전
py .githooks/lib/update_changelog.py --preview -              # 커밋 후
```

## hook 해제

```powershell
git config --unset core.hooksPath
git config --unset alias.ship
```

## 나중에 확장 가능

- CHANGELOG 한 달 파일이 NKB 넘으면 **아카이브만** 옮기기 (같은 `docs/changelog/` 유지)
- LLM 요약: `update_changelog.py`의 `build_entry` 뒤에 API 호출 추가
