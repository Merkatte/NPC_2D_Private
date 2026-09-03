# Inventory and Items

## 기능 목적

아이템 원본 데이터의 적재, 시설과 NPC의 runtime 수량 저장, 그리고 둘 사이의 운반·입고 transaction 경계를 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| Item data loading | CSV를 row로 파싱하고 `ItemInfo` table로 변환 |
| Runtime inventory | item ID별 현재 수량 저장(시설·NPC 봇짐)과 두 저장소 사이의 입고 transaction |
| Cost lookup | `ActionType`별 action cost asset 제공 |

`IInventory.TryAdd` 하나로 시설과 봇짐이 같은 수량 계약을 공유하므로 운반·입고는 별도 세부 기능이 아니라 Runtime inventory의 실행 경로다. 독립적으로 변경되는 세부 기능이 4개가 되면 폴더형 인덱스로 분할한다.

## 현재 실행 흐름

```text
ItemData.csv -> CSVParser -> ItemInfoCsvMapper -> ItemDataContext
selector -> IDataManager.TryGetActionCostInfo<T>(ActionType) -> cost asset

수확물 운반:
FarmWorkSite(Harvest) -> InteractionRequest.Cargo.TryAdd(outputItemId, pending yield)
  -> WorkerInventory (NPCComponent가 소유, 용량 제한, 단일 item type)
  -> 수락량만큼만 pending yield 차감

창고 입고:
DepositAction -> WarehouseDepositPoint.TryInteract(Deposit, cargo)
  -> ICarriedInventory.TryTransferAllTo(WarehouseInventory)
  -> 창고가 거부한 잔량은 계속 봇짐에 남는다
```

`IInventory.TryAdd`는 요청 수량 중 수락 가능한 만큼만 받고, 1개 이상 수락되면 true와 함께 `acceptedQuantity`를 돌려주는 부분 수락 계약이다. `WarehouseInventory`는 현재 용량 제한이 없어 실질적으로 항상 전량을 수락하고, `WorkerInventory`는 남은 용량까지만 수락한다.

`DepositAction`은 1초 동안 서 있기만 하고 stat 비용이 없으며, `BaseWorkingAction`이 아니라 `DefaultAction`을 상속한다 — Tool layer의 작업 모션이 `IsWorking`으로 게이트되어 있어 창고에 짐을 내려놓으며 호미를 휘두르는 표현을 피하기 위함이다. 도착 시 봇짐이 이미 비어 있으면 실패가 아니라 정상 완료로 처리하고, 창고가 거부하면 `RequestReplan`한다.

`WorkerInventory`는 MonoBehaviour가 아닌 plain C#이며 `NPCComponent`가 `CombatRuntimeState`와 같은 방식으로 인라인 소유한다. 한 번에 한 item type만 담고, 표시 여부 판단(비었는지 여부)은 자신이 하되 실제 `SpriteRenderer` 조작은 생성자로 받은 콜백을 통해 `NPCComponent`에 위임한다. `Clear()`는 `ICarriedInventory`에 없고 소유자인 `NPCComponent`만 pool 재사용 시 호출한다 — 외부 provider가 남의 봇짐을 비울 권한은 없다.

