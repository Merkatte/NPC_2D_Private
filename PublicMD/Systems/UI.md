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
  -> source.TryGetClickPopup(out PopupType) — 지금 클릭 가능한지는 source가 판단(예: MerchantVisual의 Animator 상태 조회)
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
`PointerClickRouter`/`PointerHoverRouter`의 `Physics2D.OverlapPoint` world 경로와는 완전히 별개의 입력 계통이다 — 드래그 앤 드롭은 uGUI `Canvas`/`GraphicRaycaster`/`EventSystem` 위에서만 동작하고, world clickable 레이어와는 무관하다. 소스는 드롭 대상을 모르고 고스트만 다루며, "받아들일지"는 전적으로 드롭 대상이 판단한다. 구체적인 소유·흐름은 [Merchant Caravan](Merchant_Caravan.md)을 참고한다 — 이번 도입은 그 기능 전용이며 범용 드래그 프레임워크로 일반화하지 않았다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/HoverInfo.cs` | anchor/progress 및 선택적인 LocalizeKey·추적 Transform 정보 값 |
| `Assets/Scripts/Enum/HoverType.cs` | hover registry category key |
| `Assets/Scripts/Enum/PopupType.cs` | popup registry category key |
| `Assets/Scripts/Interface/IHoverInfoSource.cs` | domain object가 hover 정보와 Unity owner를 제공하는 계약 |
| `Assets/Scripts/Interface/IClickPopupSource.cs` | world object가 클릭 시 열 PopupType을 답하는 최소 계약 |
| `Assets/Scripts/Interface/IUIService.cs` | popup·hover 표시 의도를 전달하는 UI facade 계약 |
| `Assets/Scripts/UI/FarmGaugeHover.cs` | farm progress를 fill과 screen anchor로 표현하는 hover view |
| `Assets/Scripts/UI/NPCMessageHover.cs` | LocalizeText 문구 표시와 머리 위 anchor의 LateUpdate 화면 위치 추적 |
| `Assets/Scripts/UI/HoverBase.cs` | source 소유권, refresh, show/hide 공통 hover lifecycle |
| `Assets/Scripts/UI/PointerHoverRouter.cs` | pointer hit를 hover source와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/UI/PointerClickRouter.cs` | pointer 클릭을 `IClickPopupSource`와 UI service 호출로 변환하는 입력 adapter |
| `Assets/Scripts/UI/PopBase.cs` | popup 식별자와 open/close 공통 lifecycle |
| `Assets/Scripts/UI/MerchantPopup.cs` | 이 프로젝트 최초의 concrete popup — 상단 거래 UI([Merchant Caravan](Merchant_Caravan.md) 주 소유) |
| `Assets/Scripts/UI/TownHallPopup.cs` | 시청 모집 UI([Town Hall](Town_Hall.md) 주 소유) |
| `Assets/Scripts/UI/TownHallRecruitCard.cs` | 시청 직군별 모집 상태 표시([Town Hall](Town_Hall.md) 주 소유) |
| `Assets/Scripts/UI/SeedSelectionPopup.cs` | FarmSeedSource의 보유 씨앗 표시, 선택·확정 요청·실패 문구와 대상 수명 관리 |
| `Assets/Scripts/UI/UIManager.cs` | popup·hover registry와 현재 표시 상태를 조정하는 facade |
| `Assets/Scripts/UI/GoldHUD.cs` | 공유 골드 잔액과 최신 증감을 이벤트 기반으로 표시하는 상시 HUD |

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

`UIManager`에는 popup·hover 배열을 등록한다. FarmerTest의 `Canvas/PopupUI/SeedSelectionPopup`은 비활성 상태로 배치되어 `PopupType.SeedSelection`으로 등록된다. 빈 Soil 클릭에서 전달된 FarmSeedSource가 해당 밭·창고·catalog를 제공하므로 popup에는 고정 창고 참조가 없다. 기존 MerchantItemSlot 모양과 씨앗 아이콘을 유지한다. 심기·취소·닫기 Button을 serialized field로 연결하고 OnEnable/OnDisable에서 listener를 등록·해제한다.

