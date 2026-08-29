# Farming

## 기능 목적

농경지의 현재 생산 상태, 작업 진행, 수확 결과와 crop definition·catalog를 설명한다. NPC는 작업 action을 실행할 뿐이며 무엇이 심어져 있고 무엇을 반환하는지는 `FarmWorkSite`가 소유한다.

## 책임 경계

- `FarmWorkSite`: 배치별 현재 crop, phase와 progress, 생산 규칙 적용, 씨앗 선택 transaction, 수확 transaction을 소유한다.
- `FarmProductionDefinition`: crop ID·표시 이름, 최대 진행도, 작업당 변화량, 결과 item과 yield 범위를 정의한다.
- `CropCatalog`: 선택 가능한 `FarmProductionDefinition` 목록과 구조적 유효성·중복 crop ID·output item cross-reference 검증(`TryValidate`)을 제공한다. 어떤 crop이 심어져 있는지는 소유하지 않는다.
- `FarmingAction`: provider에 작업 1회를 요청하고 NPC 작업 비용·표현 lifecycle을 처리한다.
- 창고는 수량 저장과 수락 여부만 담당한다. 생산 품목을 결정하지 않는다.
- Farmer selector는 농사 여부와 queue만 결정하며 농경지 내부 상태를 복제하지 않는다.

## 현재 실행 흐름

```text
FarmWorkSite.TrySelectCrop(definition)   // 빈 농경지에서만 성공, 성공 시에만 current crop/phase/progress 갱신
  -> CanInteractCore: current crop이 없거나 invalid하면 Farming 자체가 불가

FarmerActionSelector
  -> DestinationDB.TryGetInteractionProvider가 CanInteract를 재확인 (crop 없으면 provider 조회 실패)
  -> 실패 시 one-shot 로그 후 Idle로 fallback (selector 쪽 기존 경로, 변경 없음)
  -> FarmWorkSite provider + InteractionRequest(Farming, strength)
  -> FarmingAction.Start(): working animation 시작
  -> provider.TryInteract(request)
     -> Growing: progress 증가, max에서 Harvesting 전환
     -> Harvesting: yield 계산(최초 1회, 거부되면 재사용) -> inventory 전체 수락 -> progress 감소
  -> NPCStat에 FarmingActionCost 적용
  -> working animation 정리 후 완료/실패
```

