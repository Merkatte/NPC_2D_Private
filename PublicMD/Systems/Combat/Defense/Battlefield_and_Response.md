# Defense Battlefield and Response

## 목적과 책임

DefenseTest 전투의 actor 상태와 긴급 우선순위를 소유한다. NPCStat은 유일한 NPC HP owner이고 CombatTarget은 피격 adapter다. WorkerNPC는 정책 없이 실행 중단·queue 반환만 수행한다. 공유 selector에 NPC별 target·timer·자리 상태를 저장하지 않는다.

## 현재 흐름

NPCManager의 예약 또는 웨이브 생성이 DefenseActor.BindBattlefield 후 WorkerNPC.Init을 호출한다. Actor는 stat과 target을 연결하고 Battlefield에 등록한다. BaseNPCActionSelector의 주거 overload와 재판단 hook이 DefenseResponsePolicy를 가장 먼저 호출하므로 귀가·식사·수면·태업 중에도 전투/도주가 우선한다. 긴급 상황이 없으면 기존 생활 규칙으로 돌아간다.

- 검객(기존 Guard=1)·궁수(Archer=5): 전장에 등록된 살아 있고 공격 가능한 적 전체에서 현재 위치 기준 2D 직선거리가 가장 가까운 대상을 선택한다. `FindEnemy(position, PositiveInfinity, false)`를 재사용하며 성벽 안팎·감지 반경으로 후보를 제한하지 않는다. 기존 0.25초 재판단으로 더 가까운 적과 대상 사망·소실에 대응하되 같은 대상의 Move/DefenseAttack은 재시작하지 않는다.
- 두 병사는 사거리 밖이면 Friendly navigation의 기존 동적 Move로 사거리의 85%까지 접근하고 사거리 안이면 현재 위치에서 공격한다. 궁수는 벽 위에서도 사거리 내 적에게 발사한다. 경로가 없으면 기존 follower의 1초 실패 대기·재판단을 유지하며 추격용 직선 이동·순간이동은 없다.
- 병사와 교전 상대를 유지 중인 적은 같은 대상의 Move 중에도 기존 재판단 주기마다 대상의 현재 위치로 사거리를 확인한다. 사거리 안이면 Move queue를 반환하고 같은 대상의 DefenseAttack으로 전환한다. 공통 follower의 캐시된 목적지·중간 waypoint 도착을 기다리며 서로 지나치거나 방향을 반복 전환하지 않도록 전투 시작 조건을 현재 거리와 일치시킨다. 사거리 밖의 같은 대상 Move와 진행 중인 DefenseAttack은 유지하며, 공통 이동의 재탐색 거리·실패 대기와 공격 주기·사거리는 바꾸지 않는다.
- 궁수가 추격해야 하거나 오르내리는 중 전투로 전환하거나 서 있던 벽이 파괴되면 `DefenseStationAction`의 ground-exit으로 자신의 지상 출입 지점까지 내려온다. 이 액션에는 욕구 비용이 없고 대상 변경·사망이나 벽 복구로 하강을 반복 취소하지 않는다. 지상 도착 즉시 완료하고 새 queue에서 다시 대상을 선택한다. 벽 복구가 전투 중 원래 자리 복귀를 강제하지 않는다.
- `DefenseActor`의 비직렬화 Ground/Climbing/Wall/Descending 상태는 station action의 취소·pool 반환에도 남으며 actor Initialize/Unbind에서 초기화한다. 진행 중 access만 직접 오르내림을 허용한다. 기존 평시 생활 navigation이 완료된 벽 자리에서 벗어났다면 다음 정책/자리 판단에서 실제 top anchor와 거리를 비교해 Ground로 보정하여 먼 위치에서 잘못 직접 하강하지 않는다. action-local follower는 Stop/Clear에서 지워지고 Downed의 queue 취소로 이동도 중단된다.
- 전투는 욕구·태업·최초 귀가보다 우선한다. 대상이 모두 사라지면 전투 중 벽 위에 남은 궁수와 취소된 access는 먼저 지상으로 내려온 뒤 기존 생활·경계 판단으로 돌아간다. 평시 duty의 비용과 도착 후 0.5초 완료는 유지한다. `IsGroundExitActive`와 기존 `IsEmergencyDuty`는 station 시작·종료/취소·pool 반환과 actor Initialize/Unbind/Downed에서 정리한다. 궁수의 자리 예약은 추격 중에도 유지한다.
- 외부 적: 근처 외부 검객 → 이미 타격을 시작한 벽 → 도달 가능한 breach → 도달 가능한 벽. 외부 검객을 선택하면 `DefenseActor.EngagedOpponent`에 교전을 보존하고, 더 가까운 검객이나 적/상대의 성벽 내부 경계 통과만으로 대상을 바꾸지 않는다. 같은 대상의 Move/DefenseAttack도 재시작하지 않는다. 외부 신규 획득은 기존 Guard 역할과 `LocalCombatRange`(기본 6)로 제한하고 유지 거리는 `EnemyDisengageRange`(임시 기본 9)로 분리한다. 해제 거리는 Inspector와 runtime getter에서 획득 거리보다 최소 0.1 크게 보정하며, 기존 asset에 새 필드가 없거나 양수가 아니면 기본 9를 사용한다. 최종 밸런스는 TBD다.
- 교전 대상의 Downed·사망·소실·비활성화·target 불가, 해제 거리 초과 또는 도달 불가 시 교전을 해제한다. 획득과 유지 모두 공격 사거리 안이면 기존 근접 규칙대로 허용하고, 밖이면 Enemy navigation의 실제 경로가 있어야 한다. 따라서 도달 불가로 해제한 대상을 같은 조건에서 다시 획득하지 않는다. 경로 불가는 정책의 재판단 시 해제하며 공통 MoveAction/follower의 1초 실패 대기 자체는 변경하지 않는다. 교전 해제 뒤 현재 위치가 외부면 보존한 벽 commitment와 기존 breach/벽 선택 순서를 재개하고, 내부면 기존의 도달 가능한 주민/시설 거리 비교를 사용한다. 교전하지 않는 적의 내부 진입은 기존대로 의도를 다시 선택하고 breach는 실제 선택이 바뀔 때만 재판단한다. 교전 참조는 actor Initialize/Unbind/Downed에서 지운다.
- 민간인: 벽 개방 또는 내부 적이 있는 경보 상태에서만 근거리 적을 피한다. 탐색 가능한 내부 도주 후보를 선택하고 위험이 사라지면 기존 selector로 복귀한다. 작업 예약은 queue 취소로 해제되고 cargo는 남는다.

