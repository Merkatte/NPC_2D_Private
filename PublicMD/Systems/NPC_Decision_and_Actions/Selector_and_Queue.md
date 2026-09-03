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
     -> 창고 provider가 없으면 1회만 오류 로그 후 Idle로 대기
  -> 그 외: DestinationDecider.Decide
     -> Work이면 같은 provider에서 batch 작업 위치를 한 번 결정
     -> 필요한 경우 MoveAction
     -> 같은 위치를 공유하는 Farming batch 또는 Eat/Drink/Sleep/Idle action
  -> 모든 action Init 완료 후 queue 반환
```

Farmer 우선순위는 긴급 욕구 > Harvest(봇짐에 여유가 있을 때만) > Deposit(봇짐이 있을 때만) > 기존 decider 결과다. Harvest를 selector 단계에서 `!Cargo.IsFull`로 먼저 게이트하기 때문에 창고가 없거나 봇짐이 가득 차도 매 프레임 replan이 반복되지 않는다.

Harvest는 queue당 1회만 대여한다. 봇짐 용량 경계는 시도할 때마다 재판단으로 처리되므로 미리 여러 개를 대여할 이득이 없다.

`decision.Intent == Work`인데 작업 위치를 얻지 못한 경우, `BuildingType.Farm`이 `DestinationDB`에 아예 등록되지 않았을 때만 오류로 기록한다. 등록은 되어 있지만 지금 상호작용할 수 없는 상태(빈 밭, 이미 Harvesting phase)는 정상이므로 조용히 Idle로 대체한다 — `DestinationDecider`의 Farmer work 후보 생성은 destination 등록 여부만 보고 `CanInteract`를 확인하지 않기 때문이다.

중간 대여나 초기화가 실패하면 이미 대여한 action을 모두 pool에 반환하고 null이 섞인 queue를 반환하지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs` | action 대여·초기화·반환과 기본 Idle queue 골격 |
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

## 불변 규칙

- `TryRentAction`으로 초기화된 action만 queue에 넣는다.
- queue 구성 실패 시 대여 목록 전체를 반환한다.
- selector 안에서 거리·욕구 utility 공식을 복제하지 않는다. 긴급 욕구 판단은 `DestinationDecider.HasCriticalNeed`를 호출해 decider의 기존 임계 규칙을 그대로 재사용한다.
- `DestinationDecider`는 등록된 농장 중심으로 Work utility를 계산하고, 분산 위치 난수는 Work 선택 뒤 selector의 queue 구성에서만 소비한다.
- 위치 난수를 소비하는 `TryGetActionPosition`은 실제로 queue를 만들기로 확정한 분기에서만 호출한다.
- 물류 분기도 Farming과 동일하게 destination 조회 → provider 조회 → action 위치 조회 순서를 거치고, action에는 확정된 위치만 넘긴다.
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
