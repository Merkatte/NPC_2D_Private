# Construction Facilities

## 책임과 흐름

BuildingFactory는 선택된 정의와 고정 부지의 배치 정보를 받아 시설을 생성·주입·초기화·등록한다. BuildingPlot은 성공한 참조를 보관하고 완료 상태를 확정한다. 정확한 적용/검증 상태는 PROGRESS를 따른다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Scripts/Manager/BuildingFactory.cs | 씬 의존성 조립·시설별 등록과 생성 실패 정리 |
| Assets/Scripts/Actor/CompletedBuildingFacility.cs | prefab의 명시적 entrance/provider/시설 컴포넌트 연결과 수명 |

주택 생성은 Factory의 선택적 HousingDataContext/HousingManager가 연결된 씬에서만 가능하다. CompletedBuildingFacility의 House/visual/source 참조를 검증하고 비활성 상태에서 House.TryConfigure로 주입한 뒤 입구를 정렬하고 활성화한다. House의 등록·해제는 자체 lifecycle이 소유하며 공통 DestinationDB에는 등록하지 않는다. 다른 시설 경로에는 주거 서비스 연결이 필수가 아니다.

주택 업그레이드는 새 prefab을 생성하지 않는다. Factory가 다음 단계와 sprite를 검증하고 기존 facility가 sprite를 준비한 다음 House.TryApplyTier를 호출한다. false이면 sprite를 복원하며 House domain은 단계/입주를 변경하지 않는다. 성공 변경 이벤트는 새 단계와 sprite를 함께 관찰한다. 공사 비용/예약/진행은 Plot이 소유한다.

## 선택적 Defense 주입

BuildingFactory의 DefenseBattlefield/DefenseMaintenanceRegistry/DefenseDurabilitySettings/DefenseGameSession 참조가 연결되면 생성한 variant의 DefenseBuildingDurability에 resources/plot/facility/DestinationDB까지 주입한다. 기존 비활성 생성→시설 설정→입구 정렬→활성화 순서 안에서 처리한다. Defense 서비스가 없으면 기존 prefab과 경로를 유지한다. 파괴는 기존 CompletedBuildingFacility.RemoveRegistrations와 시설별 disable 수명을 사용한다. 피해/잔해 transaction은 [Defense durability](../Defense/Durability_and_Maintenance.md)가 소유한다.

## 최소 확인 범위와 불변 규칙

- 생성/수명 변경은 두 파일과 해당 시설의 주 소유 문서, DestinationDB/InteractableManager, ResourceManager를 확인한다.
- 주입 전 Awake/조회가 초기화 실패를 고정하지 않도록 비활성 생성과 명시적 configure 순서를 유지한다.
- prefab 루트에 provider가 있다고 추측하지 않는다. Pub는 EntrancePoint 자식, Inn은 기존 위치 기반 Sleep이다.
- prefab 입구 anchor를 Plot의 고정 입구에 맞춘다. Farm 작업/Guard 순찰은 고정 외곽 영역, Farm 작물 visual은 내부다.
- 새 Farm은 빈 밭이며 위치/수확 난수는 분리한다. gameplay 난수에 UnityEngine.Random을 사용하지 않는다.
- 성공 전 이용 대상으로 공개하지 않는다. 실패 시 그 시도의 객체/등록만 정리하고 다른 시설이나 공사 진행도/지불 상태를 변경하지 않는다.

## 배선과 검사

기존 Restaurant/Inn/Warehouse/Soil 외형과 신규 GuardPost prefab을 연결한다. BuildingConstructionSetup은 신규·기존 GuardPost prefab 모두에 전용 GuardPostPositionRandom 자식의 SeededRandomSource(seed 1977)를 생성·재사용하고 GuardPost._positionRandomSource에 연결한다. _patrolArea는 완공 시 Factory가 부지 영역을 주입한다. scene 참조는 Factory가 주입하고 prefab에 scene object 참조를 저장하지 않는다. 목적지/provider/용량 등록은 인스턴스별이며 destroy/unregister의 반복도 안전해야 한다. 등록 실패·재시도·Missing Script/GUID/child provider·초기화 순서를 검사한다. 실제 NPC 이용과 화면은 자동 검증하지 않는다. 동적 로딩/철거 gameplay는 후속이며 이 책임을 선행 확대하지 않는다.

기존 GuardTest의 scene-native 농장은 씨앗 선택 source가 없는 구성을 유지한다. 새로 생성하는 Soil prefab 농장에는 collider와 같은 오브젝트의 FarmSeedSource가 필수다. GuardTest의 기존 Storage는 같은 오브젝트를 용량 제공자로 이관하며, 기존에 없던 입고 목적지나 클릭 collider를 추가하지 않는다.

비-Play 단위 검사는 생산 코드의 정리 콜백을 명시적으로 호출하여 등록 해제·반복 정리·다른 시설 보존을 검증한다. Unity가 실제 플레이에서 삭제 콜백을 전달하는 동작은 이 검사 범위가 아니며 사람의 확인 항목으로 남긴다.