열기·대상 전환 때 이전 선택을 지우고 목록을 읽는다. 선택은 재고를 변경하지 않고 확인 문구에 씨앗 1개 소비를 표시한다. 확정 전에 선택 ID를 비워 연속 입력을 막고 source가 현재 재고·밭 상태를 다시 검사한다. 성공은 RequestClose로 닫고 실패는 목록과 사유를 갱신한다. 닫기·비활성화에서 source와 선택을 해제하며, 대상이 파괴되거나 비활성화되면 닫는다. 종류만 받는 기존 TryShow overload는 유지하되 seed popup은 유효한 source 없이는 열리지 않는다.

PointerHoverRouter/PointerClickRouter에는 world camera, IUIService 구현 source와 감지 mask가 필요하다. 현재 FarmerTest의 click mask는 전체 레이어이며 Soil의 기존 Hoverable collider를 재사용한다. Router는 NPC·농장 구체 타입을 참조하지 않는다. FarmGaugeHover에는 fill image와 camera를 연결한다.

## 팝업 공통 표현

상단(MerchantPopup)의 목재 외곽·제목 명패·종이 내부 패널·녹색 확정/적갈색 취소 버튼을 기준으로 재사용한다. 기능별 내용 구조와 루트 크기는 유지한다. 닫기 X는 실제 배경 우측 상단 안쪽에 두며 anchor/pivot은 (1,1), 오른쪽·위 여백은 버튼 한 변의 1/4이다. 현재 Merchant 100/25, TownHall 96/24, SeedSelection 84/21, Construction·Warehouse 56/14 UI 단위다. Merchant 배경은 루트보다 위로 확장되어 있으므로 실제 배경 상단을 기준으로 위치를 계산한다.

건설·창고의 제목판과 BodyBackground는 기존 UI 이미지를 사용하는 별도 자식 Image이며 raycast를 받지 않는다. 기존 닫기 Button과 클릭 연결을 유지하고 이전 닫기 라벨을 제목판으로 옮겼다. 본문과 목록은 목재 프레임 안쪽에 배치한다. 현재 건설 5개 행과 창고 9개 항목이 들어가는 범위를 기준으로 하며 항목 수 증가 시 목록 높이·스크롤 정책을 다시 확인해야 한다.

씬의 팝업 크기 override를 유지한다. 프리팹의 저장값·참조 검토는 Unity에서의 실제 표시 및 클릭 검증과 구분하며, 화면 QA는 사람의 확인 항목이다.

## 상시 골드 HUD

`GoldHUD`는 popup/hover registry에 등록하지 않는 상시 표시 컴포넌트다. scene의 `ResourceManager`를 serialized reference로 받고 `ResourcesChanged`에서 `GetQuantity(ResourceManager.GoldItemId)`를 조회한다. 골드 저장·차감·환급 규칙은 [Shared Resources](Inventory_and_Items/Shared_Resources.md)가 소유하며 HUD는 값을 변경하지 않는다.

활성화 때 현재 잔액을 천 단위 구분으로 즉시 표시하고 첫 조회에는 증감을 표시하지 않는다. 골드가 실제로 바뀐 경우만 최신 차이를 `+150`/`-100` 형식으로 표시한다. 다른 자원이나 용량 변경은 표시 타이머를 재시작하지 않는다. 증감은 기본 1.5초의 unscaled 시간 뒤 지우며 Inspector의 Change Display Duration이 0이면 표시하지 않는다. 비활성화 때 이벤트 구독·coroutine·이전 표시를 정리한다.

BuildingTest의 `Canvas/HUDUI/GoldHUD`에 `Assets/Prefab/UI/GoldHUD.prefab`을 배치한다. HUDUI는 Canvas의 마지막 자식으로 두어 popup과 겹칠 때도 잔액이 보이게 한다. 기존 HoveringUI·PopupUI의 상대 순서는 유지한다. 기존 목재 패널 `ui-merchant-board-panel-9slice.png`는 Sliced 이미지, `ui-gold-coin.png`의 `ui-gold-coin_0`는 골드 아이콘으로 재사용한다. AmountText와 ChangeText는 기존 UI와 같은 Legacy Text다. 배경·아이콘·텍스트의 raycast와 HUDUI의 blocksRaycasts를 꺼 world 클릭을 가로채지 않는다.

