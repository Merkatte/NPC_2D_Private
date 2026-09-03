# Farming Runtime and Transactions

## 기능 목적

농경지 배치별 current crop, phase, progress, 분산 작업 위치와 수확물 인도 transaction을 소유한다.

## 책임 경계

- `FarmWorkSite`는 crop 선택, Growing/Harvesting 전환, pending yield와 수확물 인도를 소유한다.
- `FarmWorkSite`는 Farming과 Harvest를 phase로 게이트된 두 capability로 노출한다.
- `FarmWorkSite`는 작업 영역을 격자로 나누고 Farming/Harvest batch마다 한 작업 위치를 제공한다.
- `FarmWorkPhase`는 Growing과 Harvesting 상태를 식별한다.
- runtime 상태가 성공적으로 바뀐 뒤 `StateChanged`를 발행하지만 구체 presentation을 참조하지 않는다.

## 현재 실행 흐름

```text
TrySelectCrop(valid definition)
  -> current definition + Growing + progress 0
  -> StateChanged

CanInteract(Farming)  -> crop 있음 + Growing phase
CanInteract(Harvest)  -> crop 있음 + Harvesting phase

TryInteract(Farming, strength)
  -> progress 증가, max에서 Harvesting으로 전환
  -> StateChanged

TryInteract(Harvest, strength, cargo)
  -> pending yield가 없으면 1회만 roll
  -> cargo.TryAdd(outputItemId, pending yield)
  -> 수락 0: 아무 상태도 바꾸지 않고 false
  -> 부분 수락: 수락량만 pending에서 차감하고 progress 유지, true
  -> 전량 수락: progress 감소, 0이면 phase Growing + current definition 제거, true
  -> 성공한 상태 변경만 StateChanged

TryGetActionPosition(Farming | Harvest, registered farm destination)
  -> 4 x 2 cell 순서를 shuffle
  -> 한 cycle에서 중복 없이 셀 선택
  -> 셀 내부 jitter를 더해 world position 반환
  -> 배선·영역 오류 시 한 번 경고하고 registered destination 반환
```

수확물은 `FarmWorkSite`가 창고에 직접 넣지 않고 `InteractionRequest.Cargo`(요청자가 들고 온 `ICarriedInventory`)로 인도한다. 창고까지의 운반과 입고는 [Inventory and Items](../Inventory_and_Items.md)가 소유한다. cargo가 없는 Harvest 요청은 한 번만 오류 로그를 남기고 거부한다.

pending yield는 한 번 굴린 뒤 전량이 cargo로 넘어갈 때까지 유지되며 재굴림하지 않는다. 부분 수락은 실제 작업 성과이므로 성공으로 보고하되 progress는 소모하지 않는다. 최종 수확 뒤 visual 완료를 기다리지 않고 농장은 선택 대기 상태가 된다.

Farming과 Harvest가 phase로 배타적이므로 selector는 `FarmWorkPhase`를 직접 읽지 않고 평소의 `CanInteract` 조회만으로 "지금 수확 가능한 밭인지"를 판단한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Farming/FarmWorkSite.cs` | 농장 runtime crop·progress·phase, Farming/Harvest capability, 분산 작업 위치, 수확물 인도와 상태 알림 |
| `Assets/Scripts/System/Farming/FarmWorkPhase.cs` | Growing과 Harvesting phase 식별 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| progress·phase·crop 종료 | `FarmWorkSite.cs`, [Definition and Catalog](Definition_and_Catalog.md) |
| 수확물 인도·pending yield | `FarmWorkSite.cs`, [Inventory and Items](../Inventory_and_Items.md) |
| Farming/Harvest 게이트 | `FarmWorkSite.cs`, [Farming Action](Farming_Action.md), [Selector and Queue](../NPC_Decision_and_Actions/Selector_and_Queue.md) |
| 상태 이벤트·visual 동기화 | `FarmWorkSite.cs`, [Crop Presentation](Crop_Presentation.md) |
| 농장 게이지 | `FarmWorkSite.cs`, [UI](../UI.md) |
| 작업 위치 분산 | `FarmWorkSite.cs`, [Interaction and Destinations](../Interaction_and_Destinations.md), [Selector and Queue](../NPC_Decision_and_Actions/Selector_and_Queue.md) |

## 불변 규칙

- phase, progress, current crop은 scene의 `FarmWorkSite`만 저장한다.
- `TrySelectCrop`은 모든 입력 검증 뒤 성공 시에만 상태를 함께 변경한다.
- current crop이 없거나 invalid하면 Farming과 Harvest interaction을 모두 정상적으로 거부한다.
- 외부 cargo가 전량을 수락하기 전에는 progress를 소모하지 않는다.
- 거부된 yield는 재롤하지 않으며 production 난수는 주입된 `IRandomSource`만 사용한다.
- 농장은 수확물을 창고나 특정 소비자에게 직접 넣지 않고 요청에 실려 온 cargo로만 인도한다.
- 위치는 batch를 구성할 때 한 번만 선택하며 같은 batch의 Move와 작업 action이 공유한다.
- 기본 4 x 2 작업 셀은 한 순환 안에서 중복 없이 배정되지만 위치 예약·반납은 하지 않는다. 8명을 초과하거나 이전 batch가 끝나기 전에 순환하면 작업 위치가 겹칠 수 있다.
- 작업 위치용 난수는 yield 난수와 분리하며 utility 후보 평가 중에는 소비하지 않는다.
- presentation subscriber 부재는 gameplay 동작에 영향을 주지 않는다.

## Unity 배선과 검증 도구

- `FarmWorkSite`에는 yield용 `SeededRandomSource`, 작업 영역 `BoxCollider2D`와 위치용 `SeededRandomSource`를 연결한다. 수확물 목적지 inventory는 더 이상 농장에 배선하지 않는다(`_outputInventorySource` 제거).
- 위치 dependency가 누락되거나 영역이 유효하지 않아도 생산 provider 초기화는 실패시키지 않고 기존 농장 중심을 사용한다.
- `FarmerTest`의 `TestFarmProductionWindow`는 FarmWorkSite·Warehouse·CropCatalog·ItemDataContext가 배선되어 있으며, 자체 probe cargo로 Harvest를, `WarehouseDepositPoint`로 Deposit을 각각 실행한다.
- Seed Phase 1 Carrot/Potato 선택·진행·입고 Play Mode 시나리오는 2026-09-01 사용자 확인으로 통과했다. Harvest/cargo 경로의 Play Mode 검증은 2026-09-04 기준 `NOT VERIFIED`다.

## 알려진 제약과 TBD

- `WarehouseInventory`는 용량 제한이 없어 창고 측 입고 거부 경로를 Play Mode에서 재현할 수 없다. 부분 수락은 봇짐이 가득 찬 경우로만 관측된다.
- 실제 seed item 소비는 후속 slice로 보류되어 있다.
- 서로 다른 작물이 한 봇짐에 섞이는 상황은 현재 게임 구조상 도달할 수 없다고 판단해 item type 불일치는 단순 거부로 처리한다.

## 관련 문서

- [Farming index](README.md)
- [Definition and Catalog](Definition_and_Catalog.md)
- [Crop Presentation](Crop_Presentation.md)
- [Inventory and Items](../Inventory_and_Items.md)

## 문서 갱신 조건

crop 선택, phase/progress, capability 게이트, 수확물 인도, 최종 cycle 종료와 `StateChanged` 의미가 바뀌면 갱신한다.
