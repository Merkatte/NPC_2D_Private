# Inventory and Items

아이템 정의, 공유 자원, NPC 운반, 창고 표시를 나누어 소유한다. 현재 구조를 기록하며 검증 상태는 PROGRESS를 따른다.

| 변경 대상 | 읽을 leaf |
|---|---|
| CSV·category·데이터 조회·cost registry | [Item Data](Item_Data.md) |
| 공유 수량·용량·지출·환급·교환·알림 | [Shared Resources](Shared_Resources.md) |
| NPC 봇짐·부분 입고·창고 용량 제공 | [Cargo and Deposit](Cargo_and_Deposit.md) |
| 창고 클릭·공유 수량 표시 | [Warehouse UI](Warehouse_UI.md) |

정의는 공유 데이터, 수량은 scene runtime 상태다. UI는 조회만 하고 생산·판매·모집·건설의 게임 규칙은 각 domain이 소유한다.
