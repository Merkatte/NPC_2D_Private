# Merchant Caravan

## 기능 목적과 책임 경계

주기적으로 등장·체류·퇴장하는 상단(캐러밴) 액터와, 클릭으로 여는 거래 UI가 실제로 골드를 주고받는 transaction을 설명한다. 골드 로드맵(골드 → **상단** → 후보군)의 2단계다.

이 문서가 소유하지 않는 것: `GoldManager`(잔액 자체는 [Player Gold](Player_Gold.md) 소유), 클릭 감지 입력 adapter(`PointerClickRouter`/`IClickPopupSource`는 [UI](UI.md) 소유), item 가격 데이터 저장(`ItemInfo`/`ItemDataContext`는 [Inventory and Items](Inventory_and_Items.md) 소유 — 이 문서는 그 소비자일 뿐이다).

핵심 분리: `MerchantVisual`(표현·클릭 표면)과 `MerchantTradeSite`(거래 도메인)는 서로를 전혀 모른다. 유일한 연결점은 `PopupType.Merchant`로 여는 `MerchantPopup`이다. `MerchantCaravan`(상태·이동)도 `GoldManager`나 거래 로직을 모른다 — "언제 상태가 바뀌는지"만 소유하고 "무엇을 살 수 있는지"는 전혀 모른다.

## 현재 실행 흐름

```text
MerchantArrivalScheduler (방문 간 간격 타이머, 상단 체류 중엔 카운트 안 함)
  -> MerchantCaravan.BeginVisit()
     -> Approaching: Vector3.MoveTowards(dockPoint) 도달까지
     -> Landed: 고정 dwell 타이머(외부에서 리셋 불가) 소진까지, MerchantVisual.SetLanded(true)
     -> Departing: departurePoint 도달까지, MerchantVisual.SetLanded(false) -> IUIService.TryHide(Merchant)
     -> Away: SetActive(false)

클릭(체류 중에만 성립):
PointerClickRouter -> MerchantVisual.TryGetClickPopup()
  -> Animator가 Landed 상태이고 전이 중이 아닌지 실시간 조회(캐시된 bool 아님)
  -> IUIService.TryShow(PopupType.Merchant) -> MerchantPopup 오픈

거래:
MerchantPopup.OnOpened() -> MerchantTradeSite.GetAvailableOffers()
  -> CropCatalog.Definitions 순회, WarehouseInventory.GetQuantity > 0인 것만
  -> IDataManager.TryGetItemInfo로 표시명·가격 join
MerchantPopup.TrySell(itemId, quantity) -> MerchantTradeSite.TryTrade(itemId, quantity)
  -> IDataManager.TryGetItemInfo로 가격 재조회(Popup이 넘긴 값 불신)
  -> WarehouseInventory.TryRemove(itemId, quantity) — all-or-nothing
  -> GoldManager.Add(price * quantity)
  -> TradeResult(Success/OutOfStock/InvalidRequest) 반환
```

