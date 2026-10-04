# Selector and Queue

## 기능 목적

role 판단 결과를 실행 순서가 있는 `Queue<IAction>`으로 원자적으로 구성하고 action pool과 연결하는 계층을 설명한다.

## 책임 경계

- `BaseNPCActionSelector`: action 대여·초기화·반환과 안전한 fallback 골격을 제공한다.
- role selector: role 우선순위와 semantic decision을 구체 action sequence로 번역한다.
- `ActionPool`: `ActionType`별 instance factory와 재사용 queue를 소유한다.
- selector는 action을 실행하거나 장기 runtime state를 보관하지 않는다.

## 현재 Farmer 흐름

```text
FarmerActionSelector.RequestNewActionQueue
  -> DestinationDecider.HasCriticalNeed 이면 아래 물류 분기를 건너뛴다
  -> 봇짐에 여유가 있고 Farm이 Harvest 가능: Move + Harvest 1회
  -> 봇짐이 비어 있지 않음: Move + Deposit (Warehouse)
     -> 사용 가능한 창고 provider가 없거나 공유 용량이 가득 차면 Idle로 대기
  -> 그 외: DestinationDecider.Decide
     -> Work이면 같은 provider에서 batch 작업 위치를 한 번 결정
     -> 필요한 경우 MoveAction
     -> 같은 위치를 공유하는 Farming batch 또는 Eat/Drink/Sleep/Idle action
  -> 모든 action Init 완료 후 queue 반환
```

Farmer는 태업이면 업무 provider 조회 전에 공통 생활/배회 queue로 전환한다. 태업이 아닐 때 우선순위는 긴급 욕구 > Harvest(봇짐에 여유가 있을 때만) > Deposit(봇짐이 있을 때만) > 기존 decider 결과다. Harvest를 selector 단계에서 `!Cargo.IsFull`로 먼저 게이트하기 때문에 창고가 없거나 봇짐이 가득 차도 매 프레임 replan이 반복되지 않는다.

Harvest는 queue당 1회만 대여한다. 봇짐 용량 경계는 시도할 때마다 재판단으로 처리되므로 미리 여러 개를 대여할 이득이 없다.

`DestinationDecider`는 `CanInteract`를 만족하는 시설 중 등록된 입구까지 가장 가까운 실제 시설을 선택하고, `NPCDecision`에 그 시설과 provider를 보존한다. Work 선택 뒤 해당 provider에서 작업 위치를 한 번 얻는다. 선택 이후 시설 상태가 바뀌어 작업 위치를 얻지 못하면 Idle로 대체한다. 같은 유형의 다른 시설로 다시 조회하여 선택된 시설을 바꾸지 않는다.

중간 대여나 초기화가 실패하면 이미 대여한 action을 모두 pool에 반환하고 null이 섞인 queue를 반환하지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs` | action 대여·초기화·반환과 공통 생활/배회·Idle queue 골격 |
| `Assets/Scripts/System/Actor/FarmerActionSelector.cs` | Farmer decision과 물류 우선순위를 이동·공급·농사·수확·입고 action queue로 변환 |
| `Assets/Scripts/System/Lib/ActionPool.cs` | `ActionType`별 action factory와 reusable instance queue |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| 공통 대여·반환 | `BaseNPCActionSelector.cs`, `ActionPool.cs`, [Action Runtime](Action_Runtime.md) |
| Farmer queue 순서 | `FarmerActionSelector.cs`, [Decision Policy](Decision_Policy.md), 대상 action 문서 |
| 수확·입고 우선순위 | `FarmerActionSelector.cs`, [Inventory and Items](../Inventory_and_Items.md), [Farming Runtime](../Farming/Runtime_and_Transactions.md) |
| 새 action type | `ActionPool.cs`, `ActionType.cs`, 새 action 구현, 사용하는 selector |
| Guard·Enemy queue | [Guard](../Combat/Guard.md), [Enemy](../Combat/Enemy.md) |
| Builder queue·Wander factory | [Builder](../Builder.md), `ActionPool.cs` |

## 불변 규칙

- `TryRentAction`으로 초기화된 action만 queue에 넣는다.
- queue 구성 실패 시 대여 목록 전체를 반환한다.
- selector 안에서 거리·욕구 utility 공식을 복제하지 않는다. 긴급 욕구 판단은 `DestinationDecider.HasCriticalNeed`를 호출해 decider의 기존 임계 규칙을 그대로 재사용한다.
- `DestinationDecider`는 선택된 농장의 등록 입구를 기준으로 Work utility를 계산하고, 분산 위치 난수는 Work 선택 뒤 selector의 queue 구성에서만 소비한다.
- 위치 난수를 소비하는 `TryGetActionPosition`은 실제로 queue를 만들기로 확정한 분기에서만 호출한다.
- 물류 분기는 `DestinationDecider.TrySelectNearest`의 공통 가용성·거리 정책으로 destination/provider를 확정한 뒤 action 위치를 조회하고, action에는 확정된 위치만 넘긴다.
- `CanUseStat`으로 role selector와 runtime stat의 호환성을 spawn 전에 확인한다.
- action pool factory와 `ActionType`은 함께 갱신한다.

## Unity 배선

role selector는 scene object이며 공유 `ActionPool`, `DestinationDB`, `DataManager` 등 필요한 service를 serialized reference로 받는다. prefab은 scene selector를 직접 참조하지 않는다.

## 관련 문서

- [Decision Policy](Decision_Policy.md)
- [Action Runtime](Action_Runtime.md)
- [Movement](Movement.md)
- [Needs Actions](Needs_Actions.md)
- [NPC Runtime](../NPC_Runtime.md)

## 문서 갱신 조건

selector 책임, queue 구성 원자성, action 대여·반환, Farmer 우선순위와 변환 규칙이 바뀌면 갱신한다.

## 시설 이동 모드

FarmerActionSelector와 GuardActionSelector는 `_facilityMoveMode`와 `_navigation`을 명시적으로 받는다.
Navigation 모드의 시설 이동은 Navigated 요청, 기존 Direct 모드는 Fixed 요청이다.
이동 context와 시설 interaction context를 분리하고 provider의 목적지/난수/transaction은 유지한다.
Guard 전투 Dynamic 요청은 Direct다. 경로 서비스 미설정은 한 번 로그 후 Idle로 안전하게 재판단한다.
세부 실행은 Movement, 씬 서비스는 Navigation 문서가 소유한다.

## 불만도·태업 연결

BaseNPCActionSelector.BuildLeisureQueue는 decider가 선택한 Eat/Drink/Sleep을 기존 Move+생활 action으로 조립한다. 생활 선택이 없고 HasCriticalNeed이면 timed Idle, 나머지는 navigation·WanderActionCost·명시적 IRandomSource로 도달 가능한 배회를 만든다. 경로망/비용/난수원 부재는 Idle로 안전하게 처리한다. Farmer/Guard는 `_wanderCost`, `_randomSource`와 기존 `_navigation`을 사용한다. Builder도 이 공통 조립을 재사용한다. Farming/Harvest/Deposit와 해당 이동은 RequiresWorkAvailability=true, 생활/배회는 false다. 배회 실행은 [Needs Actions](Needs_Actions.md)가 소유한다.
