# Farming

## 기능 목적

농경지의 현재 생산 상태, 작업 진행, 수확 결과와 생산 definition을 설명한다. NPC는 작업 action을 실행할 뿐이며 무엇이 심어져 있고 무엇을 반환하는지는 `FarmWorkSite`가 소유한다.

## 책임 경계

- `FarmWorkSite`: 배치별 phase와 progress, 생산 규칙 적용, 수확 transaction을 소유한다.
- `FarmProductionDefinition`: 최대 진행도, 작업당 변화량, 결과 item과 yield 범위를 정의한다.
- `FarmingAction`: provider에 작업 1회를 요청하고 NPC 작업 비용·표현 lifecycle을 처리한다.
- 창고는 수량 저장과 수락 여부만 담당한다. 생산 품목을 결정하지 않는다.
- Farmer selector는 농사 여부와 queue만 결정하며 농경지 내부 상태를 복제하지 않는다.

## 현재 실행 흐름

```text
FarmerActionSelector
  -> FarmWorkSite provider + InteractionRequest(Farming, strength)
  -> FarmingAction.Start(): working animation 시작
  -> provider.TryInteract(request)
     -> Growing: progress 증가, max에서 Harvesting 전환
     -> Harvesting: yield 계산 -> inventory 전체 수락 -> progress 감소
  -> NPCStat에 FarmingActionCost 적용
  -> working animation 정리 후 완료/실패
```

수확은 inventory가 전체 yield를 수락한 경우에만 progress를 감소시킨다. 부분 수락은 성공으로 취급하지 않는다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs` | 농장 progress와 결과 item·yield의 공유 생산 정의 |
| `Assets/Data/ScriptableObject/Script/FarmingActionCost.cs` | 농사 1회가 NPC 욕구에 주는 비용 |
| `Assets/Scripts/System/Action/FarmingAction.cs` | 농사 interaction 1회와 비용·작업 표현 lifecycle 실행 |
| `Assets/Scripts/System/Farming/FarmWorkPhase.cs` | Growing과 Harvesting phase 식별 |
| `Assets/Scripts/System/Farming/FarmWorkSite.cs` | 농경지 runtime progress, phase, 수확 transaction의 owner |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 성장·수확 진행도 | `FarmWorkSite.cs`, `FarmProductionDefinition.cs` |
| 농부의 작업 비용·모션 | `FarmingAction.cs`, `FarmingActionCost.cs`, [NPC Presentation](NPC_Presentation.md) |
| 수확물 입고 | `FarmWorkSite.cs`, [Inventory and Items](Inventory_and_Items.md) |
| Farmer가 농장을 선택하는 방식 | [Decision Policy](NPC_Decision_and_Actions/Decision_Policy.md), [Selector and Queue](NPC_Decision_and_Actions/Selector_and_Queue.md) |
| 농장 게이지 | `FarmWorkSite.cs`, [UI](UI.md) |
| 씨앗·작물 선택 | `FarmWorkSite.cs`, `FarmProductionDefinition.cs`, [Game Plan](../Game_Plan.md), [SPEC](../SPEC.md) |

## 불변 규칙

- phase와 progress는 scene의 `FarmWorkSite`가 소유하고 NPC나 ScriptableObject에 저장하지 않는다.
- 수확 결과가 외부 inventory에 반영된 뒤에만 내부 progress를 감소시킨다.
- production gameplay 난수는 주입된 `IRandomSource` 구현을 사용한다.
- 농장 전용 provider interface를 만들지 않고 공통 interaction protocol을 사용한다.

## Unity 배선과 검증 도구

- `FarmWorkSite`: production definition, `SeededRandomSource`, `IInventory`를 구현한 output source가 필요하다.
- `Assets/Scenes/FarmerTest.unity`: 현재 농장·창고·UI 검증 scene이다.
- `Assets/TestOnly/TestFarmProductionWindow.cs`: production definition 교체, 강제 작업, phase/progress, 창고 수량을 확인한다.
- `Assets/Prefab/UI/FarmGauge.prefab`: UI 소유 에셋이며 이 문서에서는 소비 관계만 가진다.

## 알려진 제약과 TBD

- 현재 어떤 씨앗을 심을지 선택하는 사용자 기능은 없다.
- 현재 `FarmWorkSite`는 하나의 `FarmProductionDefinition` 참조만 사용한다.
- 씨앗 선택 규칙과 전환 시점은 기획 확정 전까지 TBD이며 이 문서에서 임의로 정하지 않는다.

## 관련 문서

- [Interaction and Destinations](Interaction_and_Destinations.md)
- [Inventory and Items](Inventory_and_Items.md)
- [UI](UI.md)
- [판단과 Action 인덱스](NPC_Decision_and_Actions/README.md)

## 문서 갱신 조건

농장 phase, 생산 definition, 작업 transaction, 결과 입고 또는 씨앗 선택 책임이 바뀌면 갱신한다.
