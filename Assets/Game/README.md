# 새 게임 (`Assets/Game`)

여기서 **처음부터 다시** 만드는 본편 게임 코드를 둡니다.

## 문서

- [이동 (Movement)](Docs/Movement.md) — 걷기·점프·갈고리 기획 및 구현 상태
- [플레이어 애니메이션](Docs/PlayerAnimation.md) — Animator·방향·콤보 공격·**추후 프레임 증가 시 확인 항목**
- [전투 · 콤보 SO](Docs/Combat.md) — `ComboDefinition`·히트박스·이벤트 size 파라미터
- [Docs 작성 규칙](Docs/README.md) — 기능별 md 명세·Future 체크리스트 워크플로
- 아래 **콤보 공격** — 주먹 3연타·입력 버퍼

## 현재 상태

- 시작 씬: `Scenes/Game_Main.unity` (Build Settings 등록됨)
- 진입 스크립트: `Scripts/Bootstrap/GameBootstrap.cs`
- 어셈블리: `Game.asmdef` (`ClockWork.Game` 네임스페이스 권장)

## 권장 폴더 (앞으로 추가)

```
Game/
  Scripts/
    Bootstrap/     ← 씬 초기화
    Player/
    Combat/
    World/
  Scenes/
  Prefabs/
  Resources/       ← 새 게임 전용 Resources.Load 경로
  Art/
```

## Play 테스트

1. `Game_Main.unity` 열기 (또는 Unity 재시작 후 Play)
2. Console에 `Clock Work — 새 게임 시작` 로그 확인

## 플레이어 아트 (`art/player/`)

### 2026-06-29 — 스프라이트·Visual (임시)

#### 프레임 크기 (기준)

| 항목 | 값 |
|------|-----|
| **목표 프레임** | **225 × 324** px (임시 기준) |
| **walk** | `tick - walk.png` 슬라이스 225×324 (`tick - walk_0`, `tick - walk_1`) |
| **wait** | `tick - wait.png` 슬라이스 **420 × 575** (아직 walk와 크기 다름) |
| **PPU** | 100 (walk 기준) |
| **Animator** | `visual.controller` (Idle / Walk, `Speed` 파라미터) |

#### wait → walk 키 맞추기 (420×575 → 225×324)

Unity Import만으로 슬라이스 픽셀 리사이즈는 불가. 씬에서 맞출 때:

| 방법 | 값 |
|------|-----|
| **Visual Scale (uniform, 키 높이 기준)** | **324 ÷ 575 ≈ 0.563** |
| **Visual Scale (가로 225 기준)** | 225 ÷ 420 ≈ 0.536 |
| **wait PNG PPU만 올리기** | 100 × (575÷324) ≈ **178** |

> 추후 아트를 Photopea 등에서 **225×324**로 통일·재슬라이스 예정.

#### 씬 구조 (`Game_Main`)

- **`player` Transform = 발** (`PlayerMovement.feetAtTransform`, Capsule `Offset.y = Size.y × 0.5`)
- **`player/Visual`** — SpriteRenderer + Animator + `PlayerCharacterVisual`
- 루트 placeholder `SpriteRenderer`는 끔 (`GameBootstrap`)
- Animator: `Idle_Left` / `Idle_Right` (`FaceLeft`), Walk (`Speed`), 공격 3연타
- flipX: Walk·공격만 (Idle은 좌/우 전용 스프라이트)

## 콤보 공격 (주먹)

- 스크립트: `Scripts/Player/PlayerFistCombat.cs` · 데이터: `Resources/Combos/FistCombo.asset` (`ComboDefinition`)
- 상세: [Docs/Combat.md](Docs/Combat.md) · 애니: [Docs/PlayerAnimation.md](Docs/PlayerAnimation.md)
- 모션 유지: 1타 0.35초, 2·3타 0.6초 (`ComboDefinition` Step · `motionHold`)

### 입력 버퍼

- **모든 단계 공통 0.35초** (`comboInputBuffer`)
- **모션 끝나기 0.35초 전 ~ 끝** 구간에서만 공격키 입력 받음 → 모션 끝과 동시에 다음 타 실행
- 그보다 일찍 누른 입력은 무시

### 공격 종료·Idle 복귀

- 공격 상태 **Exit Time → Idle 전환 없음** (자동 복귀 제거)
- `ComboMotionHold` 종료 시 `PlayerCharacterVisual`이 Idle/Walk로 복귀
- **프레임·클립 길이 늘릴 때** → [PlayerAnimation.md](Docs/PlayerAnimation.md) Future 체크리스트 확인

## Legacy와의 관계

- `_Legacy` 코드는 **자동 참조되지 않음** (`Legacy.asmdef`)
- 에셋이 필요하면 복사 후 `Game` 쪽 경로로 정리
- 프로토타입 씬: `Assets/_Legacy/Scenes/Legacy_SampleScene.unity`
