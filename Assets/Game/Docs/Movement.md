# 이동 (Movement)

플레이어 이동·점프·갈고리 관련 기획 메모와 구현 상태입니다.

| 항목 | 상태 |
|------|------|
| 걷기 | ✅ 완료 (수치 조정 예정) |
| 대쉬 | ⏸ 보류 — 특성/모듈로 대체 검토 |
| 점프 | ✅ 완료 (수치 조정 예정) |
| 이단 점프 | ✅ 완료 (수치 조정 예정) |
| 갈고리 | ✅ 완료 (수치·맵 설계 조정 예정) |

---

## 걷기

지형 위에서 좌우 이동한다.

- **상태:** 완료
- **구현:** `Scripts/Player/PlayerMovement.cs`
- **입력:** `Move` (WASD / 스틱)
- **동작:** 지상·공중 가속/감속 분리, `moveSpeed` 기준 목표 속도로 보간
- **추후:** 가속·최고속·마찰 등 Inspector 수치 튜닝

---

## 대쉬

순간적으로 속도가 빨라지며 이동. 쿨타임 2초.

- **상태:** 미구현 / 필요 여부 검토 중
- **검토:** 별도 대쉬 대신 **시간 가속 능력**으로 대체 가능하지 않을까?
- **대안:** 추후 **특성(바퀴 모듈)** — 장착 시 **기본 이동 속도가 빨라지는** 체감으로 대쉬 역할 대체
- **결정:** 이동 파트 단독 기능으로 넣기 전, 특성·능력 트리와 함께 재검토

---

## 점프

버튼을 **누른 시간**에 따라 점프 높이가 달라진다. 최대 높이에 상한이 있다.

- **상태:** 완료
- **구현:** `PlayerMovement.cs`
  - `jumpForce` — 초기 상승 속도
  - `jumpCutMultiplier` — 버튼을 뗄 때 수직 속도 감쇠 (낮은 점프)
  - `lowJumpGravityMultiplier` — 상승 중 버튼을 떼면 추가 중력
  - `fallGravityMultiplier` — 하강 시 추가 중력
  - `coyoteTime` / `jumpBufferTime` — 코요ote·점프 버퍼
- **추후:** `jumpForce`, 중력 배율, 상한 체감 튜닝

---

## 이단 점프

공중에서 **점프와 같은 로직**으로 한 번 더 점프할 수 있다. 공중 점프는 지상 점프보다 **상대적으로 낮은** 높이.

- **상태:** 완료
- **구현:** `PlayerMovement.cs`
  - `maxAirJumps` — 공중 점프 횟수 (기본 1)
  - `airJumpForce` — 공중 점프 힘 (`jumpForce`보다 낮게 설정)
  - 착지 시 `airJumpsRemaining` 리셋
- **추후:** 공중 점프 높이·횟수 밸런스

---

## 갈고리

특정 지점에 갈고리를 걸어 이동한다. (기획 원안: 발사 중 낙하 없음, 목표 쪽으로 당겨지듯 이동, 가까울수록 가속.)

### 현재 구현 (Game)

- **상태:** 완료 (기획 원안과 표현·수치는 계속 다듬을 예정)
- **스크립트**
  - `Scripts/Player/GrapplingHookController.cs` — 선택·스냅·앵커/모멘텀 처리
  - `Scripts/World/GrapplePoint.cs` — 포인트 종류·반경
  - `Scripts/World/GrappleSlowMotion.cs` — 선택 중 슬로모
- **입력:** `Interact` (E) — 포인트 선택 / 확정
- **포인트 종류**
  | 종류 | 색 (기본) | 동작 |
  |------|-----------|------|
  | **Anchor** | 청록 | 붙는 위치(`AttachPosition`)까지 스냅 후 고정. 점프·이동 입력 시 해제 |
  | **Momentum** | 노랑 | 플레이어→앵커 방향으로 관성 발사, 앵커 통과 시 로프 해제·관성 유지 |

- **선택 흐름:** E → 슬로모 + 방향키로 포인트 선택 → E 확정 → 로프 스냅 → Anchor / Momentum 분기
- **Momentum 해제 후:** x·y 관성 유지 (`momentumPreserveDuration`, 입력 시 일반 이동으로 복귀)

### 기획 방향 (맵·능력과 연동)

원안과 현재 구현을 맞추기 위한 장기 아이디어:

1. **매달릴 수 있는 포인트 / 없는 포인트** 구분
2. **초반:** 매달리기(Anchor) 위주 맵 → 슬로모 없이도 플레이 가능
3. **중반 이후:** 보스 전·연산 가속(시간 감속) 없이는 선택하기 어려운 **세밀한 Momentum 포인트** 배치
4. **능력 연동:** 연산 가속 획득 후 갈고리 모션·선택 UX 변화 (슬로모 의존도 감소)

→ 초반은 Anchor 중심, 후반은 Momentum + 시간 능력으로 난이도·표현 분리.

### 추후 조정

- `launchSpeed`, `launchGravityScale`, 스냅·통과 반경
- Anchor 붙음 시간·해제 조건
- 포인트 `useRadius`, 맵별 배치 가이드

---

## 관련 파일

```
Game/
  Input/InputSystem_Actions.inputactions   ← Move, Jump, Interact
  Scripts/Player/PlayerMovement.cs
  Scripts/Player/GrapplingHookController.cs
  Scripts/World/GrapplePoint.cs
  Scripts/World/GrappleSlowMotion.cs
  Scenes/Game_Main.unity                   ← 테스트용 GrapplePoint 배치
```

그랩 활성 중에는 `PlayerMovement`가 Update/FixedUpdate를 스킵한다.

---

## 개발 메모

**2026-06-27 03:07**

- 기능 구현에 AI 활용은 수월하게 진행 중
- 코드가 점점 **누더기**처럼 쌓이는 중 — 초반에는 큰 문제 없으나 **최적화 단계**에서 영향 확인 필요
- 아직 **통합 개발** 진행 중
- **인벤토리 구현 이후** Player / World / Combat 등 **분리 여부** 결정 예정

---

## Legacy 참고

프로토타입 갈고리·이동: `Assets/_Legacy/Scripts/Player/`  
본편(`Game`)은 Legacy와 Input·어셈블리가 분리되어 있다.
