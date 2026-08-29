# Enemy

## 기능 목적

Enemy의 target adapter, instinct 없는 전투 판단, role stat과 definition을 설명한다.

## 현재 실행 흐름

```text
Enemy spawner
  -> EnemyStatDefinition.CreateRuntimeStat()
  -> 같은 EnemyStat을 WorkerNPC와 Enemy.Init에 주입

EnemyActionSelector
  -> CombatPerception 후보 없음: Idle
  -> target 선택
  -> preferred range 밖: Move
  -> Attack
```

Enemy는 hunger·thirst·fatigue나 `DestinationDecider`, `DestinationDB`를 사용하지 않는다. `Enemy` component는 health를 따로 저장하지 않고 주입받은 `EnemyStat`을 `ICombatTarget`으로 노출한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/EnemyStatDefinition.cs` | melee/ranged `EnemyStat` runtime instance 생성 definition |
| `Assets/Scripts/Actor/Enemy.cs` | `EnemyStat`을 health source로 사용하는 `ICombatTarget` adapter |
| `Assets/Scripts/Interface/IEnemyStatView.cs` | combat stat에 style과 preferred range ratio를 추가한 read model |
| `Assets/Scripts/System/Actor/EnemyActionSelector.cs` | 감지 target에만 반응해 Idle·Move·Attack queue를 구성 |
| `Assets/Scripts/System/Actor/EnemyStat.cs` | Enemy combat 능력과 attack style runtime stat |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| target 없을 때 행동 | `EnemyActionSelector.cs` |
| target 선택·추적 | `EnemyActionSelector.cs`, [Targeting and Perception](Targeting_and_Perception.md) |
| melee/ranged 거리 | `EnemyActionSelector.cs`, `EnemyStat.cs`, [Attack Runtime](Attack_Runtime.md) |
| health·damage source | `Enemy.cs`, `EnemyStat.cs`, `ICombatTarget.cs` |
| Enemy 생성·prefab | `EnemyStatDefinition.cs`, [Spawning and Pooling](../Spawning_and_Pooling.md), Enemy prefab |

## 불변 규칙

- `Enemy`와 `WorkerNPC`는 같은 `EnemyStat` instance를 사용한다.
- Enemy component는 별도 health field를 갖지 않는다.
- Enemy selector는 need, destination, facility interaction을 참조하지 않는다.
- prefab은 scene의 공유 action pool을 직접 참조하지 않고 selector는 scene object로 제공한다.
- target이 없으면 강제 랜덤 이동하지 않고 Idle한다.

## Unity 배선과 검증 도구

- `Assets/Prefab/InGame/Enemy.prefab`: `WorkerNPC`, animator-optional `NPCComponent`, `Enemy`, Friendly mask perception sensor.
- `Assets/TestOnly/TestEnemyRainSpawner.cs`: melee/ranged definition을 round-robin해 Enemy를 생성한다.
- `Assets/TestOnly/CombatTestDummy.cs`: Enemy의 임시 Friendly target.
- `Assets/Scenes/GuardTest.unity`: Guard와 Enemy 상호 전투 검증.

## 알려진 제약과 TBD

- production Enemy spawn·despawn 경로가 없다.
- Enemy 전용 Animator와 애니메이션이 없다.
- NPC를 일반 combat target으로 노출하는 정책은 미결정이다.

## 관련 문서

- [Targeting and Perception](Targeting_and_Perception.md)
- [Attack Runtime](Attack_Runtime.md)
- [Spawning and Pooling](../Spawning_and_Pooling.md)
- [NPC Presentation](../NPC_Presentation.md)

## 문서 갱신 조건

Enemy 판단, target adapter, stat definition, attack style 또는 prefab 배선이 바뀌면 갱신한다.
