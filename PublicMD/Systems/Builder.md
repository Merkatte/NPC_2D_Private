# Builder

## 기능 목적과 현재 상태

전용 건축가 시민의 건설과 생활/배회 queue 선택을 소유한다. 공통 배회 실행은 [Needs Actions](NPC_Decision_and_Actions/Needs_Actions.md)가 소유한다. 건설 변경의 현재 검증 상태는 PROGRESS를 따른다.
2026-09-26 Unity CLI에서 `BuilderCitizenSetup.Setup`을 실행해 FarmerTest와 공유 프리팹에 배선을 저장했다. FarmerTest에는 Builder selector와 생성 entry가 있고 시청에는 세 번째 모집 카드가 있다. 자동 Play Mode·화면 검증은 사용자 결정으로 제외하며 실제 플레이와 망치 손 위치는 사람의 확인 항목이다.

## Defense 유지보수

선택적 `_maintenance`가 연결되면 공사보다 먼저 [Defense maintenance](Defense/Durability_and_Maintenance.md)의 가용 site를 예약해 Maintain action을 구성한다. 도주는 Base selector의 DefenseResponsePolicy가 생활/업무보다 먼저 판단한다. 태업·긴급 욕구는 기존 규칙을 유지한다. 현재 공사 중 유지보수가 생기거나 현재 Maintain보다 높은 rank가 생기면 queue를 반환하고 재선택한다. 기존 공사와 site progress는 각각 owner가 보존한다. `_maintenance` 미연결 scene의 FIFO 공사는 동일하다.

## 책임 경계와 흐름

`BuilderActionSelector` → 기존 `DestinationDecider` → Eat/Drink/Sleep이면 Navigated Move + 기존 생활 action. 긴급 욕구가 해소 불가능하면 Idle로 대기한다. 그 외에는 BuildingPlotRegistry에서 신청 순서로 가용 공사를 찾아 이동 전에 예약한다. 정원이 찬 부지는 건너뛴다. 공사가 없으면 `TilemapNavigation.TryGetRandomReachablePosition`으로 배회 위치를 정하고 `WanderAction` 하나를 대여한다.

태업이면 기본적으로 공통 생활/배회 queue를 선택한다. 단, Homeless 기여분을 제외한 불만이 태업 기준 미만이면 House 신축(IsUpgrade=false)만 후보에 포함한다. 입주 후 회복 중인 Homeless 잔량도 제외하며 실제 태업·불만 상태는 유지한다. 업그레이드·다른 공사·유지보수에는 예외가 없다. 선택된 생활 action 또는 해소 불가 긴급 욕구의 Idle이 공사보다 우선하고, 그 외 배회를 BaseNPCActionSelector가 조립한다. Wander 실행 규칙은 [Needs Actions](NPC_Decision_and_Actions/Needs_Actions.md)를 따른다.

Selector는 생활 utility를 복제하지 않는다. 시설 transaction은 기존 provider, queue 수명은 WorkerNPC, 경로 조회는 Navigation, 이동·망치 표시는 NPCComponent가 소유한다.

## 주 소유 스크립트

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/System/Actor/BuilderActionSelector.cs` | 생활/건설/배회 queue 선택·예약·대여·실패 반환 |
| `Assets/Scripts/System/Actor/BuilderWorkPolicy.cs` | selector와 실행 action이 공유하는 태업 중 House 신축 자격 조회 |
| `Assets/Scripts/System/Action/BuildAction.cs` | NPCPathFollower 이동·도착 후 작업·중단과 lease 정리 |
| `Assets/Data/ScriptableObject/Script/BuildActionCost.cs` | CSV 작업 설정과 공통 긴급 욕구 판단 연결 |

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

## 건설 실행

BuildAction 하나가 이동과 작업을 소유한다. selector는 plot provider, reservation, 고정 작업 위치를 ActionContext/InteractionRequest로 전달한다. 첫 대여/인계 실패는 selector가 반환하고 인계 후 완료·실패·취소·pool Clear는 action이 lease를 반환한다. 부지는 매 작업 기여마다 lease와 현재 공사를 검증한다.

작업 진행은 도착 후 CSV 작업 속도 × 유효 작업 시간이다. 이동 중에는 진행하지 않는다. 이동/작업 중 CSV 욕구 비용을 적용하며 기존 긴급 욕구 기준으로 작업을 중단한다. MoveAction·WorkerNPC에 건축가 전용 분기를 추가하지 않는다. 완료 실패는 100%를 보존하고 추가 작업 없이 TestOnly에서 재시도한다.

Builder selector의 _buildingPlots/_buildCost를 scene registry와 BuildActionCost에 연결한다. 새 BuildingTest 시작 건축가 2명은 TestOnly bootstrap이 생성한다. 건축가 개별 전문 스탯은 확장 가능성만 유지하고 현재 공유 고정 설정을 사용한다. 부지·정의·완공 책임은 [Construction](Construction/README.md)을 따른다.

## 불만도·태업 연결

Build context는 RequiresWorkAvailability=true다. Start/Tick의 work predicate는 BuilderWorkPolicy를 사용해 태업 중 House 신축 자격을 현재 상태로 확인한다. 예외가 사라지면 이동·욕구 비용·작업 기여 이전에 재판단하며 BuildAction.Cleanup이 lease와 경로·도구 상태를 반환한다. 정책의 공사 자격은 CanReserve와 분리되어 자신의 예약이 마지막 슬롯이어도 실행을 계속한다. 이미 기여한 부지 progress는 보존된다. 회복 뒤 다음 선택에서 새 예약을 얻는다.

## 주거 연결

HasAvailableWork는 navigation/cost가 준비되고 정책상 허용되며 예약 가능한 부지가 있는지만 읽으며 예약하지 않는다. 실제 queue 생성도 같은 정책으로 후보를 거른 뒤 기존 FIFO 예약을 수행한다. 주거가 주입되면 공사가 없을 때 귀가하고 새 공사가 생기면 selector hook으로 기존 생활/공사 판단을 다시 실행한다. 회복 속도는 명시적으로 전달한 HousingLifeSettings를 decider와 SleepAction이 함께 사용한다. [Housing Life](Housing/Life.md)를 따른다.
