# Code Evaluation Result

## Purpose

`3f652bd`의 Merchant Caravan 클릭→팝업→판매→골드 변경을 대상으로 책임 배치, runtime lifecycle, transaction 안전성, Unity 직렬화 배선, 코드 규약과 문서 정합성을 감사했다. 사용자와 합의된 concrete `MerchantPopup`–`MerchantTradeSite` 관계, `WarehouseInventory.TryRemove`의 interface 제외, Animator 실시간 클릭 판정, `DataManager`의 immutable-data facade 역할은 설계 전제로 존중했다.

## Review Snapshot

- Date: 2026-09-06
- Commit: `3f652bd Add merchant caravan trading flow`
- Scope:
  - 변경된 production C# 16개
  - Merchant prefab, Animator Controller와 animation clip 4개 및 `.meta`
  - `ItemData.csv`, crop/item assets
  - 직접 의존 표면: `UIManager`, `PopBase`, `PointerHoverRouter`, `IUIService`, `GoldManager`, `ItemDataContext`, `CropCatalog`, inventory 계약
  - `FarmerTest.unity`, `GuardTest.unity`, `ProjectSettings/TagManager.asset`
  - 관련 Systems 문서와 기존 `Code_Evaluation_Result.md`
- Standards:
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `reviewing-npc-work-code` audit methodology
- Verification:
  - 최초 dirty 상태와 커밋 후 clean 상태의 `git status` 확인
  - `git diff --name-status HEAD^ HEAD`, `git diff --check HEAD^ HEAD`
  - 호출자·interface 구현체·enum·scene GUID·금지 패턴 검색
  - prefab/controller/clip 내부 fileID 및 cross-asset GUID 검증
  - `Visual` CRC32와 animation binding 대조
  - 문서 상대 링크 확인

## Executive Summary

책임 배치와 의존 방향은 전반적으로 좋다. `MerchantCaravan`, `MerchantVisual`, `MerchantTradeSite`, 입력 router와 popup의 역할이 분리되어 있고, 거래 가격을 domain에서 다시 조회하는 구조도 적절하다. 요청에서 명시한 deliberate architecture 선택에는 finding이 없다.

그러나 현재 구현은 end-to-end 기능으로 승인할 수 없다. 비활성 Merchant prefab은 `Awake()` 전에 `_isConfigured`를 요구하므로 최초 방문을 스스로 시작할 수 없고, `MerchantPopup`에는 실제 판매 버튼을 `TrySell(int, int)`에 연결할 수 있는 binding 경로가 없다. 여기에 거래 금액 overflow, 판매 허용 품목 검증 누락, 퇴장 프레임의 Animator 평가 race가 존재한다.

최종 판정은 **Changes requested**이다.

## Improvements Since Previous Review

- 이전 Gold 단계와 비교해 실제 gameplay 골드 획득 owner가 `MerchantTradeSite`로 좁게 추가됐다.
- `GoldManager`에는 가격·재고·UI 책임이 유입되지 않았다.
- 이전 보고서의 Gold scene 미배선 문제는 아직 해소되지 않았으며 이번 M-01에 포함했다.
- 새 기능 routing, Systems 문서, enum append, `.meta`와 asset reference는 체계적으로 추가됐다.

## Priority Coverage

| Priority | Result |
|---|---|
| 1. Architecture and responsibility placement | **Code architecture issue 없음.** deliberate concrete pairing, inventory capability 분리, GoldManager 참조 위치가 문서화된 책임과 일치한다. 문서 drift는 L-01에서 별도 지적한다. |
| 2. Correctness, lifecycle, cancellation, regression | H-01, H-02, M-02, M-03, M-04 발견. |
| 3. Unity scene, prefab, serialized-reference safety | M-01, M-05 발견. |
| 4. Convention, maintainability, dead code, magic values | L-01, L-02 발견. 익명 gameplay magic value, production dead code, enum 재정렬은 발견되지 않았다. |

## Findings By Severity

### Critical

None found.

### High

#### H-01 — 비활성 prefab은 최초 방문 전에 `Awake()`를 실행할 수 없어 bootstrap이 교착된다

