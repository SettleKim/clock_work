# Clock Work

Unity로 할로우나이트 스타일 2D 메트로배니아 게임을 만드는 프로젝트입니다.

> **프로젝트 경로:** `C:\Users\ggsej\Desktop\clock_work_unity`  
> Cursor에서 이 폴더를 열면 됩니다.

---

## 📁 프로젝트 구조 (하이브리드)

| 폴더 | 용도 |
|------|------|
| **`Assets/Game/`** | **새 게임** — 여기서부터 다시 개발 |
| **`Assets/_Legacy/`** | **프로토타입 보관** — 전투·맵·갈고리 실험 코드 (참고용) |
| `Assets/Settings/` | URP 등 프로젝트 공통 설정 |

### 시작 씬

- **본편:** `Assets/Game/Scenes/Game_Main.unity` ← Play 기본 씬
- **프로토타입:** `Assets/_Legacy/Scenes/Legacy_SampleScene.unity`

### 문서

- [Assets/Game/README.md](Assets/Game/README.md) — 새 게임 가이드
- [Assets/_Legacy/README.md](Assets/_Legacy/README.md) — Legacy 폴더 설명
- [README_LEGACY.md](README_LEGACY.md) — 프로토타입 기능·스크립트 목록

### 어셈블리 분리

- `Game.asmdef` — 새 코드 (`ClockWork.Game` 네임스페이스)
- `Legacy.asmdef` — 프로토타입 (`autoReferenced: false`, Game과 자동 분리)

---

## ⚡ 작업 시작할 때마다 (매번)

> 아래만 하면 됩니다. Setup·Configure·uv 설치 등은 **처음 한 번만** 했으면 다시 안 해도 됩니다.

### 코드만 작성할 때 (가장 흔함)

1. **Unity Hub** → `clock_work_unity` 프로젝트 열기
2. **Cursor** → 이 폴더(`clock_work_unity`) 열기
3. Unity에서 스크립트 더블클릭 또는 **Assets → Open C# Project** → Cursor에서 코드 작성
4. Unity로 돌아와 **Play(▶)** 로 테스트

### Cursor AI가 Unity 씬까지 조작할 때 (MCP 사용)

위 1~2에 더해서:

3. Unity → **Window → MCP For Unity → Toggle MCP Window** (`Ctrl+Shift+M`)
4. **Start Server** 클릭 → 새로 열린 **터미널 창을 닫지 않기**
5. Cursor **Settings → MCP** → `unityMCP`가 **초록색(연결됨)** 인지 확인

| 매번? | 항목 |
|-------|------|
| ✅ | Unity 프로젝트 열기, Cursor에서 프로젝트 폴더 열기 |
| ✅ (MCP 쓸 때만) | MCP Start Server + 터미널 유지 |
| ❌ | Setup 마법사, Configure Selected, uv 설치, External Tools 설정 |

**팁:** MCP 창에서 **Auto-start on Unity Open** 을 켜 두면 Unity 열 때 서버가 자동으로 시작됩니다.

---

## 📅 2026-06-29 — 플레이어 Visual·스프라이트 (임시)

- **목표 프레임:** 225 × 324 px (임시) — walk 슬라이스에 적용
- **wait 슬라이스:** 420 × 575 → walk 키에 맞출 때 uniform **324/575 ≈ 0.563** (Visual Scale) 또는 wait PPU **≈178**
- **Transform = 발** + `player/Visual` + `visual.controller` (Idle/Walk)
- 상세: [Assets/Game/README.md](Assets/Game/README.md) → **플레이어 아트**

---

## 📅 2026-06-02 — 프로젝트 구조 분리

- `Assets/_Legacy/` — 기존 프로토타입(스크립트·씬·Resources·Input) 이동
- `Assets/Game/` — 새 게임 시작 (`Game_Main.unity`, `GameBootstrap`)
- Build Settings → `Game_Main.unity`
- 상세: 위 **프로젝트 구조** 섹션 · [README_LEGACY.md](README_LEGACY.md)

---

## 📅 2026-06-01 — 프로토타입 세션 요약 (→ Legacy로 이동)

> 아래 내용은 `Assets/_Legacy`에 보관된 v0 프로토타입입니다.  
> 전체 목록은 [README_LEGACY.md](README_LEGACY.md) 참고.

