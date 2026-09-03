# Interaction and Destinations

## 기능 목적

NPC가 목적지를 찾고, 그 장소가 제공하는 capability를 조회하며, 명시적 request로 상호작용하는 공통 protocol을 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| Destination registry | `BuildingType`을 위치와 destination object로 변환 |
| Provider registry | destination object와 `ActionType`으로 provider 조회 |
| Interaction protocol | 지원 여부, action 위치, option 열거, request 실행, result 반환 |

세부 기능이 4개 미만이므로 현재는 단일 문서로 유지한다.

## 현재 실행 흐름

```text
DestinationDB.TryGetDestinationPos(BuildingType)

DestinationDB.TryGetInteractionProvider(BuildingType, ActionType)
  -> destination row의 GameObject
  -> InteractableManager.TryGetInteractionProvider(GameObject, ActionType)
  -> IInteractionProvider

선택된 action의 실행 위치:
  provider.TryGetActionPosition(ActionType, registered destination)
  -> 위치 제공 facility는 action별 위치 반환
  -> 기본 provider는 registered destination 반환

공급 action:
  provider.AppendOptions()
  -> selector가 InteractionRequest 선택
  -> ActionContext(provider, request)
  -> provider.TryInteract()
  -> InteractionResult의 StatEffect 적용

item을 옮기는 action:
  selector가 InteractionRequest(type, cargo: NPC의 ICarriedInventory) 구성
  -> provider.TryInteract()
  -> 생산 provider(Farm/Harvest)는 cargo에 넣고
     수령 provider(Warehouse/Deposit)는 cargo에서 뺀다
```

`InteractionRequest.Cargo`는 결과가 아니라 요청에 실린다. 이렇게 해야 provider가 "외부 수락을 확인한 뒤 내부 상태를 소모"하는 순서를 지킬 수 있고, `InteractionResult`나 `ActionContext`에 domain 전용 payload 필드를 추가하지 않아도 된다. cargo는 항상 plain C# 객체이므로 일반 null 검사로 판정하며, item을 옮기지 않는 interaction(Eat/Drink/Farming/Sleep)에서는 null이다.

`BaseBuildingAction`은 Eat/Drink/Sleep의 실내 표현을 시작하고 완료·실패·재판단·취소·pool 반환 모든 경로에서 정리한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/InteractionOption.cs` | utility 판단에 제공되는 interaction 후보 값 |
| `Assets/Data/Struct/InteractionRequest.cs` | 선택된 action, option, strength와 transaction에 참여하는 cargo를 전달하는 요청 값 |
| `Assets/Data/Struct/InteractionResult.cs` | provider 실행 결과의 actor 효과 값 |
| `Assets/Scripts/Actor/BaseInteractionProvider.cs` | 공통 초기화·검증·dispatch template을 제공하는 provider base |
| `Assets/Scripts/Actor/Pub.cs` | item table을 이용해 Eat·Drink option과 결과를 제공하는 facility |
| `Assets/Scripts/Enum/BuildingType.cs` | destination registry의 안정적인 건물 key |
| `Assets/Scripts/Interface/IInteractionProvider.cs` | support·availability·action position·option·execute 공통 계약 |
| `Assets/Scripts/Manager/InteractableManager.cs` | scene provider 초기화와 `(GameObject, ActionType)` registry |
| `Assets/Scripts/System/Action/BaseBuildingAction.cs` | 실내 action의 건물 출입 표현 lifecycle |
| `Assets/Scripts/System/Lib/DestinationDB.cs` | 건물 위치 조회와 provider registry 위임 진입점 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 새 destination | `BuildingType.cs`, `DestinationDB.cs`, 대상 scene |
| 새 facility provider | `IInteractionProvider.cs`, `BaseInteractionProvider.cs`, `InteractableManager.cs` |
| item을 옮기는 request | `InteractionRequest.cs`, [Inventory and Items](Inventory_and_Items.md) |
| 창고 입고 | [Inventory and Items](Inventory_and_Items.md)의 `WarehouseDepositPoint` |
| Eat·Drink option | `Pub.cs`, interaction value types, [Needs Actions](NPC_Decision_and_Actions/Needs_Actions.md) |
| 실내 표현 | `BaseBuildingAction.cs`, [NPC Presentation](NPC_Presentation.md) |
| 농장 interaction | [Farming](Farming/README.md) |

## 불변 규칙

- 새 facility마다 domain 전용 provider interface나 `TryGetXxxProvider`를 추가하지 않는다.
- provider는 자신의 domain transaction만 실행하고 NPC의 다음 행동을 결정하지 않는다.
- selector는 semantic action을 선택한 뒤 같은 provider에서 action 위치를 한 번 조회하며, action은 이미 확정된 위치만 받는다. 위치를 따로 계산하지 않는 Deposit 같은 action도 이 조회 경로를 건너뛰지 않는다.
- item payload는 `InteractionRequest.Cargo`로만 전달하고 `ActionContext`나 `InteractionResult`에 domain 전용 필드를 추가하지 않는다.
- provider는 요청자가 들고 온 cargo에만 접근하며 계약 밖의 조작(임의 비우기 등)을 하지 않는다.
- action 위치를 별도로 제공하지 않는 provider는 등록된 destination 위치를 그대로 사용한다.
- action은 scene registry를 직접 검색하지 않고 selector가 주입한 provider와 request를 사용한다.
- provider 초기화는 idempotent해야 하며 중복 `(object, action)` 등록은 첫 항목을 보존하고 오류로 보고한다.
- provider는 고정 scene dependency 부재만 초기화 실패로 취급한다. 정상적으로 바뀔 수 있는 domain 상태(예: 농경지에 아직 아무것도 안 심긴 상태)로 인터랙션을 막을 때는 `CanInteractCore`를 override해서 게이트하고, `TryInitializeCore`를 실패시키지 않는다 — `BaseInteractionProvider._isOperational`은 첫 초기화에서 latch되므로 여기서 실패시키면 이후 상태가 바뀌어도 영구히 복구되지 않는다. 예시: [Farming Runtime](Farming/Runtime_and_Transactions.md)의 `FarmWorkSite.CanInteractCore`.

## Unity 배선

`DestinationDB` row에는 `BuildingType`, 이동 위치, provider가 붙은 destination object를 연결한다. 모든 `BaseInteractionProvider`는 `InteractableManager._interactables`에 등록한다. 현재 등록 대상은 Pub, Well, Farm, Warehouse이며 `BuildingType.Warehouse` row와 `WarehouseDepositPoint`가 함께 있어야 Farmer가 입고 목적지를 찾는다. `FarmerTest.unity`는 배선 완료, `GuardTest.unity`는 2026-09-04 기준 미배선이다.

## 알려진 제약과 TBD

- `SleepAction`은 현재 provider transaction 없이 건물 위치와 시간 기반으로 동작한다.
- destination row의 중복 key는 현재 마지막 유효 row가 dictionary 값을 덮어쓴다.
- `ActionContext.InteractionProvider`는 캐싱된 참조이므로 provider가 파괴되면 감지하지 못한다. 현재 facility provider를 파괴하는 경로가 없어 실제 위험은 없고, `WarehouseDepositPoint`처럼 backing `MonoBehaviour`를 이미 필드로 갖고 있는 경우에만 Unity null 규칙으로 재확인한다.

## 관련 문서

- [Decision Policy](NPC_Decision_and_Actions/Decision_Policy.md)
- [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md)
- [Farming](Farming/README.md)

## 문서 갱신 조건

provider protocol, destination lookup, registry 조립, request/result shape 또는 건물 action lifecycle이 바뀌면 갱신한다.