- Severity: High
- Category: Unity lifecycle / Runtime correctness
- Location:
  - `Assets/Prefab/InGame/MerchantCaravan.prefab:22`
  - `Assets/Scripts/Actor/MerchantCaravan.cs:25,32-54`
  - `Assets/Scripts/Actor/MerchantArrivalScheduler.cs:34-45`
- Evidence:
  - prefab root는 `m_IsActive: 0`이다.
  - `_isConfigured`는 기본값 `false`이며 `MerchantCaravan.Awake()`에서만 `true`가 된다.
  - 비활성 GameObject의 `Awake()`는 활성화될 때까지 실행되지 않는다.
  - `BeginVisit()`은 `_isConfigured == false`이면 `gameObject.SetActive(true)`보다 먼저 반환한다.
  - scheduler가 호출할 수 있는 활성화 경로는 이 `BeginVisit()`뿐이다.
- Description:
  - 문서대로 비활성 prefab instance를 scheduler에 연결하면 최초 interval이 끝나도 활성화되지 않는다. 이후 scheduler가 재시도해도 `_isConfigured`는 계속 false여서 모든 방문이 실패한다.
- Recommended fix:
  - 방문 lifecycle owner를 항상 활성 상태로 유지하고 visual/collider child만 비활성화하거나, 최초 `Awake()`가 반드시 실행되는 bootstrap 상태를 명시적으로 제공한다.
  - 어떤 방식을 택하든 `BeginVisit()`이 자기 활성화 이후에만 설정될 수 있는 flag를 활성화 전제조건으로 요구하지 않게 한다.
  - inactive initial state와 두 번째 방문을 모두 Play Mode에서 검증한다.
- Impact if unfixed:
  - Merchant가 한 번도 등장하지 않아 클릭, popup, 거래와 애니메이션 전체가 실행 불가능하다.

#### H-02 — `MerchantPopup`에 실제 판매 동작을 호출할 UI binding 경로가 없다

- Severity: High
- Category: Feature completeness / UI integration
- Location:
  - `Assets/Scripts/UI/MerchantPopup.cs:26-65`
  - `Assets/Scenes/FarmerTest.unity:5226`
- Evidence:
  - 판매 진입점은 `TrySell(int itemId, int quantity)` 하나뿐이다.
  - 표준 `Button.onClick`은 이 두 개의 정수 인자를 직접 전달할 수 없다.
  - repository 전체에서 `TrySell` 호출, `Button.onClick.AddListener`, offer-row binding 또는 zero-argument adapter가 없다.
  - `RefreshOffers()`는 판매 가능 항목을 `Text` 문자열로만 출력한다.
  - Merchant popup prefab/scene object가 없고 `UIManager._popups`도 빈 배열이다.
- Description:
  - scene에 popup과 Text를 수동 배선해도 사용자가 표시된 offer를 선택해 판매를 실행할 수 없다. 현재 연결 가능한 흐름은 popup 표시까지만이며 거래 호출에는 별도 코드가 필요하다.
- Recommended fix:
  - offer row가 `itemId`와 선택 수량을 보관하고 zero-argument click handler로 popup에 의도를 전달하게 하거나, row 생성 시 코드로 button listener를 바인딩한다.
  - 실제 MerchantPopup asset을 만들고 최소 한 개 offer에 대해 클릭→`TryTrade`→결과 갱신을 검증한다.
- Impact if unfixed:
  - popup은 열리더라도 플레이어가 판매를 실행할 수 없어 click-to-trade-to-gold 파이프라인이 완성되지 않는다.

### Medium

#### M-01 — 현재 repository에는 Merchant와 Gold runtime 배선이 전혀 없다

- Severity: Medium
- Category: Unity integration / Serialized-reference safety
- Location:
  - `Assets/Scenes/FarmerTest.unity`
  - `ProjectSettings/TagManager.asset:7-17`
  - `Assets/Prefab/InGame/MerchantCaravan.prefab:16,53-55,72`
- Evidence:
  - 신규 prefab, `MerchantArrivalScheduler`, `MerchantTradeSite`, `PointerClickRouter`, `MerchantPopup`, `GoldManager` GUID는 모든 scene에서 0건이다.
  - `UIManager._popups`는 빈 배열이고 `InputEvent`에는 Transform만 있다.
  - 기존 `DataManager`에는 `_itemDataContext` serialized row가 없다.
  - TagManager의 layer 9는 비어 있지만 Merchant prefab은 `m_Layer: 9`로 저장됐다.
  - prefab의 arrival/dock/departure/UI service 참조는 모두 null이다.
