# Legacy 프로토타입 문서 (v0)

> 2026-06-01~02 세션에서 만든 실험용 기능 정리.  
> 코드·씬 위치: `Assets/_Legacy/` · 씬: `Legacy_SampleScene.unity`

## 플레이 가능했던 기능

| 기능 | 조작 | 설명 |
|------|------|------|
| **막기** | `W` | 공격 직전 0.5초 안에 입력 → 성공 시 **세계만** 3초 감속 |
| **콤보 선택** | 막기 후 3초 내 `1`→`2` 또는 `2`→`1` | 막기 없이 1/2만 누르면 콤보 **발동 안 함** |
| **콤보 1→2** | 손날 → 망치 | 손날 3연 + 망치 1타, 종료 후 망치 장착 |
| **콤보 2→1** | 망치 → 손날 | 망치 투척 → 착지 이동 → 손날 2연 |
| **무기 교체** | `1` 손날 / `2` 망치 | 전투 테스트 맵 시작 시 망치 자동 지급 |
| **일반 공격** | `A` | 장착 무기 근접·차지 공격 |
| **포션** | `Q` | HP 회복 |
| **인벤·상태** | `Tab` | 아이템·HP 확인 |
| **NPC 거래** | `E` | 고철 → 포션 |
| **허수아비 / 보스** | `A` / 자동 패턴 | 전투 테스트 |
| **맵 문 이동** | `E` | 스토리 ↔ 콤보 연습 맵 등 |
| **저장** | `F5` / `F9` | JSON 세이브 (프로토타입) |

## 주요 스크립트 (`Assets/_Legacy/Scripts/`)

| 영역 | 파일 |
|------|------|
| 보스 | `BossController`, `BossDangerZone`, `BossArcProjectile` |
| 막기·감속 | `PlayerParryController`, `WorldSlowMotion` |
| 콤보 | `PlayerComboController`, `HammerThrowProjectile` |
| 전투 | `PlayerCombatController`, `PlayerWeaponInventory` |
| 인벤 | `PlayerItemInventory`, `ItemDatabase` |
| NPC | `NpcMerchant`, `PlayerNpcInteractor`, `TradeUI` |
| 맵 | `UndergroundMapBootstrap`, `MapDoor` |
| 갈고리 | `GrapplingHookController`, `GrapplePoint` |
| 저장 | `PlayerSaveController` |
| 비주얼 | `PlayerGearbotVisual`, `GearbotMotionLoader` |

## 입력

`Assets/_Legacy/Input/InputSystem_Actions.inputactions`

| 액션 | 키 |
|------|-----|
| Block | W |
| Grapple / Interact | E |
| Previous / Next | 1 / 2 |
| UseConsumable | Q |
| MenuToggle | Tab |
| Attack | A |

## 테스트 방법 (Legacy 씬)

1. `Assets/_Legacy/Scenes/Legacy_SampleScene.unity` 열기
2. `UndergroundMapBootstrap` 인스펙터에서 맵 모드 확인 (`combatTestMap` / `grappleTestMap`)
3. Play