수확은 inventory가 전체 yield를 수락한 경우에만 progress를 감소시킨다. 부분 수락은 성공으로 취급하지 않는다. 입고가 거부된 yield 값은 버리지 않고 다음 시도에서 재사용하여 결정적 난수 stream이 실패한 시도로 어긋나지 않게 한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs` | crop ID·표시 이름과 progress·결과 item·yield의 공유 생산 정의 |
| `Assets/Data/ScriptableObject/Script/CropCatalog.cs` | 선택 가능한 crop definition 목록과 구조·중복 ID·output item cross-reference 검증 |
| `Assets/Data/ScriptableObject/Script/FarmingActionCost.cs` | 농사 1회가 NPC 욕구에 주는 비용 |
| `Assets/Scripts/System/Action/FarmingAction.cs` | 농사 interaction 1회와 비용·작업 표현 lifecycle 실행 |
| `Assets/Scripts/System/Farming/FarmWorkPhase.cs` | Growing과 Harvesting phase 식별 |
| `Assets/Scripts/System/Farming/FarmWorkSite.cs` | 농경지 runtime crop 선택, progress, phase, 수확 transaction의 owner |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 성장·수확 진행도 | `FarmWorkSite.cs`, `FarmProductionDefinition.cs` |
| 농부의 작업 비용·모션 | `FarmingAction.cs`, `FarmingActionCost.cs`, [NPC Presentation](NPC_Presentation.md) |
| 수확물 입고 | `FarmWorkSite.cs`, [Inventory and Items](Inventory_and_Items.md) |
| Farmer가 농장을 선택하는 방식 | [Decision Policy](NPC_Decision_and_Actions/Decision_Policy.md), [Selector and Queue](NPC_Decision_and_Actions/Selector_and_Queue.md) |
| 농장 게이지 | `FarmWorkSite.cs`, [UI](UI.md) |
| 씨앗·작물 선택, 새 crop 추가 | `FarmWorkSite.cs`, `FarmProductionDefinition.cs`, `CropCatalog.cs`, [Inventory and Items](Inventory_and_Items.md), [Game Plan](../Game_Plan.md), [SPEC](../SPEC.md) |

## 불변 규칙

- phase, progress, current crop은 scene의 `FarmWorkSite`가 소유하고 NPC나 ScriptableObject에 저장하지 않는다.
- 씨앗 선택은 빈 농경지(current crop 없음, 또는 Growing phase에서 progress가 0)에서만 성공한다. 성장·수확 중 선택은 상태를 바꾸지 않고 실패 사유를 반환한다.
- 씨앗 선택은 첫 구현에서 실제 seed item을 소비하지 않는다. 생산 설정만 바뀐다.
- `TrySelectCrop`은 입력을 모두 검증한 뒤에만 current crop과 phase/progress를 함께 mutate한다.
- 수확 결과가 외부 inventory에 반영된 뒤에만 내부 progress를 감소시킨다.
- 거부된 harvest yield는 재롤하지 않고 다음 시도에서 재사용한다. 부분 수락된 경우에도 수락된 만큼만 pending yield에서 차감해 재시도가 이미 입고된 수량을 다시 요청하지 않는다.
- `CropCatalog.TryValidate(itemDataContext, out reason)`는 구조적 유효성과 output item cross-reference를 함께 검증한다. 이 검증은 catalog를 소비하는 초기화 경계(`TestFarmProductionWindow`)에서 실행하며, `FarmWorkSite.TrySelectCrop`은 catalog를 거치지 않으므로 이 cross-check를 직접 수행하지 않는다.
- current crop이 없거나 invalid하면 `CanInteract`가 false를 반환해 Farming 자체를 막는다. 이는 정상 상태이므로 로그하지 않는다.
- `FarmWorkSite`의 고정 scene dependency(`SeededRandomSource`, `IInventory` output source) 부재만 초기화 실패로 취급한다. crop 부재는 초기화 실패가 아니다.
- production gameplay 난수는 주입된 `IRandomSource` 구현을 사용한다.
- 농장 전용 provider interface를 만들지 않고 공통 interaction protocol을 사용한다.

## Unity 배선과 검증 도구

- `FarmWorkSite`: `SeededRandomSource`, `IInventory`를 구현한 output source가 필요하다. `_startingDefinition`은 선택 사항이며 비어 있으면 빈 농장으로 시작한다.
- `Assets/Scenes/FarmerTest.unity`: 현재 농장·창고·UI 검증 scene이다.
- `Assets/TestOnly/TestFarmProductionWindow.cs`: `CropCatalog`의 crop 선택, 강제 작업, phase/progress, crop별 창고 수량을 확인한다. window 생성 시 `CropCatalog.TryValidate`를 1회 호출해 결과를 캐시하며, invalid catalog면 선택 버튼 대신 실패 사유를 표시한다. 이 window를 실제로 사용하려면 scene에 GameObject로 배치하고 `_farmWorkSite`/`_warehouse`/`_cropCatalog`/`_itemDataContext`를 배선해야 한다(현재 미배선, TBD 참고).
- `Assets/Prefab/UI/FarmGauge.prefab`: UI 소유 에셋이며 이 문서에서는 소비 관계만 가진다.

## 알려진 제약과 TBD

- 플레이어가 농경지에서 직접 씨앗을 선택하는 UI(popup)는 아직 없다. 선택 경로는 현재 `TestFarmProductionWindow`뿐이며, 이 window는 scene에 아직 배치되지 않았다.
- 작물 성장 단계 sprite·animation 표현은 아직 없다. current crop과 progress는 수치로만 존재한다.
- `WarehouseInventory`는 용량 제한이 없어 입고 거부(부분 수락) 경로를 Play Mode에서 재현할 수 없다.
- crop별 결과물 표현과 선택 UI는 [Seed System Implementation Plan](../Plans/Seed_System_Implementation_Plan.md)의 Phase 2·3으로 계획되어 있다. Phase 2가 들어오면 이 영역의 독립 세부 기능이 4개(runtime/transaction, definition/catalog, crop presentation, 농사 action 실행)가 되므로 그 시점에 `Systems/Farming/` 폴더 인덱스로 분할한다.

## 관련 문서

- [Interaction and Destinations](Interaction_and_Destinations.md)
- [Inventory and Items](Inventory_and_Items.md)
- [UI](UI.md)
- [판단과 Action 인덱스](NPC_Decision_and_Actions/README.md)
- [Seed System Implementation Plan](../Plans/Seed_System_Implementation_Plan.md)

## 문서 갱신 조건

농장 phase, crop 선택·definition·catalog, 작업 transaction, 결과 입고 책임이 바뀌면 갱신한다. Farming의 독립 세부 기능이 4개 이상이 되면(Phase 2 crop presentation 등) 이 문서를 `Systems/Farming/README.md` + leaf 문서로 분할한다.