변경 시 먼저 `GoldHUD.cs`, `ResourceManager.cs`, `GoldHUD.prefab`과 BuildingTest의 HUDUI 배선을 확인한다. prefab은 scene 자원 저장소를 소유하지 않고 scene instance의 override로 ResourceManager를 연결한다.

## 문구 표시 연계

꼬리 없는 독립 말풍선 패널은 `Assets/Art/Generated/UI/ui-speech-panel-white-9slice.png`다.
128×128 RGBA, 불투명 흰색 내부·검은색 3px 테두리·둥근 모서리·투명 외곽을 사용한다.
Single/FullRect, PPU 100, Bilinear, 비압축, mipmap off, 사방 Border 32px를 `.meta`에 저장했다.
UI `Image.Type = Sliced`, `Fill Center = true`로 사용한다. 일반 Simple 이미지 확대는 9-slicing이 아니다.
원본 pixel 기준 최소 가로/세로 64 이상에서 고정 corner 영역을 온전히 유지한다.
캐릭터 연결부·꼬리·텍스트·씬/prefab 배선은 포함하지 않는다. 가로/세로 9-slice 수치 검사는 통과했으며
Unity import/Canvas 실표시는 미검증이다. 원본 SVG·늘림 미리보기·설정 증거는 `.harness-runs/speech-panel-20260920/`에 있다.

`LocalizeText`의 CSV key 기반 Text/TMP 표시와 persistent `LocalizeManager` 접근은
[Localization](Localization.md)이 주 소유한다. UIManager의 popup/hover registry에는 등록하지 않는다.
첫 적용은 독립 LocalizeTest 씬이며 기존 UI 문구는 이번 작업에서 이관하지 않았다.

주민 생각·지정 대사는 [NPC Messages](NPC_Messages.md)의 source가 `HoverType.NPCMessage`로 제공한다.
`NPCMessageHover`는 문구를 선택하지 않고 `LocalizeText.SetKey`, 매 프레임 anchor 위치 추적과 표시 연출을 담당한다.
기존 HoverInfo 생성 호출은 그대로 유효하며, 선택적인 MessageKey/TrackingAnchor를 메시지 view가 소비한다.
FarmerTest의 `Canvas/HoveringUI/NPCMessageHover/MessageText`에 말풍선 한 개를 추가하고 UIManager에 등록했다.
기존 농장 게이지·PopupUI 위치와 순서는 유지한다. Router는 기존 serialized layer mask에 Friendly를 runtime filter로 합친다.
NPCGirl source/anchor/Catalog와 별도 루트 LocalizeManager 배선은 [NPC Messages](NPC_Messages.md)를 따른다.
사용자가 기존 말풍선 표시를 정상 확인했다. 한글·9-slice·화면 경계 전체 시나리오의 독립 검증과 신규 연출의 시각 확인은 별도다.

### NPC 말풍선 표시 연출

- 기존 Legacy Text 말풍선은 DOTween `DOText`로 빈 문자열부터 대사를 표시한다. duration은 문자열 길이 ×
  기본 글자당 0.025초이며 `Ease.Linear`, `SetUpdate(true)`를 사용한다. Inspector의 Typing / Character Interval로
  조절하며 0이면 즉시 표시한다. 현재 CSV의 완성형 한글은 글자 단위로 나타난다.
  LocalizeText의 첫 Start 이후 LateUpdate에서 문구를 읽고 tween을 시작한다. Rich Text 설정은 Text의 값을 따른다.
  새 key 또는 다른 anchor에서 재시작하며 같은 source의 refresh에는 이어서 표시한다. 퇴장 시 Pause하고
  완전히 닫히기 전 같은 대상 재진입 시 Play한다. 비활성화·대사 교체 시 Kill하고 원문을 복구한다.
  현재 배선처럼 LocalizeText와 Text가 같은 오브젝트에 있어야 한다. TMP는 기존 즉시 표시를 유지한다.
  별도 문자열 캐시·투명 뒷부분은 사용하지 않으므로 정렬·줄바꿈은 현재까지 표시된 문자열을 기준으로 한다.
