# UI

공통 UI 입력·표시 수명, 구체 표시, 공통 시각 규칙, 지역화와 테스트 카메라 조작을 안내한다. 거래·모집·주거·건설의 전용 UI는 각 domain 문서가 소유한다.

| 변경 대상 | 하위 기능 |
|---|---|
| world pointer 입력, category registry, popup·hover lifecycle | [Framework](Framework/README.md) |
| 씨앗 선택 창과 골드 HUD | [Views](Views/README.md) |
| 공통 팝업 스타일·prefab 계층·WarmPixel·헤더 아이콘 | [Presentation](Presentation.md) |
| CSV 문구·key 생성, 언어별 조회, Text/TMP 표시 | [Localization](Localization.md) |
| 테스트 씬 방향키 이동·휠 줌·UI 위 입력 차단 | [Camera Control](Camera_Control.md) |

world 입력 → source 계약 → UI service → view 바인딩·표시 순서다. view는 domain의 조회와 명령을 사용하고 자원·주민·시설 상태를 직접 변경하지 않는다. 문구 표시는 지역화 조회를 사용하며 카메라 입력은 테스트용 독립 경로다.

상단과 창고는 [Economy](../Economy/README.md), 시청·건설·주택은 [Settlement](../Settlement/README.md), 방어 HUD는 [Defense](../Combat/Defense/README.md)를 따른다. 새로운 표시 기능은 가장 가까운 하위 기능에 배치하며 하위 책임·연결이 바뀌면 이 인덱스를 갱신한다.
