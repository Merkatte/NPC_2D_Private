# Builder

## 기능 목적과 현재 상태

전용 건축가 시민의 배회와 생활 행동을 소유한다. 실제 건축·수리는 후속 범위다.
2026-09-26 코드와 망치 이미지 후보를 작성했으며, 사용자 지시에 따라 Unity 조립·Play 검증은 보류했다. 현재 FarmerTest에는 아직 Builder 생성 entry가 없다.

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

위 설명은 Setup의 적용 내용이며 아직 실제 prefab/scene에 저장된 상태를 뜻하지 않는다. Editor helper는 import 시 자동 실행되지 않는다. 새 asset GUID와 sprite slice는 Unity API가 생성한다.

검증 대기: import·실제 배선, 모집부터 배회/생활 복귀까지 Play, 망치 손 위치, 반복 pooling, FarmerScene.Structure v1과 독립 후보 acceptance. [승인 계획](../Plans/Builder_Citizen_Implementation_Plan.md)에 수용 조건과 후속 TBD를 기록한다.