- Description:
  - 구현 요약이 이 상태를 NOT VERIFIED로 공개한 것은 정확하다. 다만 현재 저장소 자체로는 기능을 실행하거나 Play Mode에서 검증할 수 없으므로 완료 상태로 볼 수 없다.
- Recommended fix:
  - `Clickable` layer, click router, Merchant prefab instance, scheduler, trade site, popup registry/UI, GoldManager와 DataManager item context를 문서대로 배선한다.
  - H-01과 H-02를 먼저 해결한 뒤 missing script/reference와 Console 오류가 없는지 검증한다.
- Impact if unfixed:
  - 모든 Merchant code와 asset이 compile 대상에만 존재하고 gameplay에서는 도달 불가능하다.

#### M-02 — 거래 총액의 unchecked `int` 곱셈이 재고만 소모하는 성공 transaction을 만들 수 있다

- Severity: Medium
- Category: Transaction correctness / Integer overflow
- Location:
  - `Assets/Scripts/Actor/MerchantTradeSite.cs:73-88`
  - `Assets/Scripts/System/Inventory/WarehouseInventory.cs:8-23`
  - `Assets/Scripts/Manager/GoldManager.cs:35-41`
  - `Assets/Data/CSV/ItemData.csv:5`
- Evidence:
  - 거래 총액은 `info.SellPrice * quantity`로 `int` 안에서 계산된다.
  - overflow 검증은 재고를 제거한 뒤에도 없다.
  - 창고는 `int.MaxValue` 수량까지 정상적으로 보유할 수 있다.
  - 현재 Carrot 가격 2에서 `2 * int.MaxValue`는 unchecked 연산으로 `-2`가 된다.
  - `GoldManager.Add(-2)`는 아무 골드도 추가하지 않지만 `TryTrade()`는 `Success`를 반환한다.
- Description:
  - `GoldManager.Add` 내부의 `long` 포화 처리는 호출 전에 이미 overflow된 값을 복구할 수 없다. 유효한 API 입력만으로 재고 전량이 사라지고 골드는 증가하지 않는 경로가 존재한다.
- Recommended fix:
  - 재고 제거 전에 `(long)SellPrice * quantity`로 총액을 계산하고 가격·범위를 검증한다.
  - 총액 초과 시 transaction을 거부할지 `int.MaxValue`로 포화할지 계약을 정한 뒤, 내부 mutation 전에 결과를 확정한다.
- Impact if unfixed:
  - 큰 거래에서 재고 손실, 잘못된 골드 증가와 거짓 `Success` 결과가 발생할 수 있다.

#### M-03 — `TryTrade`가 표시 가격만 재검증하고 판매 허용 품목 자체는 검증하지 않는다

- Severity: Medium
- Category: Domain validation / Responsibility boundary
- Location:
  - `Assets/Scripts/Actor/MerchantTradeSite.cs:40-88`
  - `Assets/Scripts/System/Inventory/WarehouseInventory.cs:8-23`
  - `Assets/Data/CSV/ItemData.csv:2-6`
- Evidence:
  - `GetAvailableOffers()`는 `CropCatalog.Definitions`만 순회한다.
  - `TryTrade()`는 `_cropCatalog`를 전혀 사용하지 않고, item data와 창고 재고가 있으면 판매한다.
  - `WarehouseInventory.TryAdd()`는 모든 non-negative item ID를 수락한다.
  - Water/Beer/Bread는 item table에 존재하며 `SellPrice`가 0이다.
  - 해당 item이 창고에 들어간 뒤 `TryTrade()`에 전달되면 재고를 제거하고 골드 0을 추가한 뒤 `Success`를 반환한다.
- Description:
  - popup의 사전 필터는 read model일 뿐 domain transaction의 권한 검증을 대신할 수 없다. 가격은 재조회하지만, 요청된 item이 실제 Merchant offer인지와 양수 가격인지에 대해서는 popup 입력을 신뢰한다.
