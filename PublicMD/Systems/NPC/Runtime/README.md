# NPC Runtime

NPC 개체의 생성·초기화·반환과 행동 실행 상태를 안내한다. 행동 선택 정책과 Unity 표현은 각각 형제 기능이 소유한다.

| 변경 대상 | leaf |
|---|---|
| 현재 action·queue 수명, 공통 stat, 실행 중단·재개 | [Lifecycle and Stats](Lifecycle_and_Stats.md) |
| role 조립, prefab catalog, 예약·커밋·취소, pool | [Spawning and Pooling](Spawning_and_Pooling.md) |
| 불만 원인·누적·회복·태업, 개체별 시간 갱신 | [Dissatisfaction](Dissatisfaction.md) |

생성 기능이 개체별 stat과 selector를 조립하면 runtime이 queue를 소비한다. 불만 상태는 개체별로 유지하고 판단 기능이 읽는다. 취소·disable·pool 반환은 남은 action과 연결 상태를 정리한다.

[판단과 실행](../Decision_and_Actions/README.md), [표현](../Presentation/README.md)과의 수명 경계가 바뀌면 이 인덱스와 해당 leaf를 갱신한다.
