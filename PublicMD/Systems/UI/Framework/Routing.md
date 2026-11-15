# UI Routing

## 목적과 책임 경계

popup·hover category registry와 표시 상태, 공통 BackBG의 표시와 닫기 의도 전달을 조정한다. world 입력 감지와 domain transaction은 소유하지 않는다.

## 현재 실행 흐름

입력 adapter가 category와 source를 전달하면 UIManager가 등록 view에 바인딩·열기·닫기를 위임한다.

BackBG의 uGUI `IPointerClickHandler`는 왼쪽 클릭·터치의 `Clicked` event를 전달한다. UIManager는 `OnEnable`에서 구독하고 `OnDisable`에서 해제하며, 기존 `TryHideTopPopup()`으로 최상위 popup 하나를 닫는다. 이 경로는 `PopBase.Close()`와 stack 제거, 남은 popup에 따른 BackBG 표시 갱신을 그대로 사용한다. 오른쪽·가운데 클릭과 드래그 중 click은 닫기 의도로 전달하지 않는다.

popup은 `PopupType`, hover는 `HoverType`으로 등록한다. `UIManager`는 concrete view의 domain 데이터를 직접 읽지 않는다. hover는 continuous state(enter/exit)라 poll 주기가 있고, click은 discrete edge라 poll하지 않고 매 프레임 press edge만 확인한다 — `Physics2D.OverlapPoint`는 그 press 프레임에만 실행되므로 실질적으로 이미 self-throttling이다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Interface/IUIService.cs` | popup·hover 표시 의도를 전달하는 UI facade 계약 |
| `Assets/Scripts/UI/UIManager.cs` | popup·hover registry와 현재 표시 상태를 조정하는 facade |
| `Assets/Scripts/UI/BackBg.cs` | 공통 popup 배경 fade와 uGUI 클릭 의도 전달 |

## 변경 유형별 최소 확인 범위

- registry·표시 전환: UIManager.cs, IUIService.cs와 popup/hover 등록 배열.
- 배경 클릭 닫기: BackBg.cs, UIManager.cs, [Popup](Popup.md)의 닫기 lifecycle과 BackBG의 Image·CanvasGroup·계층 배선.
- view lifecycle: [Popup](Popup.md), [Hover](Hover.md).

## 불변 규칙

- domain component는 concrete UI를 참조하지 않고 `IHoverInfoSource`(hover) 또는 `IClickPopupSource`(click)로 데이터를 제공한다.
- UI 호출자는 concrete view를 탐색하지 않고 `IUIService`에 category와 source를 전달한다.
- UI registry 중복은 첫 항목을 유지하고 오류로 보고한다.
- 배경 클릭은 최상위 popup 하나만 닫으며 domain transaction이나 게임오버 해제를 수행하지 않는다. popup 내부 클릭과 world 클릭 차단은 기존 uGUI raycast 경로를 유지한다.

## Unity 배선과 검증

현재 UI 전환 대상은 DefenseTest다. PopupUI의 House/Construction/TownHall/Merchant를 신규 WarmPixel instance로 등록하며 기존 SeedSelection/Warehouse와 두 hover를 유지한다. 운영 popup은 초기 비활성, HUD는 활성이다. 다른 씬은 이번 작업에서 변경하지 않는다. FarmerTest 및 목재 UI의 기존 배선·표현은 [씨앗 선택](../Views/Seed_Selection.md)과 [공통 표현](../Presentation.md)에 기록한다.

`UIManager`에는 popup·hover 배열을 등록한다.

DefenseTest의 BackBG는 Canvas 아래 PopupUI보다 앞선 sibling이며 자식이 없다. 전체 화면 Image의 `raycastTarget`과 CanvasGroup의 `blocksRaycasts`·`interactable`이 켜져 있고, UIManager의 `_backBg`가 같은 BackBg를 참조한다. popup은 별도 PopupUI 아래에 있어 내부 클릭이 BackBG로 올라가지 않는다. 배경 클릭은 release 시점에 닫고, [Pointer Input](Pointer_Input.md)의 press 시점 GraphicRaycaster 확인은 그대로 유지하여 뒤의 world 객체 클릭을 차단한다.

## 알려진 제약과 TBD

같은 source의 표시 재확인은 중복 개방을 일으키지 않는다. hover 퇴장 중 참조와 즉시 정리 계약은 [Hover](Hover.md)를 따른다. 실제 표시 회귀는 사람의 확인 항목이다.

## 관련 문서

[UI 기능 인덱스](../README.md). 다른 기능의 transaction과 구체 UI는 해당 주 소유 문서를 따른다.

## 문서 갱신 조건

이 leaf의 실행 흐름, 주 소유 파일, 계약 또는 배선이 바뀌면 갱신한다.
