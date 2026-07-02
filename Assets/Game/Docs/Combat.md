# 전투 · 콤보 데이터 (Combat)

## ComboDefinition (ScriptableObject)

무기·콤보별 **히트박스·모션 길이**를 `.asset` 파일로 관리합니다.

| 경로 | 용도 |
|------|------|
| `Resources/Combos/FistCombo.asset` | 주먹 3연타 (기본) |
| `Resources/Combos/HammerCombo.asset` | 망치 2연타 |
| `Resources/Weapons/Hammer.asset` | 망치 무기 (`WeaponDefinition`) |
| `Resources/Weapons/Fist.asset` | 손날 무기 (`WeaponDefinition`) |
| (추후) `Resources/Combos/SpearCombo.asset` 등 | 다른 무기 |

### Step 필드 (타당 1줄)

| 필드 | 의미 |
|------|------|
| `hitboxOffset` | 플레이어 기준 위치 (x는 `FacingDirection`으로 반전) |
| `hitboxSize` | `BoxCollider2D.size` |
| `useRightHand` | 디버그 박스 색 (오른손/왼손 구분) |
| `damage` | 타당 데미지 (0이면 `PlayerFistCombat` 기본값) |
| `motionHold` | 이 타 모션 유지 시간(초) |

### WeaponDefinition (ScriptableObject)

무기 단위로 콤보·파워어택·전환 콤보를 묶습니다.

| 경로 | 무기 | 콤보 | 비고 |
|------|------|------|------|
| `Resources/Weapons/Hammer.asset` | 망치 | `HammerCombo` 2연타 | 데미지 2/2, `motionHold` 0.6s |

| 필드 | 의미 |
|------|------|
| `weaponId` | 식별자 (`hammer`) |
| `displayName` | 표시 이름 |
| `combo` | `ComboDefinition` |
| `powerAttack` | `PowerAttackDefinition` (없으면 비움) |
| `transitionCombo` | 탭 윈도우 무기 전환 시 재생할 짧은 콤보 (선택) |

### 코드 연결

- `PlayerFistCombat` — `comboDefinition` 참조 (없으면 `Resources/Combos/FistCombo` 로드)
- 무기 교체 시 `ConfigureWeapon(weaponDefinition)` 또는 `ConfigureCombo(otherDefinition)`
- `PlayerWeaponController` — 원형 큐 `WeaponDefinition[]`, **Q** 다음 무기, **Numpad1/1·Numpad2/2** 슬롯
- `WeaponSlotUI` — 화면 **좌하단** 현재 무기 이름 표시

에셋 생성: Unity 메뉴 **Clock Work → Combat → Ensure Fist Combo Asset**

---

## 히트박스 타이밍 (애니 이벤트 · 5번)

| 이벤트 | 역할 |
|--------|------|
| `HitboxOn` | 히트박스 켜기 |
| `HitboxOff` | 히트박스 끄기 |

타이밍은 클립(`tick_attack_fist_1/2/3.anim`)의 이벤트 시각. **크기·위치는 SO Step** (`StrikeRoutine` 시작 시 적용).

### 추후: 이벤트 `floatParameter`(size) 의미

프레임이 여러 장일 때, **한 타 안에서** 히트박스 크기를 바꾸려면 애니메이션 이벤트에 **Float 파라미터**를 붙일 수 있습니다.

예시 (미구현):

```csharp
// PlayerAttackAnimEvents.cs
void HitboxSetSize(float uniformScale) => combat?.SetHitboxSize(baseSize * uniformScale);
```

| Unity 이벤트 설정 | 의미 |
|-------------------|------|
| Function: `HitboxSetSize` | 호출할 메서드 |
| Float: `1.2` | 그 프레임에 size를 기본값의 1.2배로 |

또는 **가로·세로를 따로** 쓰려면:

- `HitboxSetWidth(float)` / `HitboxSetHeight(float)` 두 이벤트
- 또는 `intParameter` = Step 인덱스로 SO에서 size 조회

**정리**

- **SO** → 타마다 기본 offset / size / motionHold (**무엇을**)
- **이벤트 On/Off** → 언제 맞는지 (**언제**)
- **이벤트 float (추후)** → 그 프레임만 크기 덮어쓰기 (**프레임별 미세 조정**)

1프레임 공격만 있을 때는 SO Step의 `hitboxSize`만으로 충분. 프레임·다단히트가 늘면 이벤트 float를 추가.

---

## 에너지 게이지 (Energy Gauge)

적 타격 시 에너지가 충전되고, 화면 **우하단**에 세그먼트 게이지로 표시됩니다.

| 항목 | 값 |
|------|-----|
| 최대 에너지 | 100 |
| 세그먼트당 에너지 | 5 (총 20칸) |
| 타격당 충전 | 5 (`PlayerEnergyGauge.energyPerHit`) |
| 그룹 구분 | 5칸마다 굵은 구분선 (5·10·15 이후) |

### 코드 연결

- `DamageDealer.OnHit` — 유효 타겟에 피해 적용 시 발행
- `PlayerEnergyGauge` (Player) — `OnHit` 구독 → `Add(energyPerHit)`
- `EnergyGaugeUI` (`GameBootstrap`) — Screen Space Overlay, 우하단 앵커, `OnEnergyChanged`로 세그먼트 점등

---

## W 전투 슬로우 모드 (Combat Slow Mode)

`PlayerCombatMode` + `CombatModeSettings` SO + `CombatSlowMotion`(전역 `timeScale` 소유).

| 경로 | 용도 |
|------|------|
| `Resources/Combat/CombatModeSettings.asset` | 탭/홀드 타이밍·에너지·슬로모 수치 |

### W 탭 (릴리즈 < 0.5s)

