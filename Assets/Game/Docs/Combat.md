# 전투 · 콤보 데이터 (Combat)

## ComboDefinition (ScriptableObject)

무기·콤보별 **히트박스·모션 길이**를 `.asset` 파일로 관리합니다.

| 경로 | 용도 |
|------|------|
| `Resources/Combos/FistCombo.asset` | 주먹 3연타 (기본) |
| (추후) `Resources/Combos/SpearCombo.asset` 등 | 다른 무기 |

### Step 필드 (타당 1줄)

| 필드 | 의미 |
|------|------|
| `hitboxOffset` | 플레이어 기준 위치 (x는 `FacingDirection`으로 반전) |
| `hitboxSize` | `BoxCollider2D.size` |
| `useRightHand` | 디버그 박스 색 (오른손/왼손 구분) |
| `motionHold` | 이 타 모션 유지 시간(초) |

### 코드 연결

- `PlayerFistCombat` — `comboDefinition` 참조 (없으면 `Resources/Combos/FistCombo` 로드)
- 무기 교체 시 `ConfigureCombo(otherDefinition)` 또는 Inspector 할당

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

## 추후 구현 시 확인 (Future)

- [ ] 새 무기 → `ComboDefinition` 에셋 추가 + `PlayerFistCombat.ConfigureCombo`
- [ ] 프레임 증가 → 이벤트 시각 + `motionHold` + [PlayerAnimation.md](PlayerAnimation.md) 체크리스트
- [ ] 프레임마다 히트박스 크기 변경 → `HitboxSetSize(float)` 등 브릿지 메서드 + 문서 예시 구현
