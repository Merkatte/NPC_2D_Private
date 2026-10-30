# Popup Lifecycle and Shared Views

## 목적과 책임 경계

공통 popup 바인딩·열기·닫기, Canvas 맞춤과 공용 재료 행 표시를 소유한다. 개별 팝업의 거래·모집·건설 요청은 각 domain 문서가 소유한다.

## 현재 실행 흐름

IUIService.TryShow → UIManager → PopBase.TryBindSource → Open. 이미 열린 popup의 대상 변경도 바인딩 경로를 사용한다. PopupCanvasFit은 크기만 맞추며 위치는 PopBase가 소유한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Enum/PopupType.cs` | popup registry category key |
| `Assets/Scripts/UI/PopBase.cs` | popup 식별자와 open/close 공통 lifecycle |
| `Assets/Scripts/UI/PopupCanvasFit.cs` | root Canvas 크기에 맞춘 팝업 전체 비율 축소, 위치는 PopBase 소유 |
| `Assets/Scripts/UI/ResourceCostRowView.cs` | 주택·건설 공용 재료 아이콘·이름·필요/보유와 부족 색 표시 |

## 변경 유형별 최소 확인 범위

- 공통 popup: PopupType.cs, PopBase.cs와 [Routing](Routing.md).
- 화면 맞춤: PopupCanvasFit.cs와 해당 prefab/scene override.
- 재료 행: ResourceCostRowView.cs와 소비 popup의 비용 표시.

## 불변 규칙

- 1:1 전용 popup-도메인 배선(예: `MerchantPopup`-`MerchantTradeSite`)은 interface로 감싸지 않는다. `IHoverInfoSource`/`IClickPopupSource`처럼 여러 구현체가 존재하거나 존재할 수 있는 진짜 다형성 상황에서만 interface를 쓴다.
- view는 자원 수량을 직접 변경하지 않는다.

## Unity 배선과 검증

PopupCanvasFit은 신규 네 popup과 DefenseTest의 기존 SeedSelection instance에 연결한다. 최초 localScale을 기준으로 root Canvas의 크기를 부모 좌표계로 변환하고 여백24 안에 전체가 들어오도록 축소한다. 확대나 내부 재배치는 하지 않는다. PopupUI의 100×100 크기를 화면 크기로 오인하지 않으며 위치는 PopBase, 주택 내부 너비는 WarmPixelHousePopup이 소유한다.

ResourceCostRowView는 주어진 이름·아이콘·필요량·보유량만 표시한다. domain을 조회하거나 자원을 변경하지 않는다. 아이콘 누락에도 텍스트를 유지하고 실제 요구 비용일 때만 부족 색을 적용한다. 환불 표시는 요구 비용으로 취급하지 않는다.

## 알려진 제약과 TBD

- concrete popup은 MerchantPopup, TownHallPopup, SeedSelectionPopup, ConstructionPopup, WarehousePopup, HousePopup 여섯 종류다. 건설 UI는 [Construction UI](../../Settlement/Construction/UI.md), 창고 UI는 [Warehouse UI](../../Economy/Inventory_and_Items/Warehouse_UI.md), 주택 UI는 [Housing UI](../../Settlement/Housing/UI.md)가 주 소유한다. PopupType.House=6을 기존 값 뒤에 추가한다. 공통 UI는 source만 전달하며 씨앗 transaction은 [농장 runtime](../../Economy/Farming/Runtime_and_Transactions.md)이 소유한다.

## 관련 문서

[UI 기능 인덱스](../README.md). 다른 기능의 transaction과 구체 UI는 해당 주 소유 문서를 따른다.

## 문서 갱신 조건

이 leaf의 실행 흐름, 주 소유 파일, 계약 또는 배선이 바뀌면 갱신한다.
