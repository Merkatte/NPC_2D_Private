# Movement

## 책임 범위와 주 소유 코드

| 파일 | 책임 |
|---|---|
| `Assets/Data/Struct/MoveRequest.cs` | 고정/동적 목적지, Direct/Navigation 모드, 최종 정지 거리 |
| `Assets/Scripts/Enum/MoveMode.cs` | Direct=0, Navigation=1 직렬화 계약 |
| `Assets/Scripts/Interface/IMoveTarget.cs` | 동적 target의 위치/유효성 |
| `Assets/Scripts/System/Actor/NPCPathFollower.cs` | action별 waypoint cursor, 실제 이동, 이탈 재탐색과 실패 대기 |
| `Assets/Scripts/System/Action/MoveAction.cs` | 이동 실행 결과를 action 완료/재판단으로 변환 |

## 변경 유형별 최소 확인 범위

요청/이동 변경은 위 다섯 파일, ActionContext, NPCComponent.Move와 WorkerNPC의 Stop/Clear 호출을 확인한다.
지역/탐색 변경은 [Navigation](../Navigation.md), Guard 내부 순찰은 [Guard](../Combat/Guard.md)를 함께 확인한다.

## 요청과 실행

selector는 MoveRequest.Fixed/Dynamic/Navigated 중 하나를 명시한다.
Fixed와 Dynamic은 Direct, Navigated는 Navigation이다. 기본 최종 정지 거리는 0.1이다.
ActionContext는 선택적인 INavigationService만 전달한다. service locator나 action queue를 노출하지 않는다.
MoveAction은 요청이 없으면 실패하며 암묵적인 원점 이동을 만들지 않는다.
기존 Dynamic은 매 Tick 위치를 다시 읽는 Direct 흐름이다. Navigated(IMoveTarget, stoppingDistance)는 target 위치를 경로망으로 추적한다.

NPCPathFollower.Begin은 이전 상태를 지우고 경로를 요청한다. 같은 지역 안에서는 navigation service가
직접 경로를 반환하므로 seeded random 목적지를 이용한 작업/순찰에 A*가 호출되지 않는다.
경로 조회는 gameplay 난수를 소비하지 않는다.
Tick은 stat 속도와 Time.deltaTime으로 이번 frame 이동 예산을 계산한다. 중간점에 최종 정지 거리를
적용하지 않으며, 남은 예산으로 다음 구간을 진행해 긴 frame에서도 코너를 건너뛰지 않는다.
실제 위치와 Flip/animation은 기존 NPCComponent adapter를 통해 변경한다.

## 실패와 lifecycle

외력으로 예상 위치를 벗어나면 지역 내부 직선 구간에서도 현재 위치에서 다시 질의한다.
경로 실패는 gameplay 시간 1초 대기 후 RequiresReplan을 반환한다. 그동안 반복 탐색하지 않는다.
실패한 MoveAction은 뒤의 시설 행동을 실행하지 않고 WorkerNPC의 기존 재판단 계약을 사용한다.
Direct target 소실은 즉시 재판단한다. Stop/Clear/다음 Begin에서 경로, cursor, timer와 모든 참조를 지운다.
WorkerNPC와 NPCComponent에 탐색 책임이나 별도 실행기를 추가하지 않는다.
GuardAction도 자기 follower를 소유하지만 감지/욕구/시설 유효성 검사는 매 Tick 먼저 수행한다.

## 불만도·태업 연결

MoveAction은 RequiresWorkAvailability=true인 업무 이동/Guard 추적만 공통 시작·Tick 검사로 중단한다. 검사는 follower 이동 이전이며 ReplanRequested 후 WorkerNPC의 기존 Stop/Clear가 경로와 이동 표현을 정리한다. 생활 시설 이동과 Enemy 추적은 false로 유지된다.

## 동적 경로와 topology

NPCPathFollower는 optional INavigationRevision을 Begin에서 캐시하고 path 생성 시 revision/목표 위치를 저장한다. revision 변화, 실제 위치 이탈 또는 동적 target의 0.5유닛 이상 이동에 현재 위치에서 다시 경로를 만든다. target 소실은 재판단하며 Clear는 새 cache도 제거한다. topology는 [Navigation](../Navigation.md), Defense target 선택은 [Defense response](../Defense/Battlefield_and_Response.md)가 소유한다.
