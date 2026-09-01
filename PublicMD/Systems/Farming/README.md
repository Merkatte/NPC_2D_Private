# Farming

농경지 runtime, crop definition, 작물 표현과 농사 action의 현재 구현을 연결하는 기능 인덱스다.

## 세부 기능 선택

| 작업 | 문서 |
|---|---|
| 농장 progress·phase·crop 선택·수확 transaction | [Runtime and Transactions](Runtime_and_Transactions.md) |
| crop 생산 규칙·catalog·검증 | [Definition and Catalog](Definition_and_Catalog.md) |
| 성장 stage sprite·cascade animation·수확 소멸 | [Crop Presentation](Crop_Presentation.md) |
| FarmingAction 실행과 NPC 비용·작업 모션 | [Farming Action](Farming_Action.md) |

## 전체 흐름

```text
crop 선택 -> FarmWorkSite runtime state
Farmer selector -> FarmingAction -> FarmWorkSite.TryInteract
  -> Growing progress 증가 / Harvesting inventory transaction
  -> StateChanged
     -> FarmCropPresenter -> CropVisualAnimator[]
```

`FarmWorkSite`가 current crop과 생산 transaction을 소유한다. `FarmProductionDefinition`은 공유 생산·표현 정의이며, `FarmCropPresenter`는 read-only 상태 이벤트를 소비할 뿐 gameplay 상태를 변경하지 않는다.

## 기능 간 불변 방향

- selector와 NPC는 crop 종류·stage sprite·yield를 저장하지 않는다.
- presentation은 inventory, progress, phase나 current definition을 변경하지 않는다.
- 최종 수확 transaction이 성공하면 runtime crop은 즉시 비워지고 visual 소멸은 비동기로 완료된다.
- crop별 공유 값은 ScriptableObject, 농장 배치별 상태는 scene의 `FarmWorkSite`가 소유한다.

## 관련 문서

- [Interaction and Destinations](../Interaction_and_Destinations.md)
- [Inventory and Items](../Inventory_and_Items.md)
- [UI](../UI.md)
- [Seed System Implementation Plan](../../Plans/Seed_System_Implementation_Plan.md)

## 문서 갱신 조건

세부 기능 추가·삭제, leaf 간 의존 방향 또는 전체 farming flow가 바뀌면 갱신한다. 세부 구현 변경은 해당 leaf만 갱신한다.
