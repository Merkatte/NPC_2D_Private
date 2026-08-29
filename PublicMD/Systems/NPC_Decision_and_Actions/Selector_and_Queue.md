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
  -> DestinationDecider.Decide
  -> destination/provider 유효성 확인
  -> 필요한 경우 MoveAction
  -> Farming/Eat/Drink/Sleep/Idle action
  -> 모든 action Init 완료 후 queue 반환
```

중간 대여나 초기화가 실패하면 이미 대여한 action을 모두 pool에 반환하고 null이 섞인 queue를 반환하지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs` | action 대여·초기화·반환과 기본 Idle queue 골격 |
| `Assets/Scripts/System/Actor/FarmerActionSelector.cs` | Farmer decision을 이동·공급·농사 action queue로 변환 |
| `Assets/Scripts/System/Lib/ActionPool.cs` | `ActionType`별 action factory와 reusable instance queue |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| 공통 대여·반환 | `BaseNPCActionSelector.cs`, `ActionPool.cs`, [Action Runtime](Action_Runtime.md) |
| Farmer queue 순서 | `FarmerActionSelector.cs`, [Decision Policy](Decision_Policy.md), 대상 action 문서 |
| 새 action type | `ActionPool.cs`, `ActionType.cs`, 새 action 구현, 사용하는 selector |
| Guard·Enemy queue | [Guard](../Combat/Guard.md), [Enemy](../Combat/Enemy.md) |

## 불변 규칙

- `TryRentAction`으로 초기화된 action만 queue에 넣는다.
- queue 구성 실패 시 대여 목록 전체를 반환한다.
- selector 안에서 거리·욕구 utility 공식을 복제하지 않는다.
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

selector 책임, queue 구성 원자성, action 대여·반환, Farmer 변환 규칙이 바뀌면 갱신한다.
