# Settlement

정착지의 건설, 주거와 주민 모집을 연결한다. 공유 자원 저장과 NPC 생성 공통 수명은 외부 기능이 소유한다.

| 변경 대상 | 하위 기능 |
|---|---|
| 건물 정의·부지·예약·건축가 작업·완공·건설 UI | [Construction](Construction/README.md) |
| 주택 정의·입주 배정·집 생활·업그레이드 표시 | [Housing](Housing/README.md) |
| 시청 후보·모집 비용·예약 스폰·낙하와 모집 UI | [Town Hall](Town_Hall.md) |

시청은 [NPC 생성](../NPC/Runtime/Spawning_and_Pooling.md)에 예약을 요청하고, 커밋된 주민은 주거 배정 대상이 된다. 건설은 부지의 공사 transaction을 완료한 뒤 시설을 등록하며 주택은 등록된 시설의 입주·생활 상태를 소유한다. 비용은 [공유 자원](../Economy/Inventory_and_Items/Shared_Resources.md), 목적지 등록은 [World](../World/README.md)의 계약을 사용한다.

세 기능의 책임이나 연결이 바뀌면 이 인덱스를 갱신한다.
