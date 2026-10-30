# Economy

생산, 자원 보관·물류와 상단 거래를 연결한다. 건설·모집 등 자원을 소비하는 기능의 규칙은 각 domain이 소유한다.

| 변경 대상 | 하위 기능 |
|---|---|
| 씨앗·작물 정의, 심기·성장·수확, 농사 action과 작물 표현 | [Farming](Farming/README.md) |
| 아이템 정의, 골드를 포함한 공유 자원, NPC 운반·입고, 창고 UI | [Inventory and Items](Inventory_and_Items/README.md) |
| 상단 방문·체류·퇴장, 판매 transaction과 거래 UI | [Merchant Caravan](Merchant_Caravan.md) |

농장은 생산물을 요청 cargo에 넘기고, 물류는 cargo를 창고에 입고한다. 상단은 판매 의도를 검증한 뒤 공유 자원 API로 물품과 골드를 원자적으로 교환한다. 수량·용량 저장은 공유 자원, 비용·가격·생산 규칙은 각 domain이 소유한다.

[NPC 판단](../NPC/Decision_and_Actions/README.md)은 작업을 선택하고 [UI](../UI/README.md)는 상태와 요청을 전달한다. 하위 기능과 transaction 경계가 바뀌면 이 인덱스를 갱신한다.
