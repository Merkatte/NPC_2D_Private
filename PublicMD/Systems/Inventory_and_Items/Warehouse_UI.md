# Warehouse UI

## 책임과 흐름

WarehouseDepositPoint를 기존 PointerClickRouter/PopBase 경로로 열고 공유 ResourceManager의 수량과 용량을 표시한다. 특정 창고의 독립 재고 화면이 아니다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Scripts/UI/WarehousePopup.cs | source 바인딩·수량 표시·구독 수명 |
| Assets/Scripts/UI/ResourceQuantityRow.cs | 자원 한 항목 표시 |

## 최소 확인 범위와 계약

위 타입, WarehouseDepositPoint, ItemInfo.ShowInWarehouse와 UI.md를 확인한다. 열기/재바인딩에서 최초 조회하고 ResourcesChanged를 구독한다. 닫기/disable에서 이전 구독을 해제한다. 매 frame polling하거나 수량을 직접 수정하지 않는다. Gold는 목록에서 제외한다.

## 배선·검증·제약

WarehousePopup/ResourceQuantityRow prefab을 Canvas/PopupUI와 UIManager registry에 연결한다. 참조·구독·source 수명을 검사하고 화면은 사람이 확인한다. 별도 골드 HUD·정렬 확장·직접 물품 조작은 범위 밖이다. popup API/직렬화 필드 변경 시 갱신한다.
