# Navigation

## 건축가 배회 위치 조회 — 2026-09-26

`TilemapNavigation.TryGetRandomReachablePosition(start, random, out position)`은 초기화 시 캐시한 이동 가능 ground node 목록에서 주입된 `IRandomSource`로 최대 16개 후보를 추출한다. 현재 위치와 같은 후보를 제외하고 기존 `TryBuildPath`가 성공한 위치만 반환한다. 실패 시 position은 기본값이며 selector가 Idle로 대기한다. Disable 시 후보·임시 경로 cache도 지운다. `INavigationService` 계약과 기존 지역/출입구/차단 규칙은 유지한다. 동적 건물 배치에 따른 그리드 갱신은 추가하지 않았다. 소비자는 [Builder](Builder.md)다.

## 책임 범위

Tilemap의 지형을 노드 그리드로 만들고 고정 목적지까지 이동할 waypoint를 제공한다.
목적지 선택과 시설 transaction은 기존 selector/provider가, 이동 실행은
[Movement](NPC_Decision_and_Actions/Movement.md)가 소유한다. 경로 비용은 이동 속도를 바꾸지 않는다.

## 주 소유 production 코드

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/System/Navigation/NavigationGrid.cs` | 불변 셀 비용, 좌표/index 변환과 입력 검증 |
| `Assets/Scripts/System/Navigation/AStarPathfinder.cs` | 8방향 A*, 재사용 workspace, 경로 복원 |
| `Assets/Scripts/System/Navigation/AStarOpenSet.cs` | 내부 indexed binary min heap, decrease-key |
| `Assets/Scripts/Actor/TilemapNavigation.cs` | 씬별 그리드와 지역/출입구/건물 footprint 조립, 경로 질의 |
| `Assets/Data/ScriptableObject/Script/TileNavigationProfile.cs` | TileBase별 통행 가능 여부와 정수 비용 definition |
| `Assets/Scripts/Interface/INavigationService.cs` | 준비 상태와 waypoint 질의 계약 |
| `Assets/Scripts/Enum/NavigationFailure.cs` | 경로 실패 원인 |

## 변경 유형별 최소 확인 범위

- 탐색/힙: NavigationGrid, AStarPathfinder, AStarOpenSet, `Assets/TestOnly/Editor/NavigationTests.cs`.
- 씬/지형: TilemapNavigation, TileNavigationProfile과 같은 이름의 asset, `Assets/Scenes/FarmerTest.unity`.
- 이동 연결: INavigationService와 Movement, Selector and Queue, Combat/Guard 문서.

## 알고리즘과 메모리

셀당 노드 하나, 상하좌우 비용 10, 대각 비용 14에 도착 셀 지형 비용을 곱한다.
양쪽 직교 셀이 모두 열려 있을 때만 대각 이동한다. heuristic은 최소 지형 비용을
곱한 octile distance다. F, H, node index 순서로 동점을 처리하므로 결과가 결정적이다.
OpenSet은 index 배열을 가진 이진 최소 힙이다. 전체 목록 정렬이나 중복 삽입을 하지 않는다.
비용은 long이며 NavigationGrid 생성 시 음수와 누적 비용 overflow 가능성을 거부한다.
배열은 그리드 크기별로 재사용하고 반환 List는 호출자가 소유한다. NPC마다 전체 그리드를 복제하지 않는다.

## 지형 definition

`TileNavigationProfile.asset`의 TileBase 참조가 유일한 지형 분류다.
흙 1, 잔디 3, 경계 혼합 타일 2이며 물/물가/빈 셀/미등록 타일은 통행 불가다.
파일명과 그림 픽셀은 runtime 판단에 사용하지 않는다. 더 싼 길을 선호하지만 잔디도 통행 가능하다.
중복/누락 참조와 잘못된 비용은 초기화 오류다. 비용 lookup은 scene이 소유하며 SO를 변경하지 않는다.

## 지역과 경로 조립

지역은 BoxCollider2D 범위와 출입 Transform 하나다. 지역 범위를 셀 경계까지 바깥쪽으로 맞춰
부분 셀의 판정 불일치를 막는다. provider의 원래 작업 범위와 seeded random은 바꾸지 않는다.
지역 내부에는 막힌 셀이나 건물 footprint가 없어야 하며 지역끼리 겹칠 수 없다.
출입구가 속한 셀의 중심을 연결점으로 사용한다. 출입 셀 외 지역 셀은 외부 탐색망에서 제외한다.

같은 지역 또는 같은 셀은 목적지 한 점을 반환하고 A*를 호출하지 않는다.
지역 밖 이동은 출발 지역 출입구까지 직선, 외부 그리드 A*, 도착 지역 출입구에서 목표까지 직선이다.
외부 좌표는 실제 셀을 사용하며 도달 불가능한 목표를 가까운 셀로 몰래 교체하지 않는다.
실패 시 output은 비우고 InvalidStart/InvalidDestination/NoPath/InvalidConfiguration을 반환한다.

## Unity 구성과 수명

`FarmerTest/Systems/Navigation`이 기존 root Grid의 Tilemap과 profile을 참조한다.
농장 및 GuardPatrolArea의 NavigationEntrance 자식과 기존 BoxCollider를 지역으로 연결한다.
창고, 여관, 식당, 시청, 초소, 상단의 NavigationFootprint 자식 BoxCollider는 데이터 전용이며 disabled다.
기존 건물과 월드 hierarchy를 유지한다. selector 두 개에서 Navigation 모드와 scene 참조를 지정한다.
NPCGirl prefab과 NPCComponent에는 새 참조가 없다.

Awake 또는 첫 질의에서 snapshot을 만들고 disable 시 비운다. 활성화 후 첫 질의는 다시 초기화한다.
정사각형, 회전 없는 XY Grid만 지원한다. Play 중 타일/건물 변경 반영과 NPC 회피는 이번 범위 밖이다.
SearchCount는 실제 A* 요청 수, LocalRouteCount는 직접 경로 수다. 선택 Gizmo는 노드와 출입구를 표시한다.

## 검증

`Tools/NPC/Navigation/Run Checks`는 순수 Dijkstra 비교와 현재 씬 지역/시설 질의를 실행한다.
`TestOnly!!/NavigationProbe`는 지정한 두 Transform의 경로와 카운터를 확인한다.
순수 알고리즘/host 검사와 저장 YAML 검사만으로 실제 Play Mode 행동을 검증했다고 주장하지 않는다.
현재 실행 기록과 결과는 [PROGRESS](../Status/PROGRESS.md)에서 연결한다.
