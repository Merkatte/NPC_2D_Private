# Cargo and Deposit

## 책임과 흐름

`FarmWorkSite → InteractionRequest.Cargo → WorkerInventory → WarehouseDepositPoint → ResourceManager`가 운반 흐름이다. 생산자는 수락량만 소모하고 입고 거부 잔량은 NPC에게 남는다. 창고별 재고 사본을 두지 않는다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Scripts/System/Inventory/WorkerInventory.cs | NPC별 단일 품목 cargo와 표시 콜백 |
| Assets/Scripts/Interface/ICarriedInventory.cs | cargo 조회·부분 수락·목적지 이관 |
| Assets/Scripts/Actor/WarehouseDepositPoint.cs | 입고 transaction·용량 제공·창고 클릭 source |
| Assets/Scripts/System/Action/DepositAction.cs | 도착 후 입고 interaction |

## 최소 확인 범위와 계약

수락·이관 변경은 위 inventory/interface, 입고·용량은 WarehouseDepositPoint/ResourceManager/CompletedBuildingFacility, queue는 FarmerActionSelector/DepositAction을 읽는다. 운반 표시는 [NPC Presentation](../NPC_Presentation.md)이 소유한다.

WorkerInventory는 NPCComponent 소유 plain 객체이며 한 번에 한 품목만 담는다. NPCComponent만 pool reset에서 Clear한다. provider에게 cargo 초기화 권한은 없다.

WarehouseDepositPoint._inventorySource는 공유 ResourceManager다. CSV definition의 제공 용량을 Configure로 전달하고 창고 identity로 등록한다. prefab에 CSV 용량을 복제하지 않는다. 일시 disable은 유지하고 OnDestroy/명시적 제거에서 해제한다.

DeferNotifications 안에서 TryTransferAllTo하므로 cargo 감소까지 확정된 상태가 구독자에게 보인다. DepositAction은 도구 모션 없이 입고한다. 빈 cargo는 정상 완료, 실패는 재판단이다. 모든 창고가 가득 차면 Farmer는 cargo를 보존하고 Idle 후 재판단한다.

## 배선·검증·제약

provider와 destination을 같은 시설 identity로 연결한다. 공유재고/cargo 총량 보존·부분 수락·참조를 검사한다. 다품목 운반·persistence는 범위 밖이며 despawn 시 cargo 보존은 TBD다. 운반·입고·등록 계약 변경 시 갱신한다.

## 불만도·태업 연결

태업이면 Farmer selector가 Deposit/창고 업무 Move를 새로 만들지 않는다. 실행 중인 context는 RequiresWorkAvailability=true로 시작/Tick 효과 이전에 재판단한다. cargo는 기존 소유자에게 남고 회복 뒤 다음 선택에서 운반/입고한다. provider transaction에는 태업 정책을 추가하지 않는다.
