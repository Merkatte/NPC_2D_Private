# Pointer Input

## 목적과 책임 경계

world hover·click을 source/service 계약으로 변환한다. uGUI 드래그의 구체 구현은 상단 문서가 소유한다.

## 현재 실행 흐름

```text
PointerHoverRouter
  -> Physics2D.OverlapPoint(설정 mask의 겹친 collider 목록, 주기적 poll)
  -> 같은 GameObject의 IHoverInfoSource
  -> IUIService.TryShow(source.HoverType, source)
  -> UIManager registry
  -> HoverBase.TryShow/주기적 refresh
  -> FarmGaugeHover.ApplyInfo / NPCMessageHover.ApplyInfo

PointerClickRouter
  -> mouse.leftButton.wasPressedThisFrame (매 프레임 확인, poll 아님)
  -> 클릭 순간 uGUI GraphicRaycaster hit가 있으면 world 입력 중단
  -> Physics2D.OverlapPoint(clickable mask)의 겹친 collider 중 유효 source 탐색
  -> 같은 GameObject의 IClickPopupSource
  -> source.TryGetClickPopup(out PopupType) — 지금 클릭 가능한지는 source가 판단(예: MerchantVisual에 전달된 거래 가능 상태)
  -> IUIService.TryShow(popupType, source)
  -> PopBase.TryBindSource 성공 후 Open (이미 열린 대상의 재바인딩도 처리)
```

popup은 `PopupType`, hover는 `HoverType`으로 등록한다. `UIManager`는 concrete view의 domain 데이터를 직접 읽지 않는다. hover는 continuous state(enter/exit)라 poll 주기가 있고, click은 discrete edge라 poll하지 않고 매 프레임 press edge만 확인한다 — `Physics2D.OverlapPoint`는 그 press 프레임에만 실행되므로 실질적으로 이미 self-throttling이다.

```text
uGUI 드래그 앤 드롭 (Merchant 판매 UI, 첫 사례):
EventSystem(InputSystemUIInputModule)
  -> IBeginDragHandler/IDragHandler/IEndDragHandler 구현 컴포넌트(드래그 소스)
     -> 재사용 고스트 뷰를 SetActive로 보여주고 커서를 따라 이동
  -> IDropHandler 구현 컴포넌트(드롭 대상)
     -> eventData.pointerDrag에서 소스를 식별해 도메인에 의도만 전달
```
`PointerClickRouter`/`PointerHoverRouter`의 `Physics2D.OverlapPoint` world 경로와는 완전히 별개의 입력 계통이다 — 드래그 앤 드롭은 uGUI `Canvas`/`GraphicRaycaster`/`EventSystem` 위에서만 동작하고, world clickable 레이어와는 무관하다. 소스는 드롭 대상을 모르고 고스트만 다루며, "받아들일지"는 전적으로 드롭 대상이 판단한다. 구체적인 소유·흐름은 [Merchant Caravan](../../Economy/Merchant_Caravan.md)을 참고한다 — 이번 도입은 그 기능 전용이며 범용 드래그 프레임워크로 일반화하지 않았다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/UI/PointerHoverRouter.cs` | pointer hit를 hover source와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/UI/PointerClickRouter.cs` | pointer 클릭을 `IClickPopupSource`와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/Interface/IClickPopupSource.cs` | world object가 클릭 시 열 PopupType을 답하는 최소 계약 |

## 변경 유형별 최소 확인 범위

- hover 감지: PointerHoverRouter.cs, source collider와 layer mask.
- click 감지: PointerClickRouter.cs, IClickPopupSource.cs, EventSystem과 GraphicRaycaster.
- 드래그: [Merchant Caravan](../../Economy/Merchant_Caravan.md).

## 불변 규칙

- collider와 `IHoverInfoSource`/`IClickPopupSource`는 현재 같은 GameObject에 있어야 한다.
- `IClickPopupSource.TryGetClickPopup`이 `false`를 반환하는 이유(예: 애니메이션 진행 중)는 router가 절대 알지 못한다 — 게이트 로직은 전부 구현체 내부에 있다.

## Unity 배선과 검증

PointerHoverRouter/PointerClickRouter에는 world camera, IUIService 구현 source와 감지 mask가 필요하다. 현재 FarmerTest의 click mask는 전체 레이어이며 Soil의 기존 Hoverable collider를 재사용한다. Router는 NPC·농장 구체 타입을 참조하지 않는다. FarmGaugeHover에는 fill image와 camera를 연결한다.

## 알려진 제약과 TBD

- PointerHoverRouter는 겹친 collider의 유효 source 중 설정된 우선 category를 먼저 선택한다.
  동일 source도 표시 상태를 재확인하여 닫힌 hover가 조건 회복 뒤 다시 열릴 수 있다.
  UIManager는 이미 표시 중인 같은 source를 재개방하지 않는다.
- PointerClickRouter는 press 시점의 EventSystem raycast 중 GraphicRaycaster hit를 확인해 UI 뒤의 world 클릭을 차단한다. native 입력 순서·화면 회귀는 별도 Play 검증 대상이다.

## 관련 문서

[UI 기능 인덱스](../README.md). 다른 기능의 transaction과 구체 UI는 해당 주 소유 문서를 따른다.

## 문서 갱신 조건

이 leaf의 실행 흐름, 주 소유 파일, 계약 또는 배선이 바뀌면 갱신한다.
