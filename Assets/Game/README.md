# 새 게임 (`Assets/Game`)

여기서 **처음부터 다시** 만드는 본편 게임 코드를 둡니다.

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

## Legacy와의 관계

- `_Legacy` 코드는 **자동 참조되지 않음** (`Legacy.asmdef`)
- 에셋이 필요하면 복사 후 `Game` 쪽 경로로 정리
- 프로토타입 씬: `Assets/_Legacy/Scenes/Legacy_SampleScene.unity`
