# Movement

## 기능 목적

고정 위치 또는 움직이는 target을 추적해 NPC를 이동시키고 stopping distance에서 완료하는 action 경계를 설명한다.

## 현재 실행 흐름

```text
selector
  -> MoveRequest.Fixed(position, distance)
     또는 MoveRequest.Dynamic(IMoveTarget, distance)
  -> ActionContext.MoveRequest
  -> MoveAction.Start(): target 검증과 즉시 도착 판정
  -> Tick(): 현재 target 위치 재조회, 방향·Flip·Move
  -> stopping distance 도달 시 Completed
  -> dynamic target 소실 시 ReplanRequested
```

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/MoveRequest.cs` | 고정·동적 목표와 stopping distance를 표현하는 이동 요청 값 |
| `Assets/Scripts/Interface/IMoveTarget.cs` | 현재 위치를 재조회할 수 있는 동적 이동 target 계약 |
| `Assets/Scripts/System/Action/MoveAction.cs` | 목표 추적, 방향, stopping distance와 이동 완료 실행 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| 고정 목적지 이동 | `MoveAction.cs`, `MoveRequest.cs`, queue를 구성하는 selector |
| 움직이는 전투 target | `MoveAction.cs`, `IMoveTarget.cs`, [Targeting and Perception](../Combat/Targeting_and_Perception.md) |
| 실제 이동·Flip 표현 | `MoveAction.cs`, [NPC Presentation](../NPC_Presentation.md) |
| stopping distance | `MoveRequest.cs`, 생성 selector, range 규칙을 소유한 기능 문서 |

## 불변 규칙

- selector는 이동 필요 여부를 복잡하게 예측하지 않고 `MoveAction.Start()`의 즉시 완료를 활용할 수 있다.
- 동적 target은 매 tick 현재 위치를 제공하며 소실을 성공으로 가장하지 않는다.
- 실제 transform 이동과 Animator speed 갱신은 `NPCComponent.Move()`를 통과한다.
- 2D gameplay 거리 판정은 Z축을 제외하는 관련 시스템 규칙과 일치시킨다.

## 관련 문서

- [Action Runtime](Action_Runtime.md)
- [Selector and Queue](Selector_and_Queue.md)
- [NPC Presentation](../NPC_Presentation.md)
- [Combat Attack Runtime](../Combat/Attack_Runtime.md)

## 문서 갱신 조건

이동 request, target 계약, stopping distance, target 소실 또는 이동 완료 규칙이 바뀌면 갱신한다.
