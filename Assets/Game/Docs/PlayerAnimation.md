# 플레이어 애니메이션 (Player Animation)

`player/Visual` — Animator + `PlayerCharacterVisual` + 공격 클립.

| 항목 | 상태 |
|------|------|
| Idle 좌/우 (`FaceLeft`) | ✅ Animator 일원화 |
| Walk (`Speed`) | ✅ |
| 주먹 3연타 (`attack_fist`, `attack_fist_con`) | ✅ |
| 공격 → Idle Exit Time | ❌ 제거 (코드가 복귀 처리) |

## Animator (`visual.controller`)

| 파라미터 | 용도 |
|----------|------|
| `Speed` | Walk 전환 (`> 0.05`) |
| `FaceLeft` | `Idle_Left` / `Idle_Right` |
| `attack_fist` | 공격 트리거 |
| `attack_fist_con` | 1 / 2 / 3타 |

| 상태 | 클립 | flipX |
|------|------|-------|
| `Idle_Left` | `Player_Idle.anim` (`wait 1_0`) | false |
| `Idle_Right` | `Player_Idle_Right.anim` (`wait 1_1`) | false |
| `Walk` | `Player_Walk.anim` | `FaceLeft`일 때 true |
| `tick_attack_fist_1/2/3` | 각 공격 클립 | `FaceLeft`일 때 true |

공격 종료 후 `PlayerCharacterVisual.RestoreLocomotionState()`가 Idle/Walk로 복귀.

---

## 추후 구현 시 확인 (Future)

기능을 확장할 때 **아래 항목을 반드시 다시 맞출 것**.

### 공격 프레임·클립 길이 늘릴 때

- [ ] `tick_attack_fist_1/2/3.anim` 클립 길이
- [ ] `ComboDefinition` Step `motionHold` (1타 0.35s, 2·3타 0.6s)
- [ ] `comboInputBuffer` (0.35s) — 모션 끝 0.35s 구간과 일치하는지
- [ ] 히트박스 애니 이벤트 시각 (`HitboxOn` / `HitboxOff`)
- [ ] **Exit Time → Idle 자동 전환을 다시 넣을지** — 넣으면 `ComboMotionHold`·코드 복귀(`RestoreLocomotionState`)와 타이밍 충돌 여부 검토
- [ ] Any State → 공격 **Transition Duration** (블렌드) — 프레임이 많아지면 0이 아닌 값 검토

### 방향·스프라이트

- [ ] wait 시트 슬라이스 변경 시 `PlayerSpriteAnimationBuilder.RebuildAll()` (메뉴: Clock Work → Player → Rebuild Idle && Walk Animations)
- [ ] 공격 스프라이트가 **한 방향 + flipX** 체계 유지하는지, 아니면 좌/우 클립 분리할지

### Walk / Idle 아트 통일 (225×324)

- [ ] `README.md` 플레이어 아트 섹션 — wait/walk PPU·스케일

### 신규 애니 상태 추가 시

- [ ] `PlayerCharacterVisual.ShouldMirrorSprite()`에 상태 이름 추가 (flipX 대상)
- [ ] `RestoreLocomotionState()` 복귀 규칙

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-07-02 | Animator 일원화 (`FaceLeft`, Idle_Left/Right), 공격 Exit Time 제거 |
