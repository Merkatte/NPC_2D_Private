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

- `ActionContext`에는 현재 action 실행에 필요한 actor-local 값과 선택된 capability만 넣는다. 아이템 payload는 별도 필드가 아니라 `Request`가 들고 있는 cargo로 전달한다.
- `DefaultAction`은 lifecycle와 결과 전이를 소유하고 concrete action이 반복 구현하지 않게 한다.
- `BaseWorkingAction`은 작업 표현(`SetWorking`) 배관과 실행 중 provider 재확인을 소유한다.
- action은 manager, registry, selector를 직접 찾거나 다음 목표를 판단하지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/DefaultActionCost.cs` | action type별 공유 cost definition의 base |
| `Assets/Data/Struct/ActionContext.cs` | action 실행에 필요한 component·stat·cost·provider·request·move 값 |
| `Assets/Scripts/Enum/ActionResult.cs` | Running·Completed·ReplanRequested·Failed 결과 식별 |
| `Assets/Scripts/Enum/ActionType.cs` | pool, cost, interaction이 공유하는 action key |
| `Assets/Scripts/Interface/IAction.cs` | action lifecycle와 결과·type의 공통 계약 |
| `Assets/Scripts/System/Action/BaseWorkingAction.cs` | 작업 표현 lifecycle와 캐싱된 provider 재확인(`ProviderStillUsable`)을 제공하는 작업 action base |
| `Assets/Scripts/System/Action/DefaultAction.cs` | action lifecycle, pause, stop, clear, 결과 전이 base |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| lifecycle·결과 의미 | `IAction.cs`, `DefaultAction.cs`, `ActionResult.cs`, [NPC Runtime](../NPC_Runtime.md) |
| 작업 표현·provider 재확인 | `BaseWorkingAction.cs`, [NPC Presentation](../NPC_Presentation.md) |
| context field | `ActionContext.cs`, 값을 구성하는 selector, 소비 action |
| 새 action type | `ActionType.cs`, `DefaultActionCost.cs`, `ActionPool.cs`, [Selector and Queue](Selector_and_Queue.md) |
| pool reset | `DefaultAction.cs`, concrete action `Clear()`, `ActionPool.cs` |

## 불변 규칙

- `Start()`는 필수 context를 한 번 검증하고 `Tick()` hot path에서 scene search나 반복 cast를 하지 않는다.
- side effect는 정상 완료 경로에서 정확히 한 번 발생한다.
- `Stop()`과 `Clear()`는 부분 초기화 상태에서도 안전해야 한다.
- `Clear()`는 context, timer, target, flag를 모두 초기화한다.
- `ActionContext`를 manager/service locator로 확장하거나 domain 전용 payload 필드를 추가하지 않는다.
- 정상적으로 바뀔 수 있는 환경 상태는 `ReplanRequested`, 필수 dependency나 배선 전제가 깨진 경우만 `Failed`로 보고한다.
- `Tick()`에서 하는 provider 재확인은 캐싱된 참조 호출만 사용한다.

## 관련 문서

- [Selector and Queue](Selector_and_Queue.md)
- [Movement](Movement.md)
- [Needs Actions](Needs_Actions.md)
- [Interaction and Destinations](../Interaction_and_Destinations.md)
- [Combat Attack Runtime](../Combat/Attack_Runtime.md)

## 문서 갱신 조건

action interface, context, result, base lifecycle 또는 cost base가 바뀌면 갱신한다.

## 경로 조회 의존 전달

ActionContext에 선택적인 `INavigationService Navigation`이 추가됐다.
실제 경로 상태는 context나 WorkerNPC가 아니라 MoveAction/GuardAction의 NPCPathFollower가 소유한다.
목적지와 MoveRequest의 의미는 Movement 문서를 따른다.

## 불만도·태업 연결

DefaultAction의 protected IsWorkAvailable 조회는 기본 태업 제한을 유지한다. BuildAction만 [Builder](../Builder.md)의 주택 신축 예외를 같은 flag 아래에서 재검사한다. 공통 lifecycle은 역할/부지 규칙을 직접 알지 않으며 다른 업무 action의 동작은 유지한다.

ActionContext.RequiresWorkAvailability는 selector가 업무와 업무 이동에 설정한다(기본 false로 기존 호출 호환). DefaultAction.Start의 ReplanIfWorkUnavailable와 Move/Farming/Harvest/Deposit/Build/Guard/Attack Tick의 같은 검사는 태업이면 효과 이전에 ReplanRequested를 반환한다. 시작 시 도구·이동·예약 작업을 먼저 실행하지 않는다. 기존 Stop/Clear와 action별 종료 정리는 경로·도구·건설 lease를 반환하며 미완료 작업을 성공으로 처리하지 않는다. 확정된 생산물·cargo·공사 진행률은 보존한다. Enemy Attack과 생활 이동은 work flag를 설정하지 않는다.

주거 연결은 ActionType 끝에 HomeStay를 추가하고 ActionPool factory를 등록한다. HomeStay의 별도 Init overload로 ResidentHousingState를 전달하며 ActionContext의 단일 provider 계약과 필드는 그대로 유지한다. 구체 실행은 [Housing Life](../Housing/Life.md)가 소유한다.