DefenseAttackAction은 공격 주기의 설정 비율에서 한 번 타격/발사한다. 근접 벽 commitment는 실제 hit에서 생긴다. 화살은 발사 시 target+Unity owner를 고정하고 이동 후 한 번 피해를 준다. target 소실/Downed이면 사라지고 새 target을 찾지 않는다.

선택적 `DefenseActor._enemyAttackDefinition`이 연결된 Enemy는 같은 `DefenseAttackAction`에서 별도 공격 주기를 실행한다. GoblinArrow는 눌렀다가 튀어오를 때 화살 1개, ShamanEnergyBall은 빠른 상하 변형과 구체 충전 뒤 에너지 구체 1개, SalamanderFireBurst는 느린 들숨 뒤 불덩이를 짧게 연속 발사한다. 액션이 windup·release·burst count·recovery와 실제 발사를 소유하며, 표현 adapter에는 액션의 경과 시간만 전달한다. 기본 임시값은 고블린 0.45초/0.55초, 샤먼 0.8초/0.6초, 샐러맨더 1초/0.65초(windup/recovery)이며 샐러맨더는 0.12초 간격 5발이다. 모든 projectile은 기존 지정 대상 추적과 1회 피해 규칙을 사용하고 광역·화상은 없다. 발사 때 벽 commitment를 기록한다.