- 진입: 에너지 **≥ 25**, **W 쿨다운 1s** 아님 (진입 시 소모 없음)
- **전역 슬로모** + **실시간 1초** + 캐릭터 우측 **세로 타이머 바** (`CombatTapWindowUI`)
- 1초 안:
  - **Attack** → 파워 어택 (−25), 모드 종료
  - **Q** → 다음 무기 전환 콤보 (−25), 모드 종료
  - **Numpad/1/2** → 해당 무기 전환 콤보 (−25), 모드 종료
  - **입력 없음** → 종료, **−5**
  - **W 재입력** → 타임아웃과 동일 (−5, 종료, 홀드 전환 없음)
- 탭 중 **그랩(E) 불가**
- 탭 윈도우 중 패시브 에너지 드레인 없음

### W 홀드 (≥ 0.5s)

- 진입: 에너지 **≥ 5** (0.5s 시점), **5/s** 드레인, 0이면 종료
- 이동 가능, `moveSpeed + 5` (절대 보너스)
- 전역 `timeScale` = 탭과 동일 (기본 0.16)
- **Attack** → 모드 종료 + **일반 공격**
- **W 릴리즈** → 종료
- **Numpad/1/2** (탭 없이) → 즉시 스왑 (0 에너지), 슬로우 유지
- **E 그랩 허용**, 드레인 계속

### 공격 중 W 캔슬

일반 공격·파워 어택 진행 중:

- W 다운 즉시 캔슬 불가 — **릴리즈/0.5s 해상** 후 판정
- **탭 경로** (릴리즈 < 0.5s): 해상 시 **≥ 25** → 공격 코루틴 취소, `comboIndex=0`, 탭 모드 진입
- **홀드 경로** (0.5s 이상 홀드): **≥ 5** → 공격 취소, 홀드 모드 진입
- 에너지 부족 시 공격 **계속**
- 파워 어택에 이미 쓴 −25 **환불 없음**

### 상호 배제·우선순위

- 탭 vs 홀드: **0.5s 경계** (아이들 진입 시 홀드가 탭 대체)
- 모드 종료 후 **W 쿨다운 1s**
- 동일 프레임: Attack / Q / 슬롯 키가 W 캔슬보다 **우선**

### 무기 큐

- `WeaponDefinition[]` 원형 큐 — **Q** = `(i+1) % n` (Q 누른 순간 인덱스)
- 홀드 중 Numpad/1/2 → 즉시 스왑 (0)
- 탭 중 Q / 슬롯 → 전환 콤보 SO (−25)

### 그랩 연동

- 앵커 선택 시 **자동 슬로모 제거**, **G 토글 제거**
- 앵커 선택·공중: **W 홀드**로 슬로우 조준 (0.5s 시 ≥5)
- 탭 모드: E 차단
- 홀드 모드: E 허용

### 파워 어택 (Power Attack)

| 항목 | 값 |
|------|-----|
| 진입 | **W 탭** → 1초 윈도우 → **Attack** (−25) |
| 시전 | `PlayerFistCombat.TryStartPowerAttack()` |
| 종료 | 패턴 완료 후 일반 상태 |

### PowerAttackDefinition (ScriptableObject)

| 경로 | 용도 |
|------|------|
| `Resources/Combos/FistPowerAttack.asset` | 주먹 파워 10연타 |

| 필드 | 의미 |
|------|------|
| `pattern` | `attack_fist_con` 시퀀스 (주먹: `2,1,2,1,2,1,2,1,2,3`) |
| `damagePerHit` | 타당 데미지 |
| `strikeInterval` | 타 간격(초) |
| `hitActiveDuration` | 히트박스 ON 유지(초) — 코드가 직접 On/Off |
| `finisherHoldDuration` | 마지막 타 모션·히트박스 유지(초, 주먹: 0.5) |
| `hitboxReference` | `ComboDefinition` — con 1/2/3 → Step 0/1/2 히트박스 참조 |

### 코드 연결

- `PlayerCombatMode` — W 상태 머신 (`Idle` / `TapWindow` / `HoldDrain` / `WListeningDuringAttack`)
- `CombatSlowMotion` — 전역 `timeScale` 슬로모 (`PlayerCombatMode` W 탭/홀드)
- `PlayerFistCombat` — `CancelAttack()`, `TryStartPowerAttack()`, `TryPlayTransitionStrike()`
- `CombatTapWindowUI` — 탭 윈도우 세로 바 (캐릭터 추적)
- `GameBootstrap` — `PlayerCombatMode`, `CombatTapWindowUI` 자동 부착

### 입력

| 액션 | 키 | 용도 |
|------|-----|------|
| `PowerMode` | W | 전투 슬로우 모드 |
| `WeaponNext` | Q | 다음 무기 (탭: 전환 콤보, 일반: 즉시) |
| `WeaponSlot1` | Numpad1 / 1 | 슬롯 1 |
| `WeaponSlot2` | Numpad2 / 2 | 슬롯 2 |
| `GrappleCancel` | X | 그랩 선택 취소 |

`UseConsumable` 액션·바인딩 **제거**.

---

## 추후 구현 시 확인 (Future)

- [ ] 새 무기 → `ComboDefinition` 에셋 추가 + `PlayerWeaponController` 큐 등록
- [x] 무기별 `transitionCombo` 에셋 연결 (`FistTransition`, `HammerTransition`)
- [ ] 프레임 증가 → 이벤트 시각 + `motionHold` + [PlayerAnimation.md](PlayerAnimation.md) 체크리스트
- [ ] 프레임마다 히트박스 크기 변경 → `HitboxSetSize(float)` 등 브릿지 메서드 + 문서 예시 구현