- `NPCMessageHover`의 DOTween 하나가 0~1 표시 진행도를 움직인다. 기본 진입은 0.2초/OutCubic,
  퇴장은 0.15초/InCubic이며 `SetUpdate(true)`로 게임 시간 배율과 독립적이다.
- 진행도에 따라 원래 크기의 65%에서 100%로 커지며 18 Canvas UI 단위 아래에서 anchor로 올라온다.
  CanvasGroup alpha도 함께 변한다. 퇴장은 반대 방향이다. Motion의 시간·거리·시작 크기는 Inspector에서 조정한다.
- LateUpdate가 현재 anchor·카메라 위치에 연출 오프셋과 크기를 합친 뒤 화면 경계를 보정한다.
  위치 tween이 anchor 추적을 덮어쓰지 않는다. 기존 scene 참조와 Hierarchy를 그대로 사용한다.
- `HoverBase.HideCurrent`는 source를 즉시 해제하고 `BeginHide`를 호출한다. 기본 hover는 즉시 닫히며,
  NPC 말풍선만 퇴장 완료 뒤 `CompleteHide`로 비활성화한다. 닫히는 동안 문구를 재조회하지 않는다.
- UIManager는 닫히는 view 참조를 유지한다. 같은 말풍선 재진입은 남은 진행도에서 열리고,
  다른 category 표시·HideAll·초기 registry 정리는 `HideImmediately`로 이전 view를 즉시 닫는다.
  다른 NPC anchor로 바뀌면 새 진입 연출을 시작한다. 같은 source 재확인은 tween을 재시작하지 않는다.
- NPCMessageHover 비활성화 시 자신이 소유한 tween을 완료 콜백 없이 Kill하고 진행도·anchor·문구 캐시를 정리한다.
  anchor가 파괴되거나 비활성화되면 즉시 닫는다. HoverBase도 비활성화 시 source를 해제한다.

## 알려진 제약과 TBD

- PointerHoverRouter는 겹친 collider의 유효 source 중 설정된 우선 category를 먼저 선택한다.
  동일 source도 표시 상태를 재확인하여 닫힌 hover가 조건 회복 뒤 다시 열릴 수 있다.
  UIManager는 이미 표시 중인 같은 source를 재개방하지 않는다.
- PointerClickRouter는 press 시점의 EventSystem raycast 중 GraphicRaycaster hit를 확인해 UI 뒤의 world 클릭을 차단한다. native 입력 순서·화면 회귀는 별도 Play 검증 대상이다.
- concrete popup은 MerchantPopup, TownHallPopup, SeedSelectionPopup, ConstructionPopup, WarehousePopup, HousePopup 여섯 종류다. 건설 UI는 [Construction UI](Construction/UI.md), 창고 UI는 [Warehouse UI](Inventory_and_Items/Warehouse_UI.md), 주택 UI는 [Housing UI](Housing/UI.md)가 주 소유한다. PopupType.House=6을 기존 값 뒤에 추가한다. 공통 UI는 source만 전달하며 씨앗 transaction은 [농장 runtime](Farming/Runtime_and_Transactions.md)이 소유한다.

## 관련 문서

- [Farming](Farming/README.md)
- [Interaction and Destinations](Interaction_and_Destinations.md)
- [Merchant Caravan](Merchant_Caravan.md) — `MerchantPopup`과 드래그 앤 드롭 슬롯 UI의 주 소유 문서
- [Town Hall](Town_Hall.md) — `TownHallPopup`의 주 소유 문서

## 문서 갱신 조건

UI facade, registry, hover 입력, view lifecycle 또는 domain-to-UI 계약이 바뀌면 갱신한다.