- Recommended fix:
  - mutation 전에 item이 현재 Merchant 판매 catalog에 속하는지와 `SellPrice > 0`인지 검증한다.
  - 허용되지 않은 item은 `InvalidRequest`로 반환하고 재고를 변경하지 않는다.
- Impact if unfixed:
  - 비판매 품목이나 잘못 구성된 0/음수 가격 품목이 소모되고도 골드가 지급되지 않을 수 있다.

#### M-04 — 퇴장 시작 프레임의 Update 순서에 따라 popup이 닫힌 직후 다시 열릴 수 있다

- Severity: Medium
- Category: Lifecycle race / Animator-state correctness
- Location:
  - `Assets/Scripts/Actor/MerchantCaravan.cs:71-78`
  - `Assets/Scripts/System/Actor/MerchantVisual.cs:51-80`
  - `Assets/Scripts/UI/PointerClickRouter.cs:48-64`
  - `Assets/Animation/Merchant/Merchant.controller:95-99`
- Evidence:
  - dwell 종료 시 `MerchantCaravan.Update()`가 `SetLanded(false)`를 호출한다.
  - 이 호출은 Animator bool을 변경하고 popup을 즉시 숨기지만 Animator를 즉시 평가하지 않는다.
  - click router도 독립적인 `Update()`에서 Animator의 현재 state와 transition을 조회한다.
  - 두 script에 execution order가 지정되지 않았다.
  - Animator transition duration은 0이며, 같은 코드의 `ResetToFlying()`은 즉시 반영을 위해 명시적으로 `Animator.Update(0f)`를 호출한다.
- Description:
  - Caravan Update가 먼저 실행된 프레임에 router가 뒤이어 클릭을 처리하면 Animator 평가 전의 `Landed` state와 `IsInTransition == false`를 읽을 수 있다. 이 경우 `TryHide` 이후 같은 프레임에 popup이 다시 열리고, 이후에는 다시 닫을 호출이 없다.
  - 이는 live Animator truth 선택 자체의 문제가 아니라 SetBool과 Animator 평가 사이의 시간 창 문제다.
- Recommended fix:
  - 퇴장 상태 변경을 반환하기 전에 Animator가 새 상태를 반영하도록 평가하거나, click query를 Animator 평가 이후 단계에서 수행해 기존 live-state 설계를 유지한다.
  - dwell 만료와 동일 프레임의 world click을 별도 Play Mode 시나리오로 검증한다.
- Impact if unfixed:
  - 드물게 상단이 이륙하거나 비활성화된 뒤에도 거래 popup이 남을 수 있다.

#### M-05 — popup 자동 닫힘의 필수 UI service 참조가 누락되어도 진단되지 않는다

- Severity: Medium
- Category: Serialized dependency safety
- Location:
  - `Assets/Scripts/System/Actor/MerchantVisual.cs:24-43,68-80`
  - `Assets/Prefab/InGame/MerchantCaravan.prefab:72`
- Evidence:
  - prefab의 `_uiServiceSource`는 null이다.
  - `Awake()`는 이를 `IUIService`로 cast하지만 null/잘못된 구현 여부를 검사하지 않는다.
  - `SetLanded(false)`는 `_uiService != null`일 때만 popup을 닫으며, 실패 로그나 안전한 interaction 차단이 없다.
  - 이 참조는 문서상 퇴장 시 popup을 닫는 유일한 seam이다.
- Description:
  - scene wiring에서 이 필드를 빠뜨리면 click router의 별도 UI service로 popup은 정상적으로 열리지만 Merchant가 떠날 때 닫히지 않는다. 결과가 정상처럼 보여 구성 오류를 조기에 발견하기 어렵다.
- Recommended fix:
  - `_uiServiceSource`를 필수 dependency로 Awake에서 한 번 검증하고 owner/field를 포함한 오류를 보고한다.
  - 자동 닫힘을 보장할 수 없는 구성에서는 클릭을 허용하지 않는 안전 정책을 적용한다.
- Impact if unfixed:
  - popup이 Merchant 부재 중에도 남고 `TryTrade`를 계속 호출할 수 있으며, 누락된 Inspector 참조가 조용히 숨겨진다.

### Low

#### L-01 — Player Gold 소유 문서가 신규 gameplay 호출자와 모순된다

