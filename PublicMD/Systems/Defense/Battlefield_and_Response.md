# Defense Battlefield and Response

## 목적과 책임

DefenseTest 전투의 actor 상태와 긴급 우선순위를 소유한다. NPCStat은 유일한 NPC HP owner이고 CombatTarget은 피격 adapter다. WorkerNPC는 정책 없이 실행 중단·queue 반환만 수행한다. 공유 selector에 NPC별 target·timer·자리 상태를 저장하지 않는다.

## 현재 흐름

NPCManager의 예약 또는 웨이브 생성이 DefenseActor.BindBattlefield 후 WorkerNPC.Init을 호출한다. Actor는 stat과 target을 연결하고 Battlefield에 등록한다. BaseNPCActionSelector의 주거 overload와 재판단 hook이 DefenseResponsePolicy를 가장 먼저 호출하므로 귀가·식사·수면·태업 중에도 전투/도주가 우선한다. 긴급 상황이 없으면 기존 생활 규칙으로 돌아간다.

- 검객(기존 Guard=1): 근처 적 → 내부 적 → 살아 있는 적이 남으면 긴급 경계 복귀 → 평상시 생활/원래 외부 경비 위치. 돌아가는 중에도 근처 적이 생기면 중단한다.
- 궁수(Archer=5): 사거리 내 적에게 고정 자리에서 발사한다. 자신의 벽이 열리면 해당 남쪽 ground anchor, 복구되면 같은 top anchor를 사용한다. 자리 접근·오르내림은 station action이며 추격/카이팅은 없다.
- 공격 대상이 없어도 AliveEnemyCount가 양수이면 두 병사 역할 모두 욕구·태업·최초 귀가보다 지정 자리 복귀가 우선이다. 예정 spawn은 포함하지 않는다. 긴급 station에는 GuardActionCost를 적용하지 않고 마지막 적이 사라지면 평상시 판단으로 전환한다. actor-local IsEmergencyDuty는 station 시작·종료/취소·pool 반환 및 actor Initialize/Unbind에서 관리한다. 같은 긴급 station을 주기마다 취소하지 않으며 도착 후 기존 0.5초 완료를 유지해 궁수 공격을 다시 판단한다.
- 외부 적: 근처 외부 검객 → 이미 타격을 시작한 벽 → 도달 가능한 breach → 도달 가능한 벽. 내부 진입 시 의도를 다시 선택하고 내부의 도달 가능한 주민/시설 중 거리만 비교한다. breach 선택은 실제 선택이 바뀔 때만 재판단한다.
- 민간인: 벽 개방 또는 내부 적이 있는 경보 상태에서만 근거리 적을 피한다. 탐색 가능한 내부 도주 후보를 선택하고 위험이 사라지면 기존 selector로 복귀한다. 작업 예약은 queue 취소로 해제되고 cargo는 남는다.

DefenseAttackAction은 공격 주기의 설정 비율에서 한 번 타격/발사한다. 근접 벽 commitment는 실제 hit에서 생긴다. 화살은 발사 시 target+Unity owner를 고정하고 이동 후 한 번 피해를 준다. target 소실/Downed이면 사라지고 새 target을 찾지 않는다.

HP0 친화 actor는 활성 GameObject를 유지한 Downed가 된다. 현재 action/queue를 반환하고 target 불가, 불만 갱신 정지, 자세 변경만 수행한다. stat·cargo·주거·궁수 자리·등록은 보존한다. 적은 등록 해제 후 Destroy한다. 치료·자동 HP 회복·부활은 구현하지 않는다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Actor/DefenseActor.cs` | NPC별 전투 등록·Downed·flee 전이·벽 commitment·고정 자리 상태 |
| `Assets/Scripts/Enum/DefenseActorState.cs` | Active/Downed 값 |
| `Assets/Scripts/Manager/DefenseBattlefield.cs` | 명시 등록 actor/시설/벽, 경보·거리·경로 후보 조회와 궁수 자리 예약 |
| `Assets/Scripts/System/Actor/DefenseArcherSlotLease.cs` | 선예약부터 actor disable까지의 궁수 자리 수명 |
| `Assets/Scripts/System/Actor/DefenseResponsePolicy.cs` | 역할별 긴급 판단과 combat/flee queue 구성 |
| `Assets/Scripts/System/Actor/DefenseSoldierActionSelector.cs` | 검객·궁수의 평상시 욕구/주거/배회/지정 duty queue |
| `Assets/Scripts/System/Action/DefenseAttackAction.cs` | 단일 hit timing, 근접 피해 및 지정 화살 발사 |
| `Assets/Scripts/System/Action/DefenseStationAction.cs` | 지정 duty 접근·벽 자리 전환·평상시 경비 욕구 비용 |
| `Assets/Scripts/System/Action/FleeAction.cs` | 선택된 도주 경로 실행 |
| `Assets/Scripts/Actor/DefenseProjectile.cs` | 지정 target 이동·도착 피해·소실 종료 |
| `Assets/Data/ScriptableObject/Script/DefenseCombatSettings.cs` | 재판단·근거리·도주·hit 비율·화살 속도·도주 대사 tuning |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 확인 |
|---|---|
| 역할 우선순위 | ResponsePolicy, SoldierSelector, BaseNPCActionSelector, 해당 action |
| HP0·pool/재초기화 | DefenseActor, WorkerNPC, NPCDissatisfaction, CombatTarget, NPCManager |
| 궁수 자리/모집 거절 | ArcherSlotLease, Battlefield, NPCManager, TownHallRecruitment |
| hit/화살 | DefenseAttackAction, DefenseProjectile, CombatTargetHandle, combat stat |
| 통로와 내부 target | Battlefield, ResponsePolicy, [Navigation](../Navigation.md), 조립한 target anchors |

## 배선·불변 규칙·제약

DefenseSoldierActionSelector의 평상시 utility는 GuardDutyDecisionContext로 실제 DutyPosition과 자리 가용성을 전달한다. Defense 전용 `Assets/Data/ScriptableObject/Defense/GuardActionCost.asset`의 Hunger/Thirst/Fatigue는 각 0.3/초이며 기존 interrupt threshold는 보존한다. 공유 GuardActionCost는 변경하지 않고 DefenseSetup도 별도 복사본을 생성한다.

Defense prefab의 worker/target/settings/presentation을 명시 연결한다. NPCManager와 role selector는 같은 Battlefield/ResponsePolicy를 참조한다. Enemy는 Enemy access navigation을 사용하고 기존 주민 생활 경로는 Friendly 기본 경로를 사용한다. 내부 bounds는 disabled BoxCollider의 local geometry를 읽는다. 건물 target anchor는 footprint 밖의 실제 이동 가능한 입구에 둔다. Unity global Find나 target 자동 검색으로 빠진 의존성을 보완하지 않는다.

궁수 자리는 비용 지불 전에 예약한다. 벽이 부서졌어도 빈 자리 예약은 가능하고 Downed는 자리를 반납하지 않는다. disable/취소 시에만 해제한다. 전투는 태업에 의해 거절되지 않는다. paused 시간에는 queue 전진과 공격/화살 이동이 정지한다.

실제 이동·공격 자세·Downed·도주 복귀는 사람 플레이 확인 항목이다. 기존 컴파일·정적 검사와 참조 검사는 실행 기록을 따르며 전용 테스트 도구를 추가하지 않는다. 정책·public API·상태 수명·prefab 연결이 변경되면 이 문서를 갱신한다.
