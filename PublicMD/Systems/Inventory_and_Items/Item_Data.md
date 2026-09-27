# Item Data

## 책임과 흐름

`ItemData.csv → CSVParser → ItemInfoCsvMapper → ItemDataContext → IDataManager/DataManager`가 적재·조회 경로다. DataManager는 action cost와 건설 정의도 조회하며 현재 수량은 보관하지 않는다. Pub의 기존 직접 ItemDataContext 참조는 유지한다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Data/ScriptableObject/Script/ItemDataContext.cs | category별 item table과 ID 조회 |
| Assets/Data/Struct/ItemInfo.cs | 표시·효과·판매가·저장/표시 여부의 readonly 값 |
| Assets/Scripts/Enum/ItemCategory.cs | 기존 ordinal을 보존하는 category |
| Assets/Scripts/Interface/IDataManager.cs | item·action cost·건설 데이터 조회 계약 |
| Assets/Scripts/Manager/DataManager.cs | 조회 창구와 초기 건설 데이터 검증 |
| Assets/Scripts/System/Lib/CSVParser.cs | 단순 CSV 문자열 행 파싱 |
| Assets/Scripts/System/Mapper/ItemInfoCsvMapper.cs | item 행 검증과 변환 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 확인 |
|---|---|
| CSV 열·ID·category | ItemInfoCsvMapper, ItemInfo, ItemCategory, ItemData.csv, DecisionSmokeItemData.csv |
| 적재·조회 | ItemDataContext, IDataManager, DataManager |
| action cost 등록 | DataManager, 사용하는 action, Action Runtime |
| 건물 데이터 | [Construction Definitions](../Construction/Definitions.md) |

## 계약·배선·검증

기존 10열 뒤에 `usesStorage,showInWarehouse`가 붙는 12열이다. ID 1–7을 유지하고 Gold=8, Wood=9, Stone=10을 사용한다. Gold는 두 flag가 false, 일반 저장 품목은 true다. item ID와 building ID는 별도 공간이다.

DataManager에 ItemDataContext, BuildingDataContext와 중복 없는 action cost 목록을 연결한다. 건설 데이터는 전체 검증 성공 후 공개한다. 공유 SO에 잔액·작업 진행을 기록하지 않는다. CropCatalog 검증은 [Farming Definition](../Farming/Definition_and_Catalog.md)을 따른다.

CSVParser는 quoted comma를 지원하지 않는다. Addressables와 persistence는 범위 밖이다. fixture도 production schema를 따라야 한다. schema·조회 API·cost registry 변경 시 이 leaf를 갱신하고 관련 데이터 검사를 실행한다.
