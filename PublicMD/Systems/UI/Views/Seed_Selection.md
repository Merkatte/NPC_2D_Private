# Seed Selection UI

## 목적과 책임 경계

선택한 밭의 씨앗 목록·확인·실패 표시를 소유한다. 씨앗 소비와 심기 transaction은 FarmSeedSource가 담당한다.

## 현재 실행 흐름

열기·대상 전환 때 이전 선택을 지우고 목록을 읽는다. 선택은 재고를 변경하지 않고 확인 문구에 씨앗 1개 소비를 표시한다. 확정 전에 선택 ID를 비워 연속 입력을 막고 source가 현재 재고·밭 상태를 다시 검사한다. 성공은 RequestClose로 닫고 실패는 목록과 사유를 갱신한다. 닫기·비활성화에서 source와 선택을 해제하며, 대상이 파괴되거나 비활성화되면 닫는다. 종류만 받는 기존 TryShow overload는 유지하되 seed popup은 유효한 source 없이는 열리지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/UI/SeedSelectionPopup.cs` | FarmSeedSource의 보유 씨앗 표시, 선택·확정 요청·실패 문구와 대상 수명 관리 |

## 변경 유형별 최소 확인 범위

- 씨앗 선택: SeedSelectionPopup.cs, SeedSelectionPopup.prefab, SeedSelectionItemSlot.prefab.
- 슬롯 표시: [Merchant Caravan](../../Economy/Merchant_Caravan.md)의 ItemSlotView 계약.
- source와 소비 규칙: [Farming Runtime](../../Economy/Farming/Runtime_and_Transactions.md).

## 불변 규칙

- 선택은 재고를 변경하지 않는다. 확정 시 source가 재고와 밭 상태를 다시 검사한다.
- 닫기·비활성화·대상 소실 때 source와 선택을 해제한다.

## Unity 배선과 검증

`UIManager`에는 popup·hover 배열을 등록한다. FarmerTest의 `Canvas/PopupUI/SeedSelectionPopup`은 비활성 상태로 배치되어 `PopupType.SeedSelection`으로 등록된다. 빈 Soil 클릭에서 전달된 FarmSeedSource가 해당 밭·창고·catalog를 제공하므로 popup에는 고정 창고 참조가 없다. 슬롯은 `ItemSlotView`에 씨앗 이름·수량·아이콘을 전달하며, 이름 표시는 슬롯의 선택적 `_nameText` 연결로 정한다. 심기·취소·닫기 Button을 serialized field로 연결하고 OnEnable/OnDisable에서 listener를 등록·해제한다.

`SeedSelectionPopup._minimumVisibleSlots`는 빈 칸을 포함한 최소 표시 수이며 기본값 20으로 기존 구성을 유지한다. 0이면 실제 보유 씨앗만 표시하고 재고가 없을 때는 빈 상태 문구를 표시한다. Inspector와 runtime에서 음수를 0으로 보정하며, 목록이 줄어들면 남은 대여 슬롯을 비우고 비활성화한다. `RentSlot`은 기존 수동 5열 좌표를 계속 기록한다. 목록에 `GridLayoutGroup`이 연결되어 있으면 layout rebuild가 이 좌표를 덮어써 최종 슬롯 배치를 소유하며, layout이 없을 때는 수동 좌표가 유지된다.

현재 `SeedSelectionPopup.prefab`은 WarmPixel 종이 창을 사용하는 800×700 구성이다. 기존 prefab GUID와 원래 object fileID·씨앗 아이콘·버튼 참조를 보존했다. `_minimumVisibleSlots=0`으로 실제 보유 씨앗만 표시하며, 신규 `SeedSelectionItemSlot.prefab`은 200×145 카드 안에 아이콘·이름·수량을 배치한다. root의 ItemSlotView에 세 표시 참조를 연결하고 Button과 drag handle은 넣지 않는다. 클릭 Button은 기존 RentSlot이 생성한다. 기존 상단·창고 슬롯 prefab은 수정하지 않았다.

가운데 704×350 영역은 ScrollRect·RectMask2D viewport·3열 GridLayoutGroup·세로 PreferredSize ContentSizeFitter·세로 Scrollbar로 구성한다. 카드 간격은 16, grid padding은 8이다. 하단 704×166 영역은 선택 전 안내를 표시하며, 씨앗 선택 시 같은 위치의 ConfirmationPanel이 안내를 덮고 선택 이름·씨앗 1개 소비 문구·심기 확정/취소 버튼을 표시한다. 전체 목록을 덮는 별도 확인 모달은 사용하지 않는다. 기존 source 바인딩·팝업 category·소비와 실패 처리는 유지한다. 프리팹의 저장값·실제 Editor typed reference 로딩과 Play의 화면·클릭·스크롤 확인은 구분한다.

## 알려진 제약과 TBD

실제 화면·입력·Play 확인은 사람의 QA 항목이며 이 문서 재배치에서는 실행하지 않았다.

## 관련 문서

[UI 기능 인덱스](../README.md). 다른 기능의 transaction과 구체 UI는 해당 주 소유 문서를 따른다.

## 문서 갱신 조건

이 leaf의 실행 흐름, 주 소유 파일, 계약 또는 배선이 바뀌면 갱신한다.
