# Inventory and Items

## 기능 목적

아이템 원본 데이터의 적재와 시설 inventory의 runtime 수량 저장 경계를 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| Item data loading | CSV를 row로 파싱하고 `ItemInfo` table로 변환 |
| Runtime inventory | item ID별 현재 수량과 원자적 입고 |
| Cost lookup | `ActionType`별 action cost asset 제공 |

## 현재 실행 흐름

```text
ItemData.csv -> CSVParser -> ItemInfoCsvMapper -> ItemDataContext
FarmWorkSite -> IInventory.TryAdd(itemId, full yield) -> WarehouseInventory
selector -> IDataManager.TryGetActionCostInfo<T>(ActionType) -> cost asset
```

`WarehouseInventory`는 현재 용량 제한이 없으며 요청 수량 전체를 받거나 실패한다. 생산 품목과 yield는 농사 definition이 소유한다.

`ItemData.csv`는 농사 crop 결과 item(현재 Carrot=4, Potato=5, Food category)도 다른 item과 동일한 9열 계약으로 보유한다. `ItemDataContext.TryGetItemInfo(id, out info)`가 id 기준 조회를 제공하며, `CropCatalog.TryValidate(itemDataContext, out reason)`가 catalog에 등록된 모든 crop의 `OutputItemId`를 이 조회로 cross-check해 CSV에 없는 id를 가진 crop을 거부한다. 이 검증은 catalog를 소비하는 초기화 경계(현재 `TestFarmProductionWindow`)에서만 실행되며, `FarmWorkSite`가 item table 전체를 들고 있지는 않는다 — `TrySelectCrop`을 catalog를 거치지 않고 직접 호출하면 이 cross-check를 우회한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/ItemDataContext.cs` | category별 `ItemInfo` table과 id 기준 단건 조회(`TryGetItemInfo`)를 제공하는 공유 item context |
| `Assets/Data/Struct/ItemInfo.cs` | CSV item row의 ID·category·표시 정보·stat effect 값 |
| `Assets/Scripts/Enum/ItemCategory.cs` | item table 분류 key |
| `Assets/Scripts/Interface/IDataManager.cs` | action cost를 type-safe하게 조회하는 서비스 계약 |
| `Assets/Scripts/Interface/IInventory.cs` | item 수량의 전체 수락 여부와 조회 계약 |
| `Assets/Scripts/Manager/DataManager.cs` | `ActionType`별 `DefaultActionCost` registry |
| `Assets/Scripts/System/Inventory/WarehouseInventory.cs` | item ID별 runtime 정수 수량 저장소 |
| `Assets/Scripts/System/Lib/CSVParser.cs` | `TextAsset` CSV를 문자열 row로 파싱 |
| `Assets/Scripts/System/Mapper/ItemInfoCsvMapper.cs` | CSV row를 category별 `ItemInfo` dictionary로 변환 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| item CSV column·mapping | `CSVParser.cs`, `ItemInfoCsvMapper.cs`, `ItemInfo.cs` |
| item category·효과 | `ItemCategory.cs`, `ItemInfo.cs`, `ItemDataContext.cs` |
| 창고 입고·용량 | `IInventory.cs`, `WarehouseInventory.cs`, 소비 기능 문서 |
| action cost 조회 | `IDataManager.cs`, `DataManager.cs`, [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md) |
| 농장 수확물 | [Farming](Farming/README.md) |
| item id 조회·cross-validation | `ItemDataContext.cs`, [Farming Definition](Farming/Definition_and_Catalog.md)의 `CropCatalog.TryValidate` |

## 불변 규칙

- 현재 수량은 scene runtime inventory가 소유하고 ScriptableObject에 쓰지 않는다.
- `TryAdd`의 `acceptedQuantity`와 bool 계약을 호출자가 모두 확인한다.
- 외부 transaction 성공 전에 생산자의 내부 상태를 소모하지 않는다.
- item ID와 serialized enum 변경은 기존 CSV·asset 호환성을 함께 검토한다.

## Unity 배선

`DataManager._costInfos`에는 같은 `ActionType`의 cost가 중복되지 않아야 한다. `FarmWorkSite._outputInventorySource`는 `IInventory`를 구현한 `MonoBehaviour`여야 한다.

## 알려진 제약과 TBD

- 창고 용량, 출고, 예약, 저장 persistence는 아직 없다.
- `DataManager.instance`는 현재 static service reference이며 수명 정책이 단순하다.

## 관련 문서

- [Farming](Farming/README.md)
- [Interaction and Destinations](Interaction_and_Destinations.md)
- [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md)

## 문서 갱신 조건

item schema, CSV mapping, inventory transaction, cost registry가 바뀌면 갱신한다.
