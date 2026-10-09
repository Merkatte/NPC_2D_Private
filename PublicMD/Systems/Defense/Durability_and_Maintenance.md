# Defense Durability and Maintenance

## 목적과 책임

성벽과 완공 시설의 HP·손상 수명, 건축가 유지보수 transaction을 소유한다. 피해/복구 판정은 health owner, 비용/진행/예약은 maintenance provider, 업무 선택은 Builder selector, 이동·작업 실행은 action으로 분리한다.

## 현재 흐름

벽 HP0은 해당 NavigationObstacle2D를 열고 외형을 교체한다. 벽을 철거하거나 다른 위치에 짓지 않는다. 재건 완료는 원래 위치의 HP를 복구하며 통로 안에 actor가 있으면 개방을 유지하고 비워진 뒤 닫는다. 지역 점유 판정은 disabled collider의 local geometry를 사용한다.

시설 HP0은 먼저 target을 끄고 Plot의 기존 공사/업그레이드 예약을 환불 없이 취소한다. CompletedBuildingFacility의 등록을 해제하고 scene-native DestinationDB 등록도 제거한다. 명시적으로 연결한 provider/House/warehouse 등의 기능, collider와 원본 sprite를 끄고 잔해를 켠다. House/warehouse의 기존 해제 API가 입주·용량을 처리한다. 잔해 제거는 즉시 cleared 상태와 예약 종료를 확정한 뒤 원래 Plot을 Empty로 바꾸고 시설 객체를 파괴한다. Empty 이후에는 기존 유료 신축 흐름을 사용한다. 시청 파괴는 별도 잔해 업무를 만들지 않고 Session.EndGame을 호출한다.

건축가 우선순위는 도주 → 기존 태업/긴급 생활 → 공격받는 벽 수리 → 벽 재건 → 나머지 수리 → 잔해 제거 → 기존 공사다. 같은 rank는 발생 Order를 유지한다. site별 한 예약이며 중단은 lease만 반납하고 진행/최초 지불 상태를 보존한다. repair 완료/재파괴처럼 작업 종류가 바뀌면 새 작업으로 전환한다. 비용은 작업 첫 성공 tick에서 한 번 지불한다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Actor/DefenseWallSegment.cs` | 성벽 HP·sprite·타격 시각·통로·점유 중 폐쇄 지연 |
| `Assets/Scripts/Actor/DefenseBuildingDurability.cs` | 시설 HP·기능 해제·잔해·시청 종료 연결 |
| `Assets/Scripts/Actor/DefenseMaintenanceSite.cs` | 유지보수 예약 검증·지불·진행·완료 transaction |
| `Assets/Scripts/Manager/DefenseMaintenanceRegistry.cs` | 활성 site와 rank/발생 순서 조회 |
| `Assets/Scripts/System/Actor/MaintenanceLease.cs` | site 한 개 작업 예약과 완료/반납 수명 |
| `Assets/Scripts/System/Action/MaintenanceAction.cs` | 작업점 이동·기존 건축 욕구 비용·망치·시간 작업 요청 |
| `Assets/Scripts/Enum/MaintenanceKind.cs` | None/Repair/RebuildWall/ClearRubble 값 |
| `Assets/Data/ScriptableObject/Script/DefenseDurabilitySettings.cs` | 벽/시설/시청 HP, 수리 속도·재건/정리 시간·선택적 비용 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 확인 |
|---|---|
| 성벽 피해·재건·통로 | WallSegment, MaintenanceSite, NavigationObstacle2D, Battlefield |
| 건물 파괴·잔해 | BuildingDurability, BuildingPlot, CompletedBuildingFacility, 각 시설 owner |
| 유지보수 선택·중단 | Registry, MaintenanceLease, MaintenanceAction, BuilderActionSelector |
| 비용·시간 | DurabilitySettings, MaintenanceSite, ResourceManager |
| 동적 신축 배선 | BuildingFactory, Defense 시설 prefab, Defense BuildingDataContext |

## 배선과 불변 규칙

벽은 north attack, south work/ground/entry, top anchor와 collider geometry를 명시 연결한다. 자동 친화 통로의 장애물은 Enemy만 차단한다. site가 먼저 Enable되어도 health owner 초기화 뒤 RefreshTask가 현재 HP로 작업을 다시 확정한다.

Defense BuildingFactory는 비활성 시설에 Battlefield/registry/resources/settings/session/plot/destination 참조를 주입한 뒤 활성화한다. 기존 scene-native 시설은 `_initialFacility`가 있는 Plot wrapper로 시작한다. `_functionalBehaviours`에는 해제할 시설 기능만 넣고 durability/maintenance/target/장애물 owner를 넣지 않는다. GuardPost의 기존 health view는 DefenseDurability로 위임한다. shared warehouse 저장량은 capacity 감소로 버리지 않는다.

HP·progress는 scene instance에 있고 SO를 변경하지 않는다. Time.timeScale=0에서는 수리·피해·재건·잔해 transaction을 실행하지 않는다. 진행 중 주택 업그레이드는 타겟이며 파괴 후 이전 집/공사 효과를 남기지 않는다. 신규 미완공 부지·상인은 target 등록에서 제외한다.

## 검증·제약·관련 문서

컴파일·기존 정적 검사·serialized 참조 확인과 사람 플레이 확인을 구분한다. 자동 Play/화면 확인은 제외한다. 복구/철거 추가 규칙은 [승인 계획](../../Archive/Plans/Defense_Implementation_Plan.md)을 따른다. [Construction](../Construction/README.md), [Builder](../Builder.md), [Navigation](../Navigation.md), [Housing](../Housing/README.md), [공유 자원](../Inventory_and_Items/Shared_Resources.md)과 책임 경계를 함께 확인한다. HP/작업 API, priority, 기능 해제 목록, 배선이 바뀌면 갱신한다.
