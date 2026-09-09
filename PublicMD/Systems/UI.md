# UI

## 기능 목적

popup과 hover의 category 기반 표시 lifecycle, pointer 입력 adapter, 농장 게이지 표현을 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| UI routing | category registry, popup stack, active hover 조정 |
| Hover input | world pointer 아래 source를 찾아 표시 의도 전달 |
| Click input | world pointer 클릭을 감지해 popup 오픈 의도 전달 |
| Concrete views | popup/hover 공통 lifecycle과 농장 progress 표현 |
| Drag input | uGUI `EventSystem` 기반으로 UI 슬롯을 끌어 다른 UI 영역에 놓는 의도 전달(첫 사례: Merchant 판매 UI) |

## 현재 실행 흐름

```text
PointerHoverRouter
  -> Physics2D.OverlapPoint(hoverable mask, 주기적 poll)
  -> 같은 GameObject의 IHoverInfoSource
  -> IUIService.TryShow(source.HoverType, source)
  -> UIManager registry
  -> HoverBase.TryShow/주기적 refresh
  -> FarmGaugeHover.ApplyInfo

PointerClickRouter
  -> mouse.leftButton.wasPressedThisFrame (매 프레임 확인, poll 아님)
  -> Physics2D.OverlapPoint(clickable mask)
  -> 같은 GameObject의 IClickPopupSource
  -> source.TryGetClickPopup(out PopupType) — 지금 클릭 가능한지는 source가 판단(예: MerchantVisual의 Animator 상태 조회)
  -> IUIService.TryShow(popupType)
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
`PointerClickRouter`/`PointerHoverRouter`의 `Physics2D.OverlapPoint` world 경로와는 완전히 별개의 입력 계통이다 — 드래그 앤 드롭은 uGUI `Canvas`/`GraphicRaycaster`/`EventSystem` 위에서만 동작하고, world clickable 레이어와는 무관하다. 소스는 드롭 대상을 모르고 고스트만 다루며, "받아들일지"는 전적으로 드롭 대상이 판단한다. 구체적인 소유·흐름은 [Merchant Caravan](Merchant_Caravan.md)을 참고한다 — 이번 도입은 그 기능 전용이며 범용 드래그 프레임워크로 일반화하지 않았다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/HoverInfo.cs` | hover view가 그릴 anchor와 progress 정보 값 |
| `Assets/Scripts/Enum/HoverType.cs` | hover registry category key |
| `Assets/Scripts/Enum/PopupType.cs` | popup registry category key |
| `Assets/Scripts/Interface/IHoverInfoSource.cs` | domain object가 hover 정보와 Unity owner를 제공하는 계약 |
| `Assets/Scripts/Interface/IClickPopupSource.cs` | world object가 클릭 시 열 PopupType을 답하는 최소 계약 |
| `Assets/Scripts/Interface/IUIService.cs` | popup·hover 표시 의도를 전달하는 UI facade 계약 |
| `Assets/Scripts/UI/FarmGaugeHover.cs` | farm progress를 fill과 screen anchor로 표현하는 hover view |
| `Assets/Scripts/UI/HoverBase.cs` | source 소유권, refresh, show/hide 공통 hover lifecycle |
| `Assets/Scripts/UI/PointerHoverRouter.cs` | pointer hit를 hover source와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/UI/PointerClickRouter.cs` | pointer 클릭을 `IClickPopupSource`와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/UI/PopBase.cs` | popup 식별자와 open/close 공통 lifecycle |
| `Assets/Scripts/UI/MerchantPopup.cs` | 이 프로젝트 최초의 concrete popup — 상단 거래 UI([Merchant Caravan](Merchant_Caravan.md) 주 소유) |
| `Assets/Scripts/UI/UIManager.cs` | popup·hover registry와 현재 표시 상태를 조정하는 facade |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 새 popup | `PopupType.cs`, `PopBase.cs`, `UIManager.cs` |
| 새 hover | `HoverType.cs`, `HoverBase.cs`, `IHoverInfoSource.cs`, `UIManager.cs` |
| hover 감지 | `PointerHoverRouter.cs`, source collider와 layer 배선 |
| click 감지 | `PointerClickRouter.cs`, `IClickPopupSource.cs`, source collider와 layer 배선 |
| 농장 gauge | `FarmGaugeHover.cs`, `HoverInfo.cs`, `Assets/Prefab/UI/FarmGauge.prefab`, [Farming](Farming/README.md) |

## 불변 규칙

- domain component는 concrete UI를 참조하지 않고 `IHoverInfoSource`(hover) 또는 `IClickPopupSource`(click)로 데이터를 제공한다.
- UI 호출자는 concrete view를 탐색하지 않고 `IUIService`에 category와 source를 전달한다.
- collider와 `IHoverInfoSource`/`IClickPopupSource`는 현재 같은 GameObject에 있어야 한다.
- UI registry 중복은 첫 항목을 유지하고 오류로 보고한다.
- hover owner가 파괴되면 view는 다음 refresh에서 스스로 닫힌다.
- `IClickPopupSource.TryGetClickPopup`이 `false`를 반환하는 이유(예: 애니메이션 진행 중)는 router가 절대 알지 못한다 — 게이트 로직은 전부 구현체 내부에 있다.
- 1:1 전용 popup-도메인 배선(예: `MerchantPopup`-`MerchantTradeSite`)은 interface로 감싸지 않는다. `IHoverInfoSource`/`IClickPopupSource`처럼 여러 구현체가 존재하거나 존재할 수 있는 진짜 다형성 상황에서만 interface를 쓴다.

## Unity 배선

`UIManager`에는 popup·hover 배열을 등록한다. `PointerHoverRouter`에는 world camera, `IUIService` 구현 source, Hoverable layer mask가 필요하다. `PointerClickRouter`에는 world camera, `IUIService` 구현 source, Clickable layer mask가 필요하다(Hoverable과 별도 레이어 — 클릭과 hover는 의미가 다른 별개 관심사라 굳이 합치지 않는다). `FarmGaugeHover`에는 fill image와 camera가 필요하다.

## 알려진 제약과 TBD

- `Physics2D.OverlapPoint`는 겹친 collider의 명시적 UI 우선순위를 제공하지 않는다.
- 열린 popup 위에서 world 클릭이 그대로 통과해 다른 clickable에 닿을 수 있다 — 지금은 clickable이 상단 하나뿐이라 관측되지 않지만, `Physics2DRaycaster`/`EventSystem` 기반 world click-through 차단은 아직 없다.
- concrete popup은 `MerchantPopup` 하나뿐이다.

## 관련 문서

- [Farming](Farming/README.md)
- [Interaction and Destinations](Interaction_and_Destinations.md)
- [Merchant Caravan](Merchant_Caravan.md) — `MerchantPopup`과 드래그 앤 드롭 슬롯 UI의 주 소유 문서

## 문서 갱신 조건

UI facade, registry, hover 입력, view lifecycle 또는 domain-to-UI 계약이 바뀌면 갱신한다.
