# 주택 정의와 CSV

주택 단계는 기존 BuildingData/BuildingCost의 buildingId를 참조한다. HousingDataContext는 네 CSV와 BuildingDataContext를 검증한 후 읽기 전용 정의와 생활 설정을 한 번에 공개한다. 공유 SO에 입주자·타이머를 저장하지 않는다.

| 주 소유 코드 | 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/HousingDataContext.cs` | CSV 참조와 단계/다음 단계/buildingId 조회 |
| `Assets/Data/Struct/HouseTierDefinition.cs` | 단계의 buildingId와 완전한 옵션 목록 |
| `Assets/Data/Struct/HousingOption.cs` | optionId, 효과 종류, 값 |
| `Assets/Data/Struct/HousingLifeSettings.cs` | 집·여관 회복과 재판단 간격 |
| `Assets/Scripts/Enum/HousingEffectType.cs` | 안정 효과 ID |
| `Assets/Scripts/System/Mapper/HousingCsvMapper.cs` | CSV 전체 검증과 불변 정의 생성 |

`Assets/Data/CSV/HouseTierData.csv`는 tier/buildingId, `HousingOption.csv`는 optionId/effectType/value, `HouseTierOption.csv`는 tier/optionId, `HousingLife.csv`는 집 피로 회복/여관 피로 회복/집 허기 증가/집 갈증 증가/재판단 초다. effectType은 Capacity=0, DissatisfactionRecovery=1, FatigueRecovery=2, HungerRecovery=3, ThirstRecovery=4다.

단계는 1부터 연속이고 buildingId는 중복 없이 실제 House 정의를 참조한다. 옵션 값은 유한한 0 이상이다. 정원은 양의 정수이며 단계가 오를 때 감소하지 않는다. 단계마다 같은 효과는 한 번만 연결한다. 옵션은 상속하지 않으므로 유지하려는 효과도 다음 단계 목록에 명시한다. 미등록 옵션/단계와 중복 참조는 전체 로드를 거절한다. 비용 아이템 확장은 기존 등록 아이템 ID를 BuildingCost에 추가하며 주택 코드 변경이 필요 없다.

초기 단계 1~4는 buildingId 6~9, 정원 2/2/3/3, 불만 회복 보너스 0/0.25/0.25/0.5, 피로 보너스 0/0/0/2다. 허기·갈증 회복 종류는 지원하되 초기 옵션에 없다. 집 기본 피로 회복 5/초, 여관 10/초, 집 허기·갈증 0.3/초, 재판단 0.5초는 잠정 밸런스다.

변경 유형별 최소 확인 범위: CSV 스키마/검증은 위 여섯 코드와 네 CSV, 건설 비용/그림 연결은 Construction의 정의·완공 leaf와 BuildingDataContext, 실행 효과는 [Life](Life.md)를 읽는다. 수치·필드·enum·조회 계약 변경 시 이 문서를 갱신한다.