새 공격은 각 Tick에 기존 stat 사거리를 확인하고 대상 변경·소실·사거리 이탈 시 미발사분을 취소하고 재판단한다. Stop/Fail/RequestReplan/Clear와 actor disable/Unbind/Downed는 몸 자세와 충전 표시를 정리한다. 이미 발사한 projectile은 기존 수명 규칙을 유지한다. 프레임 지연으로 여러 release 시점을 지난 경우 액션은 아직 발사하지 않은 수량만 따라잡는다. `Time.deltaTime`과 action pause가 진행을 멈추며 animation event는 피해를 만들지 않는다. profile 없는 slime·검객·궁수는 기존 AttackSpeed/HitTimeRatio 분기를 그대로 사용한다.

HP0 친화 actor는 활성 GameObject를 유지한 Downed가 된다. 현재 action/queue를 반환하고 target 불가, 불만 갱신 정지, 자세 변경만 수행한다. stat·cargo·주거·궁수 자리·등록은 보존한다. 적은 등록 해제 후 Destroy한다. 치료·자동 HP 회복·부활은 구현하지 않는다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Actor/DefenseActor.cs` | NPC별 전투 등록·Downed·flee 전이·적 교전 상대·벽 commitment·자리와 access 전환 상태 |
| `Assets/Scripts/Enum/DefenseActorState.cs` | Active/Downed 값 |
| `Assets/Scripts/Manager/DefenseBattlefield.cs` | 명시 등록 actor/시설/벽, 경보·거리·경로 후보 조회와 궁수 자리 예약 |
| `Assets/Scripts/System/Actor/DefenseArcherSlotLease.cs` | 선예약부터 actor disable까지의 궁수 자리 수명 |
| `Assets/Scripts/System/Actor/DefenseResponsePolicy.cs` | 역할별 긴급 판단과 combat/flee queue 구성 |
| `Assets/Scripts/System/Actor/DefenseSoldierActionSelector.cs` | 검객·궁수의 평상시 욕구/주거/배회/지정 duty queue |
| `Assets/Scripts/System/Action/DefenseAttackAction.cs` | 기존 단일 hit와 선택적 적 windup·연사·회복 timing, 근접 피해 및 지정 projectile 발사 |
| `Assets/Scripts/System/Action/DefenseStationAction.cs` | 지정 duty 접근·벽 자리 전환·ground-exit 실행·평상시 경비 욕구 비용 |
| `Assets/Scripts/System/Action/FleeAction.cs` | 선택된 도주 경로 실행 |
| `Assets/Scripts/Actor/DefenseProjectile.cs` | 지정 target 이동·도착 피해·소실 종료 |
| `Assets/Data/ScriptableObject/Script/DefenseCombatSettings.cs` | 재판단·근거리 획득·적 교전 해제 거리·도주·hit 비율·화살 속도·도주 대사 tuning |
| `Assets/Data/ScriptableObject/Script/DefenseEnemyAttackDefinition.cs` | 선택적 적 공격 style·windup·recovery·연사 수/간격·projectile 속도 공유 tuning |
| `Assets/Scripts/Enum/DefenseEnemyAttackStyle.cs` | GoblinArrow=0, ShamanEnergyBall=1, SalamanderFireBurst=2 직렬화 값 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 확인 |
|---|---|
| 역할 우선순위 | ResponsePolicy, SoldierSelector, BaseNPCActionSelector, 해당 action |
| 적 교전 유지·해제 | ResponsePolicy, DefenseActor, Battlefield, DefenseCombatSettings, CombatRuntimeState/CombatTargetHandle, DefenseAttackAction, [Movement](../../NPC/Decision_and_Actions/Movement.md) |
| HP0·pool/재초기화 | DefenseActor, WorkerNPC, NPCDissatisfaction, CombatTarget, NPCManager |
| 궁수 자리/모집 거절 | ArcherSlotLease, Battlefield, NPCManager, TownHallRecruitment |
| 병사 추격·궁수 access 전환 | ResponsePolicy, DefenseActor, DefenseStationAction, DefenseAttackAction, WorkerNPC, [Movement](../../NPC/Decision_and_Actions/Movement.md) |
| hit/화살 | DefenseAttackAction, DefenseProjectile, CombatTargetHandle, combat stat |
| 적 공격 archetype | DefenseEnemyAttackDefinition/Style, DefenseAttackAction, DefenseActor, [Presentation](Presentation.md), 새 enemy/projectile prefab과 paired EnemyStatDefinition |
| 통로와 내부 target | Battlefield, ResponsePolicy, [Navigation](../../World/Navigation.md), 조립한 target anchors |

## 배선·불변 규칙·제약

DefenseSoldierActionSelector의 평상시 utility는 GuardDutyDecisionContext로 실제 DutyPosition과 자리 가용성을 전달한다. Defense 전용 `Assets/Data/ScriptableObject/Defense/GuardActionCost.asset`의 Hunger/Thirst/Fatigue는 각 0.3/초이며 기존 interrupt threshold는 보존한다. 공유 GuardActionCost는 변경하지 않고 DefenseSetup도 별도 복사본을 생성한다.

Defense prefab의 worker/target/settings/presentation을 명시 연결한다. NPCManager와 role selector는 같은 Battlefield/ResponsePolicy를 참조한다. Enemy는 Enemy access navigation을 사용하고 기존 주민 생활 경로는 Friendly 기본 경로를 사용한다. 내부 bounds는 disabled BoxCollider의 local geometry를 읽는다. 건물 target anchor는 footprint 밖의 실제 이동 가능한 입구에 둔다. Unity global Find나 target 자동 검색으로 빠진 의존성을 보완하지 않는다.

새 세 prefab은 `Assets/Prefab/Defense/Enemies/DefenseGoblinArcher.prefab`, `DefenseShaman.prefab`, `DefenseSalamander.prefab`이다. `Projectiles/EnergyOrb.prefab`와 `Fireball.prefab`는 기존 Arrow와 같은 DefenseProjectile을 사용한다. 대응하는 `Assets/Data/ScriptableObject/Defense/Enemies/<prefab명>Attack.asset`와 `<prefab명>Stat.asset`가 각각 timing과 stat을 소유한다. 사거리 임시값은 4.5/4/3이며 HP10·발당 공격력2·이동속도2는 기존 EnemyStat에서 복사한다. 새 profile의 공격 주기는 windup + (발수-1)×간격 + recovery이고 stat AttackSpeed는 legacy 공격에만 쓰인다. 최종 밸런스는 TBD다. 새 적을 생성할 때 기존 `BindBattlefield` 후 `WorkerNPC.Init(NPCType.Enemy, pairedStat.CreateRuntimeStat(), 기존 enemy selector)` 흐름을 사용한다. prefab 단독 배치는 자동 초기화되지 않는다. 이 작업은 세 prefab과 대응 에셋만 준비하며 기존 scene, wave controller 및 편성 asset은 변경하지 않는다.

궁수 자리는 비용 지불 전에 예약한다. 벽이 부서졌어도 빈 자리 예약은 가능하고 Downed는 자리를 반납하지 않는다. disable/취소 시에만 해제한다. 전투는 태업에 의해 거절되지 않는다. paused 시간에는 queue 전진과 공격/화살 이동이 정지한다.

실제 이동·공격 자세·Downed·도주 복귀는 사람 플레이 확인 항목이다. 기존 컴파일·정적 검사와 참조 검사는 실행 기록을 따르며 전용 테스트 도구를 추가하지 않는다. 정책·public API·상태 수명·prefab 연결이 변경되면 이 문서를 갱신한다.
