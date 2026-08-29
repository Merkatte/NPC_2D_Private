# UI

## 기능 목적

popup과 hover의 category 기반 표시 lifecycle, pointer 입력 adapter, 농장 게이지 표현을 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| UI routing | category registry, popup stack, active hover 조정 |
| Hover input | world pointer 아래 source를 찾아 표시 의도 전달 |
| Concrete views | popup/hover 공통 lifecycle과 농장 progress 표현 |

## 현재 실행 흐름

```text
PointerHoverRouter
  -> Physics2D.OverlapPoint(hoverable mask)
  -> 같은 GameObject의 IHoverInfoSource
  -> IUIService.TryShow(source.HoverType, source)
  -> UIManager registry
  -> HoverBase.TryShow/주기적 refresh
  -> FarmGaugeHover.ApplyInfo
```

popup은 `PopupType`, hover는 `HoverType`으로 등록한다. `UIManager`는 concrete view의 domain 데이터를 직접 읽지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/HoverInfo.cs` | hover view가 그릴 anchor와 progress 정보 값 |
| `Assets/Scripts/Enum/HoverType.cs` | hover registry category key |
| `Assets/Scripts/Enum/PopupType.cs` | popup registry category key |
| `Assets/Scripts/Interface/IHoverInfoSource.cs` | domain object가 hover 정보와 Unity owner를 제공하는 계약 |
| `Assets/Scripts/Interface/IUIService.cs` | popup·hover 표시 의도를 전달하는 UI facade 계약 |
| `Assets/Scripts/UI/FarmGaugeHover.cs` | farm progress를 fill과 screen anchor로 표현하는 hover view |
| `Assets/Scripts/UI/HoverBase.cs` | source 소유권, refresh, show/hide 공통 hover lifecycle |
| `Assets/Scripts/UI/PointerHoverRouter.cs` | pointer hit를 hover source와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/UI/PopBase.cs` | popup 식별자와 open/close 공통 lifecycle |
| `Assets/Scripts/UI/UIManager.cs` | popup·hover registry와 현재 표시 상태를 조정하는 facade |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 새 popup | `PopupType.cs`, `PopBase.cs`, `UIManager.cs` |
| 새 hover | `HoverType.cs`, `HoverBase.cs`, `IHoverInfoSource.cs`, `UIManager.cs` |
| pointer 감지 | `PointerHoverRouter.cs`, source collider와 layer 배선 |
| 농장 gauge | `FarmGaugeHover.cs`, `HoverInfo.cs`, `Assets/Prefab/UI/FarmGauge.prefab`, [Farming](Farming.md) |

## 불변 규칙

- domain component는 concrete UI를 참조하지 않고 `IHoverInfoSource`로 데이터를 제공한다.
- UI 호출자는 concrete view를 탐색하지 않고 `IUIService`에 category와 source를 전달한다.
- collider와 `IHoverInfoSource`는 현재 같은 GameObject에 있어야 한다.
- UI registry 중복은 첫 항목을 유지하고 오류로 보고한다.
- hover owner가 파괴되면 view는 다음 refresh에서 스스로 닫힌다.

## Unity 배선

`UIManager`에는 popup·hover 배열을 등록한다. `PointerHoverRouter`에는 world camera, `IUIService` 구현 source, Hoverable layer mask가 필요하다. `FarmGaugeHover`에는 fill image와 camera가 필요하다.

## 알려진 제약과 TBD

- `Physics2D.OverlapPoint`는 겹친 collider의 명시적 UI 우선순위를 제공하지 않는다.
- 현재 concrete popup 구현은 최소 골격 수준이다.

## 관련 문서

- [Farming](Farming.md)
- [Interaction and Destinations](Interaction_and_Destinations.md)

## 문서 갱신 조건

UI facade, registry, hover 입력, view lifecycle 또는 domain-to-UI 계약이 바뀌면 갱신한다.
