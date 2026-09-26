# Builder

## 기능 목적과 현재 상태

전용 건축가 시민의 배회와 생활 행동을 소유한다. 실제 건축·수리는 후속 범위다.
2026-09-26 Unity CLI에서 `BuilderCitizenSetup.Setup`을 실행해 FarmerTest와 공유 프리팹에 배선을 저장했다. FarmerTest에는 Builder selector와 생성 entry가 있고 시청에는 세 번째 모집 카드가 있다. 자동 Play Mode·화면 검증은 사용자 결정으로 제외하며 실제 플레이와 망치 손 위치는 사람의 확인 항목이다.

## 책임 경계와 흐름

`BuilderActionSelector` → 기존 `DestinationDecider` → Eat/Drink/Sleep이면 Navigated Move + 기존 생활 action. 긴급 욕구가 해소 불가능하면 Idle로 대기한다. 그 외에는 `TilemapNavigation.TryGetRandomReachablePosition`으로 배회 위치를 정하고 `WanderAction` 하나를 대여한다.

`WanderAction`은 `NPCPathFollower`로 최대 5초 이동하고 1초 쉰다. 도착이 빠르면 바로 휴식한다. 이동과 휴식 중 허기·갈증·피로를 각각 0.3/초 증가시키고 완료 후 selector가 재판단한다. 경로 실패는 follower의 재판단 규칙을 따르며 Stop/Clear에서 경로·timer·context를 정리한다. 공유 SO에 actor별 상태를 쓰지 않는다.

Selector는 생활 utility를 복제하지 않는다. 시설 transaction은 기존 provider, queue 수명은 WorkerNPC, 경로 조회는 Navigation, 이동·망치 표시는 NPCComponent가 소유한다.

## 주 소유 스크립트

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/System/Actor/BuilderActionSelector.cs` | 생활/배회 queue 선택·대여·실패 반환 |
| `Assets/Scripts/System/Action/WanderAction.cs` | 배회 이동·휴식·욕구 변화·실행 수명 |
| `Assets/Data/ScriptableObject/Script/WanderActionCost.cs` | 이동/휴식 시간과 욕구 증가 수치 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 우선순위·생활 복귀 | BuilderActionSelector, Decision Policy, Needs Actions |
| 배회 실행·pool 재사용 | WanderAction, WanderActionCost, Movement, NPC Runtime |
| 도달 가능 위치 | [Navigation](Navigation.md), TilemapNavigation |
| 망치·역할 전환 초기화 | [NPC Presentation](NPC_Presentation.md), NPCComponent, WorkerNPC, NPCGirl.prefab |
| 모집·직업 등록 | [Town Hall](Town_Hall.md), [Spawning and Pooling](Spawning_and_Pooling.md), FarmerTest |

## Unity 조립과 검증

`Assets/TestOnly/Editor/BuilderCitizenSetup.cs`의 명시적 메뉴 `Tools/NPC/Builder/Setup`이 BuilderStatContext, WanderActionCost 에셋 및 scene selector/random source/creation entry를 만든다. 기존 공유 pool과 NPCGirl, 시청 transaction을 재사용한다. Farmer는 호미, Builder는 `builder-hammer.png`, 미등록 역할은 도구 숨김이다. 세 번째 모집 카드는 100골드/60초다.

위 구성은 실제 prefab/scene에 저장돼 있다. BuilderActionSelector는 기존 ActionSelector 묶음 아래에 있고 Farmer와 같은 공통 dependency를 참조한다. 독립 SeededRandomSource의 seed는 260926이다. BuilderStatContext와 WanderActionCost는 별도 에셋이며, 망치는 900 PPU로 import됐다. Editor helper는 import 시 자동 실행되지 않는다. 새 asset GUID와 sprite slice는 Unity API가 생성한다.

배선 적용 증거는 `.harness-runs/builder-cli-wiring-20260926/`에 보관한다. Unity에서 저장된 참조·세 직업 카드·모집 수치를 읽어 확인하며, FarmerScene.Structure v1과 독립 리뷰의 최종 결과는 PROGRESS에 기록한다. 모집부터 배회/생활 복귀까지의 플레이, 망치 손 위치와 반복 pooling은 미검증인 사람 확인 항목이며 배선 구현 완료를 막지 않는다. [승인 계획](../Plans/Builder_Citizen_Implementation_Plan.md)에 수용 조건과 후속 TBD를 기록한다.