- Severity: Low
- Category: Documentation drift / Cross-system ownership
- Location:
  - `PublicMD/Systems/Player_Gold.md:11,79-80,102`
  - `Assets/Scripts/Actor/MerchantTradeSite.cs:87`
- Evidence:
  - 문서는 거래·가격이 구현되지 않았고 실제 gameplay 골드 획득 경로가 없으며 유일한 호출자가 `TestGoldWindow`라고 기록한다.
  - 현재 `MerchantTradeSite`가 `GoldManager.Add`를 호출한다.
  - 문서 자체의 갱신 조건은 “새 호출자 추가” 시 갱신하도록 명시한다.
- Description:
  - Merchant 문서는 새 의존성을 설명하지만 Gold의 주 소유 문서는 이전 단계 상태에 머물러 있다.
- Recommended fix:
  - 현재 실행 흐름과 호출자 목록에 Merchant 판매를 추가하고 “상단 미구현/호출자 없음” 항목을 제거한다.
- Impact if unfixed:
  - 후속 모집·경제 작업자가 실제 Gold mutation 경로를 잘못 파악할 수 있다.

#### L-02 — 10열 mapper 변경을 “하위 호환”이라고 기록했지만 9열 row는 거부된다

- Severity: Low
- Category: Documentation accuracy / Data migration
- Location:
  - `PublicMD/Status/PROGRESS.md:61`
  - `Assets/Scripts/System/Mapper/ItemInfoCsvMapper.cs:7,30-37`
- Evidence:
  - `ColumnCount`는 10이고 `row.Length < 10`이면 parsing을 거부한다.
  - 따라서 기존 9열 row는 호환되지 않는다.
  - `<` 비교는 10열보다 많은 미래 row를 허용하는 forward tolerance일 뿐, 이전 9열 schema와의 backward compatibility가 아니다.
- Description:
  - 현재 repository의 `ItemData.csv`는 모두 10열이라 즉시 runtime 오류는 없지만 완료 기록의 호환성 설명이 반대다.
- Recommended fix:
  - 기록을 “추가 trailing column에는 관대하지만 기존 9열 row는 migration 필요”로 교정하거나, 실제로 9열을 허용하려면 기본 SellPrice 정책을 명시한다.
- Impact if unfixed:
  - 이전 형식의 item data를 재사용할 때 호환된다고 오판해 row 전체가 parsing에서 누락될 수 있다.

## Findings By File

- `MerchantCaravan.cs`
  - phase·이동·dwell 책임은 응집되어 있다.
  - H-01의 inactive bootstrap이 최초 실행을 차단한다.
- `MerchantArrivalScheduler.cs`
  - 겹치지 않는 gap timer와 always-active owner 분리는 적절하다.
  - 현재는 H-01 때문에 호출이 성공할 수 없다.
- `MerchantVisual.cs`
  - Animator 기반 click truth와 domain 비의존성은 적절하다.
  - M-04의 평가 시점 race와 M-05의 UI dependency 검증 누락이 있다.
- `MerchantTradeSite.cs`
  - Gold·warehouse·가격 transaction의 단일 owner 배치는 적절하다.
  - M-02의 overflow와 M-03의 판매 eligibility 누락을 수정해야 한다.
- `MerchantPopup.cs`
  - 가격을 계산하지 않고 intent만 전달하는 방향은 적절하다.
  - H-02 때문에 실제 UI에서 거래를 시작할 수 없다.
- `PointerClickRouter.cs`
  - press frame에만 physics query를 수행하며 concrete domain을 모른다.
  - hover router와 분리한 판단은 타당하다.
- `WarehouseInventory.cs`
  - `TryRemove`는 all-or-nothing이며 false 경로에서 상태를 변경하지 않는다.
  - `IInventory`에 추가하지 않은 선택은 현재 계약에 맞다.
- `DataManager.cs` / `IDataManager.cs`
  - item lookup facade 추가로 깨지는 다른 구현체는 없다.
  - scene의 `_itemDataContext`는 아직 미배선이다.
- `ItemInfoCsvMapper.cs`
  - 현재 10열 CSV를 정확히 parsing한다.
  - L-02의 호환성 설명만 교정이 필요하다.
- Merchant prefab/animation assets:
  - local fileID와 cross-asset GUID는 정합하다.
  - H-01의 inactive root와 M-01/M-05의 미배선 참조가 남아 있다.

