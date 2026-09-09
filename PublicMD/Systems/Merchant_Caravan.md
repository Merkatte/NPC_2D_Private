# Merchant Caravan

## 기능 목적과 책임 경계

주기적으로 등장·체류·퇴장하는 상단(캐러밴)의 **방문 연출**과, 클릭으로 여는 거래 UI가 실제로 골드를 주고받는 transaction을 설명한다. 골드 로드맵(골드 → **상단** → 후보군)의 2단계다.

상단의 건물·상인·새 3마리는 게임플레이 NPC가 아니라 하나의 방문 연출 세트다. `WorkerNPC`/`IAction`/selector/action queue/`NPCType`을 거치지 않고, 범용 AI 상태 머신이나 외부 event bus도 두지 않는다.

이 문서가 소유하지 않는 것: `GoldManager`(잔액 자체는 [Player Gold](Player_Gold.md) 소유), 클릭 감지 입력 adapter(`PointerClickRouter`/`IClickPopupSource`는 [UI](UI.md) 소유), item 가격 데이터 저장(`ItemInfo`/`ItemDataContext`는 [Inventory and Items](Inventory_and_Items.md) 소유 — 이 문서는 그 소비자일 뿐이다).

핵심 분리: `MerchantVisual`(클릭 표면)과 `MerchantTradeSite`(거래 도메인)는 서로를 전혀 모른다. 유일한 연결점은 `PopupType.Merchant`로 여는 `MerchantPopup`이다. `MerchantCaravan`(방문 연출 director)도 `GoldManager`나 거래 로직을 모른다 — "언제 거래 가능해지는지"만 `MerchantVisual.SetTradeAvailable(bool)`로 알릴 뿐, "무엇을 살 수 있는지"는 전혀 모른다.

## 현재 실행 흐름

```text
MerchantArrivalScheduler (방문 간 간격 타이머, 상단 체류 중엔 카운트 안 함)
  -> MerchantCaravan.BeginVisit()
     -> SetActive(true)가 먼저 (비활성 상태로 시작하는 캐러밴의 Awake를 동기 실행시키기 위해)
     -> 이미 방문 중(IsVisiting)이면 no-op
     -> VisitRoutine() 시작 (단일 Coroutine)

VisitRoutine():
  ResetPresentation()                    전 방문의 위치·회전·Animator 상태를 완전히 제거
    -> 건물 단독 하강 (Vector3.LerpUnclamped + AnimationCurve)
    -> 새 3마리 + 상인 동시 착륙 (상인 Z축 -360° 회전, 공유 진행도로 한 Coroutine이 4개 Transform 갱신)
    -> MerchantVisual.SetTradeAvailable(true)         <- 여기서부터 거래 가능, 체류 타이머 시작
    -> 새 3마리 각자 랜덤 지상 연출 시작 (서기/걷기/모이 먹기, TransportBird 소유)
    -> WaitForSeconds(dwellDuration)
    -> MerchantVisual.SetTradeAvailable(false)         <- 즉시 거래 불가 + 열린 팝업 자동 닫힘
    -> 새 3마리 랜덤 연출 중단
    -> 새 3마리 각자 지상 앵커로 집합 (논리적 역순 연출, 녹화 애니메이션의 역재생 아님)
    -> 새 3마리 + 상인 동시 이륙 (상인 Z축 +360° 회전)
    -> 건물 단독 상승
    -> SetActive(false)

클릭(체류 중에만 성립):
PointerClickRouter -> MerchantVisual.TryGetClickPopup()
  -> MerchantCaravan이 SetTradeAvailable로 설정한 bool을 그대로 반환 (Animator 상태 조회 아님)
  -> IUIService.TryShow(PopupType.Merchant) -> MerchantPopup 오픈

거래(판매 탭, 드래그 앤 드롭 + 일괄 확정):
MerchantPopup.OnOpened() -> MerchantTradeSite.GetAvailableOffers()
  -> CropCatalog.Definitions 순회, WarehouseInventory.GetQuantity > 0 그리고 SellPrice > 0인 것만
  -> IDataManager.TryGetItemInfo로 표시명·가격 join
  -> 창고 슬롯 그리드로 표시(칸 하나 = 아이템 한 종류)

창고 슬롯을 판매칸에 드래그:
ItemSlotDragHandle(창고 슬롯) -> DragGhostView로 커서 추적
  -> SellCartDropZone.OnDrop(드롭 소스가 ItemSlotDragHandle을 가진 진짜 창고 슬롯인지 확인)
  -> MerchantPopup.CanAcceptDrop/RequestAddToCart
  -> 남은 수량이 1개면 즉시 카트에 담김, 2개 이상이면 QuantityPromptPanel(Slider+InputField)이 열려 수량을 정함
  -> MerchantPopup의 로컬 Dictionary<itemId, quantity>(판매 대기 카트)에 누적 — 도메인은 이 상태를 모른다

판매 확정(일괄):
MerchantPopup.ConfirmSell() -> MerchantTradeSite.TryTrade(pendingSales: Dictionary<int,int>)
  -> TryComputeTotal: CropCatalog 기준 판매 가능 품목인지 자체 재검증
     + IDataManager.TryGetItemInfo로 각 줄 가격 재조회(Popup이 넘긴 값 불신)
     + long 승격 계산으로 곱셈·합계 오버플로 방지, SellPrice<=0인 줄은 개별 거절
  -> 창고 차감 전 (long)GoldManager.CurrentGold + total <= int.MaxValue 검증(골드 포화로 인한 불일치 방지)
  -> WarehouseInventory.TryRemoveBatch(pendingSales) — all-or-nothing, 실패 시 창고·골드 모두 무변경
  -> 성공 시 GoldManager.Add(total) 합계 1회 지급
  -> TradeResult(Success/OutOfStock/InvalidRequest) 반환
     -> OutOfStock이면 Popup이 카트를 최신 재고 이내로 자동 정리, InvalidRequest면 카트를 그대로 둔다
        (재고와 무관한 실패까지 재고 기준으로 정리하지 않기 위함)

MerchantTradeSite.TryGetSaleQuote(pendingSales)는 위와 같은 검증 로직(TryComputeTotal)을 공유하는 순수 조회로,
Popup이 카트 변경마다 예상 판매 금액을 되묻는 데 쓴다(mutate 없음, 빈 카트는 0으로 성공).
```

