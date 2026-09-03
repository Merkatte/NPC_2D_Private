# Farming Runtime and Transactions

## 기능 목적

농경지 배치별 current crop, phase, progress, 분산 작업 위치와 수확 입고 transaction을 소유한다.

## 책임 경계

- `FarmWorkSite`는 crop 선택, Growing/Harvesting 전환, pending yield와 inventory 입고를 소유한다.
- `FarmWorkSite`는 작업 영역을 격자로 나누고 Farming batch마다 한 작업 위치를 제공한다.
- `FarmWorkPhase`는 Growing과 Harvesting 상태를 식별한다.
- runtime 상태가 성공적으로 바뀐 뒤 `StateChanged`를 발행하지만 구체 presentation을 참조하지 않는다.

## 현재 실행 흐름

```text
TrySelectCrop(valid definition)
  -> current definition + Growing + progress 0
  -> StateChanged

TryInteract(Farming, strength)
  -> Growing: progress 증가, max에서 Harvesting
  -> Harvesting: yield 확정 -> inventory 수락 -> progress 감소
  -> 최종 progress 0: current definition 즉시 제거
  -> 성공한 상태 변경만 StateChanged

TryGetActionPosition(Farming, registered farm destination)
  -> 4 x 2 cell 순서를 shuffle
  -> 한 cycle에서 중복 없이 셀 선택
  -> 셀 내부 jitter를 더해 world position 반환
  -> 배선·영역 오류 시 한 번 경고하고 registered destination 반환
```

부분 수락된 yield는 수락량만 pending 값에서 차감하며, 실패한 입고는 progress를 감소시키지 않는다. 최종 수확 뒤 visual 완료를 기다리지 않고 농장은 선택 대기 상태가 된다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Farming/FarmWorkSite.cs` | 농장 runtime crop·progress·phase, 분산 작업 위치, 수확 transaction과 상태 알림 |
| `Assets/Scripts/System/Farming/FarmWorkPhase.cs` | Growing과 Harvesting phase 식별 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| progress·phase·crop 종료 | `FarmWorkSite.cs`, [Definition and Catalog](Definition_and_Catalog.md) |
| 수확물 입고 | `FarmWorkSite.cs`, [Inventory and Items](../Inventory_and_Items.md) |
| 상태 이벤트·visual 동기화 | `FarmWorkSite.cs`, [Crop Presentation](Crop_Presentation.md) |
| 농장 게이지 | `FarmWorkSite.cs`, [UI](../UI.md) |
| 작업 위치 분산 | `FarmWorkSite.cs`, [Interaction and Destinations](../Interaction_and_Destinations.md), [Selector and Queue](../NPC_Decision_and_Actions/Selector_and_Queue.md) |

## 불변 규칙

- phase, progress, current crop은 scene의 `FarmWorkSite`만 저장한다.
- `TrySelectCrop`은 모든 입력 검증 뒤 성공 시에만 상태를 함께 변경한다.
- current crop이 없거나 invalid하면 Farming interaction을 정상적으로 거부한다.
- 외부 inventory 반영 전에는 progress를 소모하지 않는다.
- 거부된 yield는 재롤하지 않으며 production 난수는 주입된 `IRandomSource`만 사용한다.
- 위치는 Farming batch를 구성할 때 한 번만 선택하며 같은 batch의 Move와 Farming action이 공유한다.
- 기본 4 x 2 작업 셀은 한 순환 안에서 중복 없이 배정되지만 위치 예약·반납은 하지 않는다. 8명을 초과하거나 이전 batch가 끝나기 전에 순환하면 작업 위치가 겹칠 수 있다.
- 작업 위치용 난수는 yield 난수와 분리하며 utility 후보 평가 중에는 소비하지 않는다.
- presentation subscriber 부재는 gameplay 동작에 영향을 주지 않는다.

## Unity 배선과 검증 도구

- `FarmWorkSite`에는 yield용 `SeededRandomSource`, `IInventory` 구현 source, 작업 영역 `BoxCollider2D`와 위치용 `SeededRandomSource`를 연결한다.
- 위치 dependency가 누락되거나 영역이 유효하지 않아도 생산 provider 초기화는 실패시키지 않고 기존 농장 중심을 사용한다.
- `FarmerTest`의 `TestFarmProductionWindow`는 FarmWorkSite·Warehouse·CropCatalog·ItemDataContext가 배선되어 있다.
- Seed Phase 1 Carrot/Potato 선택·진행·입고 Play Mode 시나리오는 2026-09-01 사용자 확인으로 통과했다.

## 알려진 제약과 TBD

- `WarehouseInventory`는 용량 제한이 없어 입고 거부·부분 수락 경로를 Play Mode에서 재현할 수 없다.
- 실제 seed item 소비는 후속 slice로 보류되어 있다.

## 관련 문서

- [Farming index](README.md)
- [Definition and Catalog](Definition_and_Catalog.md)
- [Crop Presentation](Crop_Presentation.md)

## 문서 갱신 조건

crop 선택, phase/progress, 수확 transaction, 최종 cycle 종료와 `StateChanged` 의미가 바뀌면 갱신한다.
