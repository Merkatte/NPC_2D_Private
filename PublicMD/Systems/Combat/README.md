# Combat

## 기능 목적

전투 감지, target 상태, 이동·공격 실행, Guard와 Enemy의 role별 판단을 연결하는 영역의 인덱스다. 이 문서는 스크립트 목록을 소유하지 않고 전투 작업을 필요한 leaf 문서로 라우팅한다.

## 세부 기능 라우팅

| 변경하려는 내용 | 읽을 문서 |
|---|---|
| sensor, 감지 후보, nearest target, sticky target | [Targeting and Perception](Targeting_and_Perception.md) |
| 공격 주기, damage, range, melee/ranged 거리 | [Attack Runtime](Attack_Runtime.md) |
| Guard 공급 판단, 순찰, 감지 interrupt, 능력치 | [Guard](Guard.md) |
| Enemy 추적·공격·Idle 판단과 능력치 | [Enemy](Enemy.md) |

## 전체 흐름

```text
CombatTarget(ICombatTarget) <- IHealthState
ProximitySensor2D -> CombatPerception -> target candidates
role selector -> CombatLib.TryFindNearestTarget -> CombatRuntimeState
  -> target이 멀면 MoveAction(dynamic CombatTargetHandle)
  -> AttackAction
  -> target 사망·소실 시 replan
```

Guard는 전투 대상이 없으면 공급 또는 경비를 판단한다. Enemy는 destination과 need를 사용하지 않고 감지된 전투 대상만 추적한다.

## 세부 기능 간 의존 관계

```text
Targeting and Perception -> 후보와 선택 target 상태
Attack Runtime -> 선택 target에 대한 공통 공격 실행
Guard / Enemy -> 언제 누구를 공격할지와 queue 구성
Movement -> 동적 target 추적
```

## 공유 불변 규칙

- perception은 후보만 관리하며 선택된 target을 소유하지 않는다.
- 선택된 target은 actor별 `CombatRuntimeState`가 소유한다.
- target interface의 Unity 생존성은 backing `Component`와 함께 검증한다.
- 범용 `CombatTarget`은 target 가능·무적 상태를 소유하고 실제 health mutation은 주입된 `IHealthState`에 위임한다.
- 거리와 nearest 계산은 2D 기준으로 일치시킨다.
- role selector가 target 교체·유지 정책을 결정하고 공통 targeting helper는 상태를 mutation하지 않는다.

## 외부 기능 연결

- 공통 action·queue: [NPC Decision and Actions](../NPC_Decision_and_Actions/README.md)
- runtime target reset: [NPC Runtime](../NPC_Runtime.md)
- actor prefab 생성: [Spawning and Pooling](../Spawning_and_Pooling.md)
- animator 없는 Enemy 표현: [NPC Presentation](../NPC_Presentation.md)

## 알려진 제약과 TBD

- NPC가 일반적으로 `ICombatTarget`이 되는 정책은 아직 확정되지 않았다.
- Enemy production spawn 경로와 전용 animation은 아직 없다.

## 문서 갱신 조건

전투 세부 기능 수, 공통 target 흐름, role 간 의존 방향이 바뀌면 이 인덱스를 갱신한다.