`MerchantVisual`은 Animator 하나만 소유한다(파라미터 `IsLanded`, 상태 `Flying`(default)→`Landing`→`Landed`→`TakingOff`). "언제 상태가 바뀌는지"는 `MerchantCaravan`(C#)이, "지금 어떻게 보이는지"는 Animator가 소유하는 분리 덕분에, 클릭 게이트가 Animator 상태를 조회하는 것만으로 착지/이륙 애니메이션 중의 클릭을 자동으로 막는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Actor/MerchantCaravan.cs` | 방문 phase 상태머신과 도착점 사이 이동 |
| `Assets/Scripts/Actor/MerchantVisitPhase.cs` | `MerchantCaravan` 전용 phase enum(직렬화 안 됨) |
| `Assets/Scripts/Actor/MerchantArrivalScheduler.cs` | 방문 간 간격 타이머(gap 기반, 겹침 불가) |
| `Assets/Scripts/System/Actor/MerchantVisual.cs` | 상단 Animator 소유, 클릭 가능 여부를 Animator 상태로 판정, 퇴장 시 팝업 자동 닫힘 |
| `Assets/Scripts/Actor/MerchantTradeSite.cs` | 상단 거래만 담당하는 도메인 provider. `GoldManager`를 아는 유일한 컴포넌트 |
| `Assets/Data/Struct/MerchantOffer.cs` | Popup에 전달하는 판매 가능 품목 표시 값 |
| `Assets/Scripts/Enum/TradeResult.cs` | 거래 결과(Success/OutOfStock/InvalidRequest) |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 방문 주기·체류 시간 | `MerchantCaravan.cs`, `MerchantArrivalScheduler.cs` |
| 착지 판정·연출 | `MerchantVisual.cs`, `Assets/Animation/Merchant/Merchant.controller` |
| 거래 로직·가격 | `MerchantTradeSite.cs`, [Inventory and Items](Inventory_and_Items.md)의 `IDataManager`/`WarehouseInventory.TryRemove` |
| 팝업 표시 | `Assets/Scripts/UI/MerchantPopup.cs`, [UI](UI.md) |
| 클릭 감지 | [UI](UI.md)의 `PointerClickRouter`/`IClickPopupSource` |
| 새 gold 소비·획득 지점 추가 | [Player Gold](Player_Gold.md) |

## 불변 규칙

- `MerchantCaravan`은 `GoldManager`나 거래 로직을 모른다. `MerchantVisual`도 마찬가지다. `MerchantTradeSite`만 `GoldManager`를 안다.
- `MerchantTradeSite`는 상단 거래만 담당한다. 나중에 모집·업그레이드가 생겨도 각자 자기 전용의 작은 provider를 새로 만들고 이 컴포넌트를 거치지 않는다 — "모든 거래의 총괄자" 역할을 만들지 않는다.
- 체류(dwell) 타이머를 건드리는 외부 메서드는 없다. 상호작용으로 리셋되거나 연장되지 않는다.
- 클릭 가능 여부는 매번 Animator 상태를 실시간 조회해서 판정한다. 캐시된 bool이나 Animation Event로 설정한 플래그가 아니다.
- `Popup`은 가격을 계산하거나 캐시하지 않는다. `TryTrade`에 itemId·수량 의도만 전달하고, 실제 가격은 `MerchantTradeSite`가 그 순간 다시 조회한다.
- `MerchantPopup`-`MerchantTradeSite`는 1:1 전용 배선이라 interface로 감싸지 않는다(concrete 참조) — 대체 구현이나 테스트 필요가 없다.
- 상단은 `NPCType`/`NPCManager`/`WorkerPool`을 거치지 않는다. Worker NPC 역할이 아니다.
- 상단은 pooling하지 않는다. 항상 하나뿐이라 씬에 상주하는 단일 GameObject를 `SetActive`로 껐다 켠다.

## Unity 배선과 검증 도구

`FarmerTest.unity`에 `MerchantCaravan` 프리팹 인스턴스, `MerchantArrivalScheduler`(`_caravan` 연결), `MerchantTradeSite`(`_warehouse`/`_cropCatalog`/`_goldManager`/`_dataManagerSource` 연결)를 배치한다. `MerchantVisual._uiServiceSource`는 씬의 `UIManager`를 가리켜야 퇴장 시 팝업이 자동으로 닫힌다. `_arrivalPoint`/`_dockPoint`/`_departurePoint`는 프리팹이 아니라 각 씬에 배치하는 빈 Transform이다(위치가 씬마다 다르므로 프리팹에 베이킹하지 않는다).

클릭이 실제로 동작하려면 [UI](UI.md)의 `PointerClickRouter`가 씬에 배선되어 있고, `MerchantCaravan` 프리팹의 콜라이더가 `Clickable` 레이어에 있어야 한다.

`Assets/Animation/Merchant/Merchant.controller`와 4개 clip(`MerchantFlying/Landing/Landed/TakingOff.anim`)은 전부 `Visual` 자식 Transform의 `localPosition`/`localScale`만 임시 스케일 자리표시자로 애니메이션한다(픽셀아트 스프라이트 도착 전). `MerchantCaravan.prefab`의 `Visual` 자식에는 아직 `SpriteRenderer`가 없다 — 실제 그림이 도착하면 이 자식(또는 그 아래)에 스프라이트를 추가하면 되고, Animator 배선은 바뀔 필요가 없다.

`GuardTest.unity`는 아직 배선하지 않았다(2026-09-06 기준).

## 알려진 제약과 TBD

- 새 3마리(및 마차/끈)의 실제 그림이 없다. 지금 Animator는 `Visual` 하나만 스케일로 움직이는 자리표시자다. 착지 후 개별 새의 좌우 살짝 보기, 랜덤 idle 여부와 타이밍은 그림 도착 후 재논의한다 — 한 Animator가 전부 바인딩하는 지금 구조로 충분한지, 새 한 마리씩 독립적인 런타임 랜덤이 필요한지 아직 결정하지 않았다.
- 착지 순간 "쿵" 사운드/먼지 파티클을 위한 Animation Event(`MerchantVisual.OnLandingImpact`, `MerchantLanding.anim`의 0.12초 지점)가 이미 배선되어 있지만, 프로젝트에 아직 audio/particle 시스템이 없어 메서드 본문이 비어 있다.
- 구매(Buy) 기능은 없다. `TradeResult`에 `InsufficientGold`가 없는 이유이기도 하다 — 지금은 판매(창고→골드)만 있어 골드 부족이라는 결과 자체가 발생할 수 없다. 구매가 생기면 그때 enum 끝에 추가한다.
- 열린 팝업 위에서 world 클릭이 그대로 통과하는 문제는 [UI](UI.md)의 TBD를 따른다.
- 창고 재고 열거는 `CropCatalog.Definitions`를 순회하는 방식이라, farm 산출물이 아닌 item(예: Pub의 Water/Beer/Bread)은 애초에 판매 목록에 나타나지 않는다 — 창고에 쌓이지 않는 item이라 의도된 동작이다.

## 관련 문서

- [Player Gold](Player_Gold.md) — 골드 잔액과 획득·지출 계약
- [Inventory and Items](Inventory_and_Items.md) — `WarehouseInventory.TryRemove`, `IDataManager.TryGetItemInfo`, item 가격 데이터
- [UI](UI.md) — `PointerClickRouter`, `IClickPopupSource`, `PopBase`/`MerchantPopup`
- [NPC Presentation](NPC_Presentation.md) — Animator 기반 표현의 선례(`CarryVisualPresenter`)
- [Spawning and Pooling](Spawning_and_Pooling.md) — 상단이 이 체계를 따르지 않는 이유(Worker 역할이 아님)

## 문서 갱신 조건

방문 phase·타이머 규칙, 클릭 가능 판정, 거래 계약(`TryTrade`/`TradeResult`), Unity 배선이 바뀌면 갱신한다.