### 현재 플레이 가능한 기능

| 기능 | 조작 | 설명 |
|------|------|------|
| **막기** | `W` | 공격 직전 0.5초 안에 입력 → 성공 시 **세계만** 3초 감속 (플레이어 속도 유지) |
| **콤보 선택** | 막기 후 3초 내 `1`→`2` 또는 `2`→`1` | 막기 없이 1/2만 누르면 콤보 **발동 안 함** |
| **콤보 1→2** | 손날 → 망치 | 손날 3연(1dmg×3) + 망치 1타(3dmg), 종료 후 망치 장착 |
| **콤보 2→1** | 망치 → 손날 | 망치 투척(2dmg) → 착지 지점으로 이동 → 손날 2연(1dmg×2), 종료 후 손날 장착 |
| **무기 교체** | `1` 손날 / `2` 망치 | 전투 테스트 맵 시작 시 망치 자동 지급 |
| **일반 공격** | `A` (Attack) | 장착 무기에 따른 근접·차지 공격 |
| **포션** | `Q` | HP 회복, 시작 3개, 우하단 UI 표시 |
| **인벤·상태** | `Tab` 열기, `←`/`→` 탭 전환 | 고철 등 아이템·HP 확인 |
| **NPC 거래** | `E` | 고철 1개 → 포션 1개 (Enter로 거래, Esc 닫기) |
| **허수아비** | `A` 공격 | 체력 10, 3초 후 부활, 머리 위 HP·누적 피해 표시 |
| **보스** | 자동 패턴 | 3종 스킬, 투사체·내려찍기 붉은 경고 **바닥** 표시 |

### 맵 (전투 테스트)

- `UndergroundMapBootstrap` → `combatTestMap = true` (기본값)
- 바닥 폭 **72**, 양끝 **벽**, 플레이어 스폰 `(-24, 2.2)`
- **허수아비** `x=-20` · **NPC** `x=-12` · **보스** `x=24`
- 고철 픽업 2개, 망치 픽업, 시작 포션 3개·망치 자동 지급

### 주요 스크립트

| 영역 | 파일 |
|------|------|
| 보스 | `BossController`, `BossDangerZone`, `BossArcProjectile` |
| 막기·감속 | `PlayerParryController`, `WorldSlowMotion`, `WorldSlowMotionRunner` |
| 콤보 | `PlayerComboController`, `HammerThrowProjectile` |
| 전투 | `PlayerCombatController` |
| 무기 | `PlayerWeaponInventory` |
| 인벤·포션 | `PlayerItemInventory`, `PlayerConsumableUse`, `ItemDatabase` |
| NPC·거래 | `NpcMerchant`, `PlayerNpcInteractor`, `TradeUI` |
| 허수아비 | `TrainingDummy`, `WorldHealthBar`, `DamagePopup` |
| UI | `PlayerMenuUI` |
| 맵 생성 | `UndergroundMapBootstrap` |

### 감속 구현 (`WorldSlowMotion`)

- `Time.timeScale` **사용하지 않음** → 플레이어가 느려지지 않음
- `WorldDeltaTime = Time.deltaTime × SlowScale` — 보스·투사체·위험존만 감속
- 플레이어 이동·입력은 `Time.deltaTime` 그대로

### 오늘 해결한 버그

- 보스 **공중 찍기** 미착지 → Kinematic 이동 + `MoveTransformTo`
- **투사체·위험 표시** 안 보임 → 스프라이트·sortingOrder, `BossDangerZone`
- **감속 시 플레이어까지 느려짐** → `WorldSlowMotion` 분리
- **콤보 미발동** → `PlayerWeaponInventory`와 `PlayerComboController` **1/2 입력 중복** 제거
- **콤보 데미지 미적용** → `Physics2D.OverlapBox` + 타격당 `HashSet<Health>`로 중복 방지
- **코루틴 중첩** → `yield return combat.StartCoroutine(...)` 패턴
- **콤보 데미지 2배(12)** → `DamageDealer` + `OverlapBox` 중복 → 콤보 시 Overlap만 사용
- **플레이어·적 겹침 불가** → `Physics2D.IgnoreCollision` + bounds 접촉 판정
- **내려찍기·투사체 경고 위치** → 바닥 Y(`floorSurfaceY`) 기준 표시
- **내려찍기 보스 순간이동** → 수직 상승 후 `MoveTowards`로 추적
- **돌진 막기** → 패턴 취소 + 보스 **1초 스턴**

