# Grid Navigation 변경 보고

노드 그리드 A*와 binary min heap을 적용했다. 힙은 OpenSet의 최소 우선순위 노드 추출에 사용한다.
같은 농장·초소 지역 안에서는 직접 이동하며 seeded random/provider는 수정하지 않았다.
흙 1, 잔디 3, 혼합 2의 비용으로 경로를 고른다. 전투 추적은 Direct다.

production 신규 9개 926줄, 기존 6개 +111/-104줄, TestOnly 신규 2개 224줄.
메타/에셋/문서 포함 전체 변경은 44개 파일이다. 최종 검증 상태는 verification-status.json과
review.json, gate-results/review-evidence.json을 함께 확인한다.

## 코드별 변경

| 파일 | 추가 | 삭제 | 책임/변경 |
|---|---:|---:|---|
| `Assets/Data/Struct/ActionContext.cs` | 3 | 1 | 선택적인 INavigationService 전달 |
| `Assets/Data/Struct/MoveRequest.cs` | 9 | 2 | Navigated factory와 이동 모드, 기본 정지 거리 |
| `Assets/Scripts/System/Action/GuardAction.cs` | 32 | 13 | 감지/욕구 검사 순서를 유지하며 순찰/복귀에 follower 사용 |
| `Assets/Scripts/System/Action/MoveAction.cs` | 17 | 83 | 중복 이동 로직을 follower로 옮기고 action 결과만 연결 |
| `Assets/Scripts/System/Actor/FarmerActionSelector.cs` | 25 | 3 | 시설 이동 context에 Navigation 요청을 명시 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | 25 | 2 | 전투 Direct 우선순위를 유지하고 시설/초소 이동에 Navigation 전달 |
| `Assets/Data/ScriptableObject/Script/TileNavigationProfile.cs` | 72 | 0 | 명시적인 TileBase 참조와 지형 비용 definition |
| `Assets/Scripts/Actor/TilemapNavigation.cs` | 351 | 0 | 씬 그리드·지역·출입구·건물 footprint 조립과 경로 질의 |
| `Assets/Scripts/Enum/MoveMode.cs` | 5 | 0 | Direct/Navigation 요청 구분 |
| `Assets/Scripts/Enum/NavigationFailure.cs` | 8 | 0 | 경로 실패 원인 |
| `Assets/Scripts/Interface/INavigationService.cs` | 11 | 0 | 경로 조회의 최소 계약 |
| `Assets/Scripts/System/Actor/NPCPathFollower.cs` | 186 | 0 | action별 경로 추종, 외부 이탈 재탐색, 실패 대기와 reset |
| `Assets/Scripts/System/Navigation/AStarOpenSet.cs` | 117 | 0 | 중복 노드 없는 indexed binary min heap |
| `Assets/Scripts/System/Navigation/AStarPathfinder.cs` | 112 | 0 | 8방향 가중 A*, 코너 차단, 재사용 배열 |
| `Assets/Scripts/System/Navigation/NavigationGrid.cs` | 64 | 0 | 불변 노드 비용과 좌표/index 검증 |
| `Assets/TestOnly/Editor/NavigationTests.cs` | 161 | 0 | Dijkstra 비교와 실제 씬 질의용 Editor 메뉴 |
| `Assets/TestOnly/TestNavigationWindow.cs` | 63 | 0 | 지정 지점 경로와 탐색 횟수·Gizmo 확인 |

## 씬과 definition

FarmerTest에 Systems/Navigation, 농장·초소 NavigationEntrance, 건물별 NavigationFootprint,
TestOnly!!/NavigationProbe를 배치했다. 기존 hierarchy와 source prefab은 유지했다.
TileNavigationProfile.asset에 실제 타일 참조를 등록하고 selector 두 곳에 scene service를 연결했다.

## 책임과 인식 부채

탐색은 순수 Navigation 폴더, scene 조립은 TilemapNavigation, 실행은 NPCPathFollower,
행동 선택은 기존 selector가 담당한다. WorkerNPC/NPCComponent/provider의 책임을 늘리지 않았다.
새 production 파일은 Navigation 또는 Movement 문서 하나에서 주 소유한다.
공통 service locator나 범용 경로 framework, JPS/NavMesh 의존은 추가하지 않았다.

## 증거의 한계

순수 검사, 실제 소스를 사용하는 host 검사, 저장된 YAML 경로 검사와 Unity 구조 Gate를 구분한다.
사용자는 Play에서 정상 이동을 확인했다. 모든 pool/reinit/실패 분기를 실제 Play에서 별도로
계측한 것은 아니며 host와 직접 코드 검토가 해당 보충 근거다.
Play 중 지형/건물 변경 갱신과 주민끼리의 회피는 지원 범위 밖이다.

전체 변경 파일/줄 수: change-inventory.md.
