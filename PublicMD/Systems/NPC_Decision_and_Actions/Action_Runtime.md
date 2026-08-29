# Action Runtime

## 기능 목적

모든 action이 따르는 lifecycle, 결과 의미, 실행 dependency 전달과 공통 cost 계약을 설명한다.

## 실행 계약

```text
Init -> Start -> Tick* -> Completed | ReplanRequested | Failed
                         cancellation -> Stop
ReturnAction -> Clear -> pool
```

| 결과 | 의미 |
|---|---|
| `Running` | 아직 실행 중 |
| `Completed` | 선택된 semantic action이 정상 완료됨 |
| `ReplanRequested` | 환경·target·욕구 변화로 현재 queue를 버리고 재판단해야 함 |
| `Failed` | 필수 dependency 또는 transaction 전제가 깨짐 |

## 책임 경계

- `ActionContext`에는 현재 action 실행에 필요한 actor-local 값과 선택된 capability만 넣는다.
- `DefaultAction`은 lifecycle와 결과 전이를 소유하고 concrete action이 반복 구현하지 않게 한다.
- action은 manager, registry, selector를 직접 찾거나 다음 목표를 판단하지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/DefaultActionCost.cs` | action type별 공유 cost definition의 base |
| `Assets/Data/Struct/ActionContext.cs` | action 실행에 필요한 component·stat·cost·provider·request·move 값 |
| `Assets/Scripts/Enum/ActionResult.cs` | Running·Completed·ReplanRequested·Failed 결과 식별 |
| `Assets/Scripts/Enum/ActionType.cs` | pool, cost, interaction이 공유하는 action key |
| `Assets/Scripts/Interface/IAction.cs` | action lifecycle와 결과·type의 공통 계약 |
| `Assets/Scripts/System/Action/DefaultAction.cs` | action lifecycle, pause, stop, clear, 결과 전이 base |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| lifecycle·결과 의미 | `IAction.cs`, `DefaultAction.cs`, `ActionResult.cs`, [NPC Runtime](../NPC_Runtime.md) |
| context field | `ActionContext.cs`, 값을 구성하는 selector, 소비 action |
| 새 action type | `ActionType.cs`, `DefaultActionCost.cs`, [Selector and Queue](Selector_and_Queue.md) |
| pool reset | `DefaultAction.cs`, concrete action `Clear()`, `ActionPool.cs` |

## 불변 규칙

- `Start()`는 필수 context를 한 번 검증하고 `Tick()` hot path에서 scene search나 반복 cast를 하지 않는다.
- side effect는 정상 완료 경로에서 정확히 한 번 발생한다.
- `Stop()`과 `Clear()`는 부분 초기화 상태에서도 안전해야 한다.
- `Clear()`는 context, timer, target, flag를 모두 초기화한다.
- `ActionContext`를 manager/service locator로 확장하지 않는다.

## 관련 문서

- [Selector and Queue](Selector_and_Queue.md)
- [Movement](Movement.md)
- [Needs Actions](Needs_Actions.md)
- [Interaction and Destinations](../Interaction_and_Destinations.md)
- [Combat Attack Runtime](../Combat/Attack_Runtime.md)

## 문서 갱신 조건

action interface, context, result, base lifecycle 또는 cost base가 바뀌면 갱신한다.