## Cross-Cutting Findings

- `MerchantVisual`과 `MerchantTradeSite` 사이의 직접 참조는 없다.
- production에서 `GoldManager`를 참조하는 Merchant component는 `MerchantTradeSite` 하나뿐이다.
- `TryRemove`는 `WarehouseInventory`에만 있으며 호출자는 `MerchantTradeSite` 하나다.
- `IDataManager` 구현체는 `DataManager` 하나다.
- `PopupType.Merchant`는 기존 `None` 뒤에 append됐다. `TradeResult`는 신규 enum이다.
- `Pub.cs`의 직접 `ItemDataContext` 접근은 명시된 범위 제외 사항으로 finding 처리하지 않았다.
- `InsufficientGold` 부재, 빈 `OnLandingImpact`, SpriteRenderer 부재는 각각 현재 sell-only 범위와 문서화된 placeholder hook이므로 finding이 아니다.

## Positive Notes

- actor state, presentation, click routing, UI와 trade domain의 책임이 명확히 분리됐다.
- 거래 시 popup 표시 가격을 신뢰하지 않고 authoritative item data를 다시 조회한다.
- `WarehouseInventory.TryRemove`는 입력 실패와 재고 부족 시 원자적으로 상태를 보존한다.
- 클릭 입력은 press frame에만 `Physics2D.OverlapPoint`를 호출해 hot-path 비용이 작다.
- visit interval, dwell, flight speed는 명명된 상수와 serialized tuning으로 관리되며 `OnValidate()` 보정이 있다.
- enum은 재정렬 없이 append됐다.
- 신규 script `.meta` GUID 중복은 0건이다.
- prefab/controller/clip에서 duplicate anchor와 unresolved local/cross-asset reference는 0건이다.
- `Visual` CRC32 `3966078249`가 모든 신규 clip binding과 일치한다.
- landing event가 실제 `OnLandingImpact` method와 연결된다.
- 관련 Markdown 링크는 모두 resolve된다.
- `git diff --check HEAD^ HEAD`가 통과했고 최종 worktree는 clean이다.
- `Assembly-CSharp.csproj`에는 신규 C# 10개가 모두 포함되어 있다.

## Verification Limits

- 사용자 지시와 read-only sandbox에 따라 파일을 수정하지 않았다.
- `dotnet build`는 output artifact를 변경하므로 독립 재실행하지 않았다. `obj/Debug/Assembly-CSharp.dll`이 신규 source보다 늦고 csproj에 모든 신규 script가 포함된 것은 확인했지만, 보고된 0 warnings/0 errors 콘솔 결과 자체는 재현하지 않았다.
- Unity Editor import와 Play Mode를 실행하지 않았다.
- hand-authored prefab/controller/clip의 Unity importer 수용 여부는 정적 YAML 정합성까지만 확인했다.
- animation 전환 시점, 동일 프레임 Update race, popup interaction과 두 번째 방문은 Play Mode에서 재현하지 못했다.
- 실제 art, scene point 위치와 UI layout은 제공되지 않아 시각적 검증 대상에서 제외했다.

## Recommended Next Actions

1. H-01의 inactive bootstrap을 해결하고 최초 방문과 두 번째 방문을 검증한다.
2. H-02의 offer button binding 경로를 구현해 실제 UI에서 거래를 호출할 수 있게 한다.
3. M-02와 M-03의 transaction preflight를 재고 mutation 전에 완료한다.
4. M-04의 Animator 평가 시점 race를 제거한다.
5. M-05의 필수 UI service 검증을 추가한다.
6. M-01의 scene, layer, popup, Gold/DataManager 배선을 완료한다.
7. Unity import 후 missing script/binding/parameter 오류를 확인하고 전체 Play Mode 시나리오를 실행한다.
8. L-01과 L-02 문서 drift를 교정한다.

## Final Verdict

**Changes requested.**

책임 배치와 deliberate architecture 선택은 승인 가능하다. 그러나 비활성 prefab bootstrap과 판매 UI 호출 경로라는 두 개의 High blocker 때문에 현재 구현은 실제 click-to-trade-to-gold 기능으로 실행될 수 없다. transaction validation과 Unity wiring까지 완료한 후 재검토가 필요하다.