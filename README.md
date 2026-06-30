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
- [CHANGELOG.md](CHANGELOG.md) — 작업·push 일지 (월별)

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
