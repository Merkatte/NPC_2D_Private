# Construction Definitions

## 책임과 흐름

건물 정보·비용·작업 CSV를 검증한 불변 C# 정의로 변환한다. numeric 원본은 CSV, prefab/icon 참조는 BuildingDataContext SO의 ID 연결이다. DataManager는 조회 facade이며 runtime 공사·재고를 보관하지 않는다. 실제 검증 상태는 PROGRESS를 따른다.

`CSVParser → BuildingDefinitionCsvMapper / BuilderWorkCsvMapper → BuildingDataContext → IDataManager/DataManager → consumer`.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Data/ScriptableObject/Script/BuildingDefinition.cs | 건물 ID/기능/표시명/작업량/정원/용량과 복사된 읽기 전용 비용 |
| Assets/Data/ScriptableObject/Script/BuilderWorkDefinition.cs | 파싱된 공유 작업 속도·욕구 증가 수치 |
| Assets/Data/ScriptableObject/Script/BuildingDataContext.cs | CSV/에셋 참조와 검증 성공한 전체 정의 캐시 |
| Assets/Scripts/System/Mapper/BuildingDefinitionCsvMapper.cs | 건물/비용 행 결합과 참조·수치 검증 |
| Assets/Scripts/System/Mapper/BuilderWorkCsvMapper.cs | 작업 설정 행 검증 |

## 최소 확인 범위와 불변 규칙

- schema/불변성 변경은 위 파일과 승인 계획의 데이터 표를 읽는다. 공통 CSV/아이템 schema는 Inventory and Items로 이동한다.
- 초기 일괄 적재가 전부 성공한 뒤 공개한다. 부분 행 성공이나 실패 후 일부 목록 공개는 없다.
- 공개 setter와 내부 가변 컬렉션 참조를 노출하지 않는다. 원본 비용을 바꿔도 이미 생성한 정의와 지불 사본이 바뀌지 않는다.
- buildingId는 정의 ID, BuildingType은 기능, itemId는 자원이다. 서로 다른 식별 대상을 혼동하지 않는다.
- NaN/Infinity/비양수 작업량·정원, 중복 ID/비용 pair, 알 수 없는 아이템, 에셋 연결 누락은 오류다. 정상 gameplay 상태는 정의 오류가 아니다.

## 배선과 검증

BuildingDataContext에 세 CSV, ItemDataContext, 다섯 ID의 시설 prefab/icon을 연결한다. DataManager 초기화에서 검증한다. CSV 수치의 Inspector 복제 원본이나 Addressables는 추가하지 않는다. 오류·행/ID 진단, 불변 비용 사본, 전체 실패가 비-Play 검사 대상이다. schema나 소유 타입이 바뀌면 이 문서를 갱신한다.
