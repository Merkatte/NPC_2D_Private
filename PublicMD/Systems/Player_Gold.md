# Player Gold

골드는 [Shared Resources](Inventory_and_Items/Shared_Resources.md)의 item ID 8이다. ResourceManager API로 조회·지출·환급·교환하고 ResourceInventory가 실제 잔액을 보관한다. 별도 GoldManager 저장소는 scene 이관 후 제거한다. 골드는 용량을 쓰지 않고 창고 목록에도 표시하지 않는다.

가격과 판매 규칙은 [Merchant Caravan](Merchant_Caravan.md), 모집 비용과 환급 시점은 [Town Hall](Town_Hall.md), 건설 지불/환불은 [Construction Plots](Construction/Plots.md)가 소유한다. 시작 잔액은 TestOnly bootstrap으로 지급하며 TestGoldWindow는 새 API의 수동 도구다.

이 문서는 기존 링크를 위한 안내이며 production C#을 별도로 소유하지 않는다. transaction의 상세 계약과 검증은 Shared Resources를 따른다.
