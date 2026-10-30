# UI Views

UI 영역이 직접 소유하는 씨앗 선택 창과 골드 표시를 안내한다. 다른 기능의 전용 popup은 해당 domain 문서가 소유한다.

| 변경 대상 | leaf |
|---|---|
| 밭 source 바인딩, 씨앗 목록·선택·확인·실패 표시 | [Seed Selection](Seed_Selection.md) |
| 상시 골드 잔액·증감, 선택적 알림 연출·popup context | [Gold HUD](Gold_HUD.md) |

씨앗 선택은 [공통 popup](../Framework/Popup.md)으로 열고 [농장](../../Economy/Farming/README.md)에 심기 의도를 전달한다. 골드 HUD는 registry와 별도로 [공유 자원](../../Economy/Inventory_and_Items/Shared_Resources.md)의 알림을 구독한다. 두 view 모두 domain 상태를 직접 변경하지 않는다.

새 표시 기능의 소유권이나 이 연결이 바뀌면 이 인덱스를 갱신한다.