`ItemData.csv`는 농사 crop 결과 item(현재 Carrot=4, Potato=5, Food category)도 다른 item과 동일한 9열 계약으로 보유한다. `ItemDataContext.TryGetItemInfo(id, out info)`가 id 기준 조회를 제공하며, `CropCatalog.TryValidate(itemDataContext, out reason)`가 catalog에 등록된 모든 crop의 `OutputItemId`를 이 조회로 cross-check해 CSV에 없는 id를 가진 crop을 거부한다. 이 검증은 catalog를 소비하는 초기화 경계(현재 `TestFarmProductionWindow`)에서만 실행되며, `FarmWorkSite`가 item table 전체를 들고 있지는 않는다 — `TrySelectCrop`을 catalog를 거치지 않고 직접 호출하면 이 cross-check를 우회한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/ItemDataContext.cs` | category별 `ItemInfo` table과 id 기준 단건 조회(`TryGetItemInfo`)를 제공하는 공유 item context |
| `Assets/Data/Struct/ItemInfo.cs` | CSV item row의 ID·category·표시 정보·stat effect 값 |
| `Assets/Scripts/Actor/WarehouseDepositPoint.cs` | 운반 cargo를 창고 `IInventory`로 옮기는 입고 provider |
| `Assets/Scripts/Enum/ItemCategory.cs` | item table 분류 key |
| `Assets/Scripts/Interface/ICarriedInventory.cs` | 운반 중 inventory의 empty·full 상태와 전량 이관 계약 |
| `Assets/Scripts/Interface/IDataManager.cs` | action cost를 type-safe하게 조회하는 서비스 계약 |
| `Assets/Scripts/Interface/IInventory.cs` | item 수량의 부분 수락 계약과 조회 계약 |
| `Assets/Scripts/Manager/DataManager.cs` | `ActionType`별 `DefaultActionCost` registry |
| `Assets/Scripts/System/Action/DepositAction.cs` | 창고 입고 interaction 1회 실행 |
| `Assets/Scripts/System/Inventory/WarehouseInventory.cs` | item ID별 runtime 정수 수량 저장소 |
| `Assets/Scripts/System/Inventory/WorkerInventory.cs` | NPC 1명의 단일 item type 운반 cargo와 표시 콜백 |
| `Assets/Scripts/System/Lib/CSVParser.cs` | `TextAsset` CSV를 문자열 row로 파싱 |
| `Assets/Scripts/System/Mapper/ItemInfoCsvMapper.cs` | CSV row를 category별 `ItemInfo` dictionary로 변환 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| item CSV column·mapping | `CSVParser.cs`, `ItemInfoCsvMapper.cs`, `ItemInfo.cs` |
| item category·효과 | `ItemCategory.cs`, `ItemInfo.cs`, `ItemDataContext.cs` |
| 창고 입고·용량 | `IInventory.cs`, `WarehouseInventory.cs`, `WarehouseDepositPoint.cs` |
| NPC 봇짐 용량·item type | `WorkerInventory.cs`, `ICarriedInventory.cs`, [NPC Presentation](NPC_Presentation.md) |
| 운반·입고 queue 순서 | `DepositAction.cs`, [Selector and Queue](NPC_Decision_and_Actions/Selector_and_Queue.md) |
| cargo를 나르는 request | `InteractionRequest.cs`, [Interaction and Destinations](Interaction_and_Destinations.md) |
| action cost 조회 | `IDataManager.cs`, `DataManager.cs`, [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md) |
| 농장 수확물 생산 | [Farming](Farming/README.md) |
| item id 조회·cross-validation | `ItemDataContext.cs`, [Farming Definition](Farming/Definition_and_Catalog.md)의 `CropCatalog.TryValidate` |

## 불변 규칙

- 현재 수량은 scene runtime inventory 또는 NPC 소유 봇짐이 가지고 ScriptableObject에 쓰지 않는다.
- `TryAdd`의 `acceptedQuantity`와 bool을 호출자가 모두 확인한다. 부분 수락은 성공이며 전량 수락을 전제로 계산하지 않는다.
- 외부 transaction 성공 전에 생산자의 내부 상태를 소모하지 않는다.
- 목적지가 거부한 수량은 계속 운반 상태로 남아 생산물이 소실되지 않는다.
- 봇짐은 한 번에 한 item type만 담고, 다른 item type의 입고는 수락 0으로 거부한다.
- 봇짐을 비우는 권한은 소유자인 `NPCComponent`에만 있다. provider는 `ICarriedInventory` 계약 밖의 조작을 하지 않는다.
- `WarehouseInventory`는 순수 저장소로 남기고 interaction protocol은 `WarehouseDepositPoint`가 담당한다.
- item ID와 serialized enum 변경은 기존 CSV·asset 호환성을 함께 검토한다.

## Unity 배선

`DataManager._costInfos`에는 같은 `ActionType`의 cost가 중복되지 않아야 한다. `WarehouseDepositPoint._inventorySource`는 `IInventory`를 구현한 `MonoBehaviour`(현재 같은 오브젝트의 `WarehouseInventory`)여야 하며, 이 provider도 `InteractableManager._interactables`와 `DestinationDB`의 `BuildingType.Warehouse` row에 함께 등록해야 Farmer가 입고 목적지를 찾는다. `NPCComponent._cargoCapacity`(기본 10)와 `_carryRenderer`는 NPC prefab에서 설정한다.

`FarmerTest.unity` 배선은 완료됐고 `GuardTest.unity`는 아직 같은 배선을 하지 않았다(2026-09-04 기준).

## 알려진 제약과 TBD

- 창고 용량, 출고, 예약, 저장 persistence는 아직 없다. 따라서 창고 쪽 입고 거부 경로는 Play Mode에서 재현할 수 없고, 부분 수락은 봇짐이 가득 찬 경우로만 관측된다.
- `NPCComponent.ResetRuntimeState`가 봇짐을 비우므로 despawn 경로가 생기면 운반 중이던 생산물이 조용히 사라진다. 현재 despawn 경로가 없어 관측되지 않는다.
- 여러 item type 동시 운반과 item별 봇짐 스프라이트는 지원하지 않는다.
- `DataManager.instance`는 현재 static service reference이며 수명 정책이 단순하다.

## 관련 문서

- [Farming](Farming/README.md)
- [Interaction and Destinations](Interaction_and_Destinations.md)
- [Selector and Queue](NPC_Decision_and_Actions/Selector_and_Queue.md)
- [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md)

## 문서 갱신 조건

item schema, CSV mapping, inventory transaction, 운반·입고 흐름, cost registry가 바뀌면 갱신한다.