### 코드 정리

- `Health` / `PlayerParryController` / `PlayerCombatController` 등 최적화
- 접촉 데미지 `OnCollision` + `FixedUpdate` **이중 경로** 제거
- `NpcMerchant`, `TrainingDummy`, `TradeUI` 등 **복구** (전투 맵에 허수아비·NPC 재배치)

### 캐릭터 모션 개선 (스프라이트 시트)

- 제공받은 모션 시트 → 배경 제거 후 `Assets/Resources/Gearbot/motion_frames/` 에 프레임 분리
- **걷기 6프레임** · **대시 4프레임** · **막기(가드) 4프레임** · **피격 1프레임**
- `PlayerGearbotVisual` — 프레임 애니메이션 재생
- `GearbotMotionLoader` — Resources에서 프레임 로드
- **W(막기)** 입력 시 가드 모션, **대시** 중 대시 모션, **피격** 시 hit 모션
- 재분할 스크립트: `Tools/slice_gearbot_motion.py`

### 코드 최적화 (세션 중반)

- `Health`: 플레이어일 때만 `PlayerParryController` / `PlayerComboController` 캐싱
- `PlayerParryController`: 중복 `GetComponent`·런타임 `AddComponent` 제거
- `PlayerCombatController`: Overlap 버퍼 재사용 (`OverlapBox` + `ContactFilter2D`)
- `PlayerComboController`: `Rigidbody2D` 캐싱, 불필요 `Debug.Log` 정리
- `UndergroundMapBootstrap`: `EnsureComponent<T>` 헬퍼

### 테스트 방법

1. Unity Hub → 프로젝트 열기 → **Play(▶)**
2. **허수아비** 공격 → HP·누적 피해·타격 숫자 확인
3. **E** → NPC 거래 (고철 → 포션)
4. 보스 공격 직전 **W** (막기) → 3초 안에 **1→2** 또는 **2→1**
5. 돌진 중 **W** 막기 → 보스 1초 스턴 확인
6. **Q** 포션, **Tab** 인벤

### 입력 (`InputSystem_Actions`)

| 액션 | 키 | 용도 |
|------|-----|------|
| Block | W | 막기 |
| Grapple / Interact | E | NPC 상호작용 |
| Previous | 1 | 손날 / 콤보 첫 입력 |
| Next | 2 | 망치 / 콤보 둘째 입력 |
| UseConsumable | Q | 포션 |
| MenuToggle | Tab | 메뉴 |
| Attack | A (마우스 좌클릭) | 공격 |
| Move | WASD / 방향키 | 이동 |

---

## 📅 2026-05-31 — 오늘 한 일 요약

> Unity·코딩 처음인 사람도 따라올 수 있도록, 오늘 세션에서 실제로 진행한 내용을 정리했습니다.

### 1. PC가 게임 개발에 쓸 만한지 확인

- 노트북: Samsung 750XGK / i5-1335U / Iris Xe / RAM 16GB
- **결론:** 2D 메트로배니아 **학습·프로토타입 가능**. RAM·GPU가 빠듯하므로 Unity 작업 전 Docker·WSL·불필요한 앱 정리 권장.

### 2. Unity 설치

| 프로그램 | 역할 | 버전 |
|----------|------|------|
| **Unity Hub** | Unity 버전·프로젝트 **관리** (Steam 같은 것) | 3.18.0 |
| **Unity Editor** | 실제로 게임 **만드는 프로그램** | 6000.3.16f1 (Unity 6.3 LTS) |

- Hub와 Editor는 **다른 프로그램**입니다. 평소 작업은 **Editor** 창에서 합니다.
- Editor 설치 경로: `C:\Program Files\Unity\Hub\Editor\6000.3.16f1`
- **Personal(무료) 라이선스** 동의 완료

### 3. Unity 프로젝트 생성

- 프로젝트 이름: **`clock_work_unity`**
- 경로: `C:\Users\ggsej\Desktop\clock_work_unity`
- 템플릿: **2D (URP)** — 2D 게임 + 가벼운 그래픽 파이프라인

