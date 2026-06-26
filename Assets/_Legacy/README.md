# Legacy 프로토타입 (v0)

`Assets/_Legacy`에는 2026-06-01~02 세션에서 만든 **실험용 프로토타입**이 보관되어 있습니다.  
새 게임 개발은 `Assets/Game`에서 진행하고, 이 폴더는 **참고·에셋 재사용** 용도로만 사용하세요.

## 포함 내용

| 경로 | 설명 |
|------|------|
| `Scripts/` | 전투, 맵 부트스트랩, 갈고리, NPC, 저장 등 프로토타입 C# |
| `Scenes/Legacy_SampleScene.unity` | 기존 SampleScene (전투/갈고리 테스트) |
| `Resources/` | Gearbot 스프라이트, 배경 (`Resources.Load` 경로) |
| `Art/` | 원본 배경 아트 |
| `Input/` | `InputSystem_Actions.inputactions` |
| `Screenshots/` | MCP 검증 스크린샷 |

## 열어보기

1. Unity에서 `Assets/_Legacy/Scenes/Legacy_SampleScene.unity` 더블클릭
2. Play로 프로토타입 플레이 (맵 플래그는 씬의 `UndergroundMapBootstrap` 인스펙터 참고)

## 컴파일 분리

- `Legacy.asmdef` — `autoReferenced: false`  
  새 `Game` 코드가 실수로 Legacy 스크립트에 의존하지 않도록 분리했습니다.
- Legacy 씬을 열면 Legacy 어셈블리만 해당 컴포넌트에 연결됩니다.

## 재사용 팁

- **스프라이트만 쓸 때:** `Resources/Gearbot` 또는 `Art/`에서 복사해 `Assets/Game/Resources/`로 옮기기
- **코드 참고만:** 클래스 이름 그대로 `Game`에 복붙하지 말고, 필요한 로직만 발췌
- **맵 부트스트랩:** `UndergroundMapBootstrap`은 런타임 동적 생성 방식 — 새 게임에서는 씬/Prefab 기반으로 다시 설계 권장

자세한 프로토타입 기능 목록은 프로젝트 루트의 [README_LEGACY.md](../README_LEGACY.md)를 보세요.
