# NPC Runtime

## 기능 목적

`WorkerNPC`가 한 NPC의 action queue를 실행하고, `NPCStat`이 개체별 mutable 상태를 보관하는 공통 런타임을 설명한다. 무엇을 할지 결정하는 정책과 구체 action 구현은 이 문서의 책임이 아니다.

## 책임 경계

- `WorkerNPC`: 초기화, 현재 action, 대기 queue, 결과 처리, 취소와 pool 반환을 소유한다.
- `NPCStat`: 체력·이동 속도·욕구의 현재값을 소유한다.
- stat definition: 공유 초기값에서 매번 새 runtime stat을 만든다.
- selector는 queue를 구성하고 action은 실행한다. `WorkerNPC`는 우선순위나 도메인 규칙을 판단하지 않는다.
- Unity 이동·애니메이션·방향 표현은 [NPC Presentation](NPC_Presentation.md)이 소유한다.

## 현재 실행 흐름

```text
WorkerNPC.Init(role, runtime stat, selector)
  -> NPCComponent.Init(stat)
  -> selector.RequestNewActionQueue(...)
  -> action.Start()
  -> action.Tick()
     -> Completed: 현재 action 반환 후 다음 action
     -> ReplanRequested/Failed: 현재 action과 남은 queue를 모두 반환 후 재판단
  -> OnDisable: queue와 actor-local runtime state 정리
```

`WorkerNPC`는 비어 있는 queue를 selector에게 다시 요청한다. `Failed`와 `ReplanRequested`는 의미는 다르지만 둘 다 남은 예측 queue를 폐기한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs` | 기본 NPC runtime stat을 생성하는 공유 definition |
| `Assets/Data/ScriptableObject/Script/NPCStatDefinition.cs` | role별 stat definition의 공통 factory 계약 |
| `Assets/Data/Struct/StatEffect.cs` | 체력·욕구 변화량을 전달하는 immutable 효과 값 |
| `Assets/Scripts/Actor/WorkerNPC.cs` | NPC action queue lifecycle의 단일 실행 owner |
| `Assets/Scripts/Interface/IStatView.cs` | 판단과 실행이 읽는 공통 stat view 계약 |
| `Assets/Scripts/System/Actor/NPCStat.cs` | 한 NPC의 mutable 체력·이동·욕구 상태 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| queue 결과 처리·취소 | `WorkerNPC.cs`, [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md) |
| 공통 stat 필드·증감 | `NPCStat.cs`, `IStatView.cs`, `StatEffect.cs` |
| NPC 초기 stat | `NPCStatDefinition.cs`, `DefaultStatContext.cs`, 생성하는 role 문서 |
| pool 재사용 초기화 | `WorkerNPC.cs`, [Spawning and Pooling](Spawning_and_Pooling.md), [NPC Presentation](NPC_Presentation.md) |

## 불변 규칙

- runtime stat은 NPC 인스턴스마다 새로 생성하며 ScriptableObject에 현재값을 저장하지 않는다.
- action instance를 반환하기 전에 `Stop()`과 selector의 반환 경로를 거친다.
- 비활성화된 pooled NPC에는 이전 queue, stat, target, animation state가 남지 않아야 한다.
- `WorkerNPC`에 role별 우선순위, destination 조회, action 세부 로직을 추가하지 않는다.

## Unity 배선

공유 NPC prefab에는 `WorkerNPC`와 `NPCComponent`가 연결된다. `WorkerNPC._component`는 필수이며 selector와 stat definition은 `NPCManager`의 생성 entry에서 role별로 조립된다.

## 알려진 제약과 TBD

- 현재 active worker 목록의 despawn·제거 lifecycle은 별도 정책이 없다.
- need의 장기 성장·밸런스 규칙은 아직 프로토타입 수준이다.

## 관련 문서

- [판단과 Action 인덱스](NPC_Decision_and_Actions/README.md)
- [NPC Presentation](NPC_Presentation.md)
- [Spawning and Pooling](Spawning_and_Pooling.md)

## 문서 갱신 조건

`WorkerNPC` lifecycle, 공통 stat 모델, action 결과 소비 방식이 바뀌면 이 문서를 갱신한다.