`MerchantVisual`은 Animator를 소유하지 않는다. "언제 거래 가능한지"는 `MerchantCaravan`(방문 Coroutine)이 명시적으로 `SetTradeAvailable(bool)`을 호출해 알리고, `MerchantVisual`은 그 bool을 그대로 저장·반환하는 얇은 클릭 표면일 뿐이다.

건물·상인·새의 이동은 별도 tween 라이브러리 없이 `Coroutine` + `Vector3.LerpUnclamped`/`Vector3.MoveTowards` + `AnimationCurve`로 구현한다. 새 3마리와 상인의 동시 착륙·이륙은 `MerchantCaravan`의 한 Coroutine이 공유 진행도(t)로 여러 Transform을 같은 프레임에 갱신하는 방식이다(새 1마리당 별도 Coroutine을 만들지 않는다). 반면 체류 중 새 각자의 랜덤 배회는 `TransportBird` 개별 컴포넌트가 독립적으로 소유한다(서로 다른 타이밍으로 돌아야 하므로).

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Actor/MerchantCaravan.cs` | 방문 연출 director. 단일 방문 Coroutine(도착→체류→출발)과 착륙 pose 기준점 소유 |
| `Assets/Scripts/Actor/MerchantArrivalScheduler.cs` | 방문 간 간격 타이머(gap 기반, 겹침 불가) |
| `Assets/Scripts/System/Actor/MerchantVisual.cs` | 클릭 표면. `SetTradeAvailable(bool)`로 받은 상태만 저장·반환, 퇴장 시 팝업 자동 닫힘 |
| `Assets/Scripts/Actor/TransportBird.cs` | 새 한 마리의 Animator 표현 명령(Play*)과 체류 중 랜덤 지상 연출(Start/StopGroundPresentation) |
| `Assets/Scripts/Actor/MerchantTradeSite.cs` | 상단 거래만 담당하는 도메인 provider. `GoldManager`를 아는 유일한 컴포넌트. batch 판매 가격 검증(`TryComputeTotal`)과 판매 가능 품목 재검증(`IsSellableItem`)을 소유 |
| `Assets/Data/Struct/MerchantOffer.cs` | Popup에 전달하는 판매 가능 품목 표시 값 |
| `Assets/Scripts/Enum/TradeResult.cs` | 거래 결과(Success/OutOfStock/InvalidRequest) |
| `Assets/Scripts/UI/MerchantPopup.cs` | 구매/판매 탭을 가진 거래 UI. 판매 대기 카트(로컬 `Dictionary<itemId,quantity>`)와 세션 상태의 유일한 소유자 |
| `Assets/Scripts/UI/ItemSlotView.cs` | 슬롯 한 칸의 표시 전담(이름·수량·단가·아이콘). 창고 칸/판매 칸이 공유 |
| `Assets/Scripts/UI/ItemSlotDragHandle.cs` | 창고 슬롯의 드래그 소스 — 카트를 모르고 고스트만 다룬다 |
| `Assets/Scripts/UI/CartSlotRemoveHandle.cs` | 판매칸 슬롯의 클릭 제거 |
| `Assets/Scripts/UI/SellCartDropZone.cs` | 판매칸 패널의 드롭 타깃 — 드롭 소스가 진짜 창고 슬롯인지까지 확인 후 Popup에 의도 전달 |
| `Assets/Scripts/UI/DragGhostView.cs` | 드래그 중 커서를 따라다니는 단일 재사용 고스트 |
| `Assets/Scripts/UI/QuantityPromptPanel.cs` | "몇 개를 옮길까요?" 수량 입력 서브패널(Slider+InputField 동기화). `MerchantPopup`의 자식이라 별도 `PopupType` 등록 없음 |

`MerchantVisitPhase.cs`(전용 phase enum)는 2026-09-10에 제거했다 — 외부에 노출할 필요가 없고 방문 Coroutine의 순차 흐름만으로 충분했다.

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 방문 주기 | `MerchantArrivalScheduler.cs` |
| 연출 순서·타이밍·높이 | `MerchantCaravan.cs`(`VisitRoutine`, tuning field) |
| 새 지상 연출 | `TransportBird.cs` |
| 거래 로직·가격·batch 검증 | `MerchantTradeSite.cs`(`TryComputeTotal`/`IsSellableItem`), [Inventory and Items](Inventory_and_Items.md)의 `IDataManager`/`WarehouseInventory.TryRemoveBatch` |
| 거래 가능 판정 시점 | `MerchantCaravan.cs`의 `SetTradeAvailable` 호출 지점, `MerchantVisual.cs` |
| 판매 UI(슬롯·카트·드래그·수량입력) | `MerchantPopup.cs`, `ItemSlotView.cs`, `ItemSlotDragHandle.cs`, `CartSlotRemoveHandle.cs`, `SellCartDropZone.cs`, `DragGhostView.cs`, `QuantityPromptPanel.cs`, [UI](UI.md) |
| 클릭 감지 | [UI](UI.md)의 `PointerClickRouter`/`IClickPopupSource` |
| 새 gold 소비·획득 지점 추가 | [Player Gold](Player_Gold.md) |

## 불변 규칙

- `MerchantCaravan`은 `GoldManager`나 거래 로직을 모른다. `MerchantVisual`도 마찬가지다. `MerchantTradeSite`만 `GoldManager`를 안다.
- `MerchantTradeSite`는 상단 거래만 담당한다. 나중에 모집·업그레이드가 생겨도 각자 자기 전용의 작은 provider를 새로 만들고 이 컴포넌트를 거치지 않는다 — "모든 거래의 총괄자" 역할을 만들지 않는다.
- 체류(dwell) 타이머를 건드리는 외부 메서드는 없다. 상호작용으로 리셋되거나 연장되지 않는다.
- 거래 가능 여부는 `MerchantCaravan`이 착륙 완료/출발 시작 시점에 `MerchantVisual.SetTradeAvailable(bool)`로 명시 설정한다. Animator 상태를 실시간 조회하지 않는다(이전 방식) — `MerchantVisual`은 더 이상 Animator를 소유하지 않는다.
- `Popup`은 가격을 계산·지급에 쓰지 않는다. `TryTrade`/`TryGetSaleQuote`에 itemId·수량 의도(카트)만 전달하고, 실제 가격 계산·검증은 `MerchantTradeSite`가 그 순간 다시 한다 — `GetAvailableOffers()`가 주는 `MerchantOffer.UnitPrice`는 창고 슬롯에 단가를 찍는 표시 전용 스냅샷일 뿐, 이걸로 합계를 직접 계산하거나 트랜잭션에 쓰지 않는다.
- `MerchantTradeSite`는 판매 가능 품목(`CropCatalog.Definitions` 기준)을 스스로 재검증한다. UI가 보낸 itemId를 신뢰하지 않는다.
- 창고를 차감하기 전에 골드 상한(`int.MaxValue`) 초과 여부를 검증한다 — `GoldManager.Add`가 포화되므로, 이 순서를 지키지 않으면 창고는 전량 빠지고 골드는 일부만 늘어나는 불일치가 생길 수 있다.
- `TryGetSaleQuote`와 `TryTrade`는 같은 가격·검증 로직(`TryComputeTotal`)을 공유한다 — 견적과 실제 거래 결과가 서로 다른 규칙으로 갈리지 않는다.
- `MerchantPopup`-`MerchantTradeSite`는 1:1 전용 배선이라 interface로 감싸지 않는다(concrete 참조) — 대체 구현이나 테스트 필요가 없다. 판매 대기 카트를 런타임에 생성한 슬롯 컴포넌트(`ItemSlotDragHandle`/`CartSlotRemoveHandle`)에 연결할 때도 직렬화가 아니라 `Initialize(...)` 런타임 주입을 쓴다 — 프리팹 자산은 자신을 생성한 팝업 인스턴스를 미리 참조할 수 없기 때문이다.
- 상단은 `NPCType`/`NPCManager`/`WorkerPool`을 거치지 않는다. Worker NPC 역할이 아니다.
- 상단은 pooling하지 않는다. 항상 하나뿐이라 씬에 상주하는 단일 GameObject를 `SetActive`로 껐다 켠다.
- 위치는 씬에 배치하는 빈 Transform이 아니라 **프리팹 local position**으로 표현한다. 프리팹에 저작된 건물/상인/새의 local position이 곧 착륙(landed) pose이고, 프리팹 인스턴스의 월드 위치가 최종 착륙 기준점이다. 필요한 높이(lift height)와 duration은 `MerchantCaravan`의 private serialized 값으로 둔다.
- 새·상인의 현재 연출 상태(어떤 phase인지, 어떤 모션인지)를 외부에 공개하는 property는 두지 않는다. `TransportBird`의 공개 API는 명령형(`PlayFlying()` 등)뿐이고 조회형이 없다.
- 체류 중 새의 랜덤 배회는 gameplay 결과에 영향이 없는 순수 표현이라 `IRandomSource` 주입 없이 `UnityEngine.Random`을 직접 쓴다(`CodeConvention.md` §12.1의 production gameplay 난수 규칙 예외 — 코드 주석에 이유를 남겨둔다).

## Unity 배선과 검증 도구

**2026-09-10 기준**, `FarmerTest.unity`에는 연출 검증용으로 배치해 둔 `MerchantCaravan`/`MerchantArrivalScheduler` 인스턴스가 있다. `MerchantTradeSite`/`PointerClickRouter`/`MerchantPopup`은 아직 어느 씬에도 없다 — 거래 UI 프리팹 3종(`MerchantPopup.prefab`/`MerchantItemSlot.prefab`/`MerchantCartSlot.prefab`)은 완성됐지만 씬에 배치되지 않았다. 씬 배선은 사용자가 에디터에서 직접 수행한다(미커밋 변경이 많은 `FarmerTest.unity`를 이 저장소 작업에서 스크립트로 건드리지 않기 위함).

씬 배선 체크리스트:

1. `MerchantCaravan.prefab` 인스턴스가 이미 있으면 재사용, 없으면 배치(비활성 상태로 시작). **인스턴스의 월드 위치가 최종 착륙 기준점이다.**
2. `MerchantVisual._uiServiceSource` ← 씬의 `UIManager`
3. `MerchantArrivalScheduler`(항상 활성인 GameObject)의 `_caravan` ← 캐러밴 인스턴스가 연결돼 있는지 확인
4. `MerchantTradeSite` 배치, `_warehouse`/`_cropCatalog`/`_goldManager`/`_dataManagerSource` 연결
5. `PointerClickRouter` 배치, `_worldCamera`/`_uiServiceSource`/`_clickableMask` 연결
6. `ProjectSettings/TagManager.asset`의 **Layer 9가 현재 무명**이다. `MerchantCaravan.prefab` 루트는 `m_Layer: 9`로 저작되어 있으므로, 이 레이어에 이름(예: `Clickable`)을 부여하고 `PointerClickRouter._clickableMask`와 짝을 맞춰야 클릭이 동작한다.
7. Canvas(Overlay) 아래에 `MerchantPopup.prefab` 인스턴스 배치(비활성 상태로 시작). `_tradeSite` ← 씬의 `MerchantTradeSite`, `_itemIcons`에 Carrot(4)/Potato(5) 스프라이트(`ui-item-carrot.png`/`ui-item-potato.png`, 이미 Sprite로 import돼 있음) 등록. `UIManager._popups`에 등록해야 팝업이 실제로 열린다(`PopupType.Merchant`는 기존 값 그대로).
8. `EventSystem`/`InputSystemUIInputModule`/Canvas의 `GraphicRaycaster`는 `FarmerTest.unity`에 이미 있음 — 확인만.

`GuardTest.unity`는 아직 배선하지 않았다.

## 알려진 제약과 TBD

- 새 3마리(및 마차/끈)의 실제 최종 그림이 아직 붙지 않았을 수 있다(스프라이트 자체는 `Shop`/`Merchant`/`TransportBird`에 배선되어 있으나 lift height·duration·지상 착륙 좌표는 프레임 안에서 육안으로 재조정이 필요한 초기 추정값이다).
- 착지 순간 "쿵" 사운드/먼지 파티클을 위한 Animation Event 배선은 제거했다(Animator 자체가 제거됨). 프로젝트에 audio/particle 시스템이 생기면 `MerchantCaravan.MoveBuilding` 완료 시점에 새로 연결한다.
- 구매(Buy) 기능은 없다. `TradeResult`에 `InsufficientGold`가 없는 이유이기도 하다 — 지금은 판매(창고→골드)만 있어 골드 부족이라는 결과 자체가 발생할 수 없다. 구매가 생기면 그때 enum 끝에 추가한다. `MerchantPopup`의 구매 탭은 "준비중입니다." placeholder뿐이다.
- 열린 팝업 위에서 world 클릭이 그대로 통과하는 문제는 [UI](UI.md)의 TBD를 따른다. uGUI `EventSystem` 드래그 경로와 `PointerClickRouter`의 world 경로는 별개 입력 계통이라 이번 작업이 새로 만든 문제는 아니다.
- 창고 재고 열거는 `CropCatalog.Definitions`를 순회하는 방식이라, farm 산출물이 아닌 item(예: Pub의 Water/Beer/Bread)은 애초에 판매 목록에 나타나지 않는다 — 창고에 쌓이지 않는 item이라 의도된 동작이다.
- Bird 3마리의 지상 착륙 local position, 각 lift height/duration은 Unity 에디터 육안 검증 후 확정해야 하는 TBD다.
- `MerchantPopup`/`MerchantItemSlot`/`MerchantCartSlot` 프리팹의 실제 레이아웃(칸 크기, 색상, `GridLayoutGroup` 셀 크기)은 디자인 영역이라 수치를 못 박지 않았다 — 에디터에서 사용자가 조정.
- 단일 `WarehouseInventory.TryRemove(int,int,out int)`/`MerchantTradeSite.TryTrade(int,int)`는 batch API로 교체되며 삭제됐다(breaking change, 이전 문서 기술과 다름).
- 드래그 앤 드롭 팝업의 실제 Play Mode 검증(첫 오픈 슬롯 채움, 재오픈 시 카트 리셋, 드래그 중 상단 퇴장 시 고스트 정리 등)은 이 저장소 작업에서 수행하지 못했다 — Unity 에디터에서 사용자가 확인해야 하는 TBD. batch 거래 트랜잭션 자체는 `Assets/TestOnly/TestMerchantBatchTradeProbe.cs`로 자동 검증 가능하다(§Unity 배선과 검증 도구에 언급 없던 신규 도구, `TestDecisionScenarioProbe.cs`와 같은 IMGUI PASS/FAIL 패턴).

## 관련 문서

- [Player Gold](Player_Gold.md) — 골드 잔액과 획득·지출 계약
- [Inventory and Items](Inventory_and_Items.md) — `WarehouseInventory.TryRemoveBatch`, `IDataManager.TryGetItemInfo`, item 가격 데이터
- [UI](UI.md) — `PointerClickRouter`, `IClickPopupSource`, `PopBase`/`MerchantPopup`, uGUI 드래그 앤 드롭
- [NPC Presentation](NPC_Presentation.md) — Animator 기반 표현의 선례(`CarryVisualPresenter`)
- [Spawning and Pooling](Spawning_and_Pooling.md) — 상단이 이 체계를 따르지 않는 이유(Worker 역할이 아님)

## 문서 갱신 조건

방문 연출 순서·타이밍, 클릭 가능 판정, 거래 계약(`TryTrade`/`TradeResult`), Unity 배선이 바뀌면 갱신한다.
