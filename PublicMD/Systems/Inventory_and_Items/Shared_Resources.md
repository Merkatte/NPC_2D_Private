# Shared Resources

## 책임과 흐름

ResourceManager는 scene 공개 API와 알림을 제공한다. plain ResourceInventory가 실제 수량, 적용된 총 용량과 사용량을 소유한다. 가변 inventory를 외부에 노출하지 않는다. 구 GoldManager/WarehouseInventory와 해당 직렬화 참조는 이관 후 제거했다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Scripts/Manager/ResourceManager.cs | 정의 검증·공개 API·용량 제공자 등록·알림 |
| Assets/Scripts/System/Inventory/ResourceInventory.cs | 수량·용량 보관과 전량 검증 후 변경 |
| Assets/Scripts/Interface/IInventory.cs | 수량 조회와 부분 수락 TryAdd 계약 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 확인 |
|---|---|
| 수량·overflow·용량 | ResourceInventory, ResourceManager, ResourceInventoryTests |
| 알림·재진입 | ResourceManager, 해당 provider의 DeferNotifications 범위 |
| 창고 등록·상실 | [Cargo and Deposit](Cargo_and_Deposit.md), CompletedBuildingFacility |
| 시작 재고 | TestOnly/BuildingTestBootstrap, 해당 scene |

## API와 불변 규칙

- GetQuantity는 조회, TryDeposit은 남은 용량까지 부분 입고한다.
- TrySpend는 전량 지출, TryRefund는 용량을 넘어도 정확한 전량 환급, TryExchange는 차감·지급의 원자적 교환이다. 실패하면 수량은 바뀌지 않는다.
- 비양수 항목·알 수 없는 ID·정수 overflow를 거부한다. 포화로 자원을 잃지 않는다. 빈 지출/환급은 성공한 무변경이다.
- 기본 용량은 0이며 시설별 제공량을 합산한다. 일시 disable은 유지, 실제 제거는 해제한다. 초과 재고는 보존하고 일반 입고를 막는다.
- 골드는 용량을 쓰지 않는 동일 자원이며 별도 잔액 저장소를 두지 않는다.
- ResourcesChanged는 실제 변경 후 payload 없이 발행한다. 동기 DeferNotifications 안에서 도메인 상태까지 확정한 뒤 알린다. coroutine/yield를 넘어 scope를 유지하지 않는다.
- 재진입 변경은 현재 알림 후 발행한다. 구독자 예외로 성공한 거래를 실패로 되돌리지 않는다.

## 배선·검증·제약

scene의 공유 ResourceManager 하나에 ItemDataContext를 연결한다. 시작 자원은 TestOnly bootstrap이 API로 지급한다. 판매는 원자적 교환, 심기·모집·입고·건설은 도메인 상태와 알림을 함께 확정한다.

원자성·부분 입고·초과 환급·제공자 등록/해제·정수 경계·알림 재진입을 비-Play 검사한다. 실행 결과는 PROGRESS를 따른다. persistence·가중치·슬롯별 stack은 범위 밖이다. API나 ownership 변경 시 이 leaf와 ARCHITECTURE를 갱신한다.
