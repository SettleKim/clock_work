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

### 스케일 보정 (프레임별 크기 편차)

AI로 뽑은 프레임들은 생성 배치마다 캐릭터 크기가 다르게 나옴 (idle 기준 대검 +40~50%, 대시 배치 내 최대 30% 등). 각 공격/이동 클립에 `Transform.localScale.x/y` 커브를 추가해서, idle-01-open.png의 실제 픽셀 높이(304px)를 기준으로 프레임마다 `기준높이 ÷ 이 프레임 높이` 배율을 곱해 보정함. 새 프레임 추가 시:

- [ ] 새 스프라이트도 같은 방식(알파 스캔으로 높이 측정 → 기준 대비 배율 → localScale 커브)으로 보정할 것
- [ ] 이 보정은 **전체 크기(스케일) 편차만** 잡음 — 머리/몸통/팔 등 부위별 **비율 자체**가 다르게 그려진 경우는 못 고침 (아래 "본 리깅" 참고)

### 본(뼈대) 리깅 전환 — 추후 고려

지금 방식(매 포즈를 완성된 한 장짜리 이미지로 생성)은 생성할 때마다 비율이 흔들리는 근본 원인. 프로토타입은 이미 검증됨: `Assets/_MainGame/Prototypes/RigTest/` (파츠 분리 + 회전만으로 포즈 생성 + Sprite Resolver로 극단적 각도만 파츠 교체, Unity 2D Animation 패키지의 Sprite Swap 기능 사용).

- 장점: 파츠 비율이 한 번 정해지면 이후 모든 포즈에서 구조적으로 고정됨(스케일 보정 같은 사후 보정 자체가 불필요해짐). 새 콤보 만들 때 AI 재생성 없이 에디터에서 회전 키프레임만 찍으면 됨 — 남은 개발 기간(1년+)과 낮은 주당 작업 시간 조합에 유리.
- 비용: 기존 캐릭터를 부위별(머리/몸통/상완/하완+무기/허벅지/정강이)로 분리한 아트가 필요함 — 이게 유일한 병목. Unity 쪽 리그 세팅은 프로토타입에서 이미 검증돼서 추가 리스크 낮음.
- 파츠 아트도 레퍼런스만 있으면 AI 생성 가능하지만, 파츠를 **각각 따로** 생성하면 관절 이음새 크기가 안 맞을 위험이 있음 — 그것보다는 **T포즈처럼 팔다리를 벌린 캐릭터 전신을 한 번에 생성**한 뒤 그 한 장을 관절선 기준으로 잘라 파츠를 만드는 방식을 권장.
- [ ] 실제 전환 시점 결정 (지금 무기 4종 프레임 기반 작업이 어느 정도 쌓인 뒤가 매몰비용 관점에서 유리)

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2026-07-02 | Animator 일원화 (`FaceLeft`, Idle_Left/Right), 공격 Exit Time 제거 |
| 2026-07-23 | 프레임별 스케일 보정 커브 추가 (idle/walk/dash/fist/hammer/greatsword/dagger 전체), 무기별 등 부착(`WeaponDefinition.BackAttachOffset`) 추가 |