### 4. Cursor ↔ Unity 연동 (코드 작성)

**역할 나누기 (앞으로 이렇게 씁니다):**

| 도구 | 하는 일 |
|------|---------|
| **Unity Editor** | 씬 배치, 오브젝트, Play 테스트 |
| **Cursor** | C# 스크립트 작성, AI에게 코드 질문 |

**한 번만 설정한 것:**

1. Package Manager → Git URL: `https://github.com/boxqkrtm/com.unity.ide.cursor.git`
2. **Edit → Preferences → External Tools** → External Script Editor = **Cursor**
3. **Regenerate project files**
4. Cursor에서 **이 폴더** 열기
5. **Assets → Open C# Project** 또는 `.cs` 더블클릭 → Cursor에서 열리는 것 **확인 완료**

### 5. MCP 연동 (Cursor AI ↔ Unity 에디터)

> MCP = Cursor가 Unity **씬·오브젝트를 원격으로** 다루게 하는 다리. **코드만 쓸 때는 필수 아님.**

**한 번만 설정한 것:**

1. Package Manager → Git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
2. **uv** 패키지 매니저 설치 (MCP 서버 실행용)
3. **Local Setup Window** → **Cursor만** 선택 → **Configure Selected**
4. `~\.cursor\mcp.json` 에 `unityMCP` (http://127.0.0.1:8080) 등록됨
5. **Toggle MCP Window** → **Start Server** → 터미널에 `Downloaded cryptography` 등 표시 (Python 보안 라이브러리 **첫 1회** 다운로드, 정상)

### 6. 그밖에 논의·정리해 둔 것

- **WSL/Docker** RAM 사용 → Unity 작업 시 끄면 좋음 (자세한 내용 아래 참고)
- **Wallpaper Engine** → Unity 포커스 시 일시정지 설정 OK
- **데드 셀 vs 할로우나이트** → 둘 다 **플레이는 2D**. 데드 셀만 아트를 3D로 만든 뒤 2D 스프라이트로 변환
- **Unity AI vs Cursor** → 코드는 Cursor, 에디터 작업 보조는 Unity AI(선택). Cursor는 Unity **안**에서 돌아가지 않고 **외부 에디터**

---

## 개발 환경 (노트북 사양)

| 항목 | 사양 |
|------|------|
| 노트북 | Samsung 750XGK |
| CPU | Intel Core i5-1335U (10코어/12스레드) |
| GPU | Intel Iris Xe (내장) |
| RAM | 16GB LPDDR5X |
| 저장장치 | 256GB NVMe SSD |
| OS | Windows 11 Home |

Unity 2D URP 개발 가능. GPU·RAM이 병목이므로 작업 전 불필요한 백그라운드 앱 정리 권장.

---

## 용어 간단 정리

| 용어 | 뜻 |
|------|-----|
| **Unity Hub** | Unity 설치·프로젝트 목록 관리 |
| **Unity Editor** | 게임 만드는 본 프로그램 |
| **Cursor** | 코드 짜는 에디터 + AI |
| **MCP** | Cursor와 Unity를 연결하는 통로 |
| **URP** | 2D/3D 모두 쓸 수 있는 가벼운 그래픽 설정 |
| **C# 스크립트** | 캐릭터 이동·점프 등 **동작**을 적는 코드 파일 (`.cs`) |

---

## WSL 메모리 사용 (보류)

Windows에서 `vmmemWSL` 프로세스가 RAM을 사용하는 것은 WSL2의 정상 동작입니다.

- WSL2는 Hyper-V 위의 가벼운 VM으로 Linux를 실행합니다.
- Ubuntu + Docker Desktop WSL이 동시에 Running이면 RAM을 더 씁니다.
- `.wslconfig` 없으면 시스템 RAM의 약 50%까지 자동 할당 가능합니다.

### 줄이는 방법

```powershell
# WSL/Docker 완전 종료
wsl --shutdown
```

`C:\Users\<사용자>\.wslconfig` 생성:

```ini
[wsl2]
memory=4GB
swap=2GB
```

저장 후 `wsl --shutdown` 실행.

Unity 작업 시 Docker Desktop·WSL을 쓰지 않으면 종료하는 것을 권장합니다.
