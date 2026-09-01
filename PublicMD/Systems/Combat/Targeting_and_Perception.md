# Targeting and Perception

## 기능 목적

물리 trigger 후보를 전투 target으로 변환하고, 가장 가까운 target을 선택하며, actor별 선택 target을 안전하게 유지하는 공통 전투 기반을 설명한다.

## 책임 경계

- `ProximitySensor2D`: layer mask에 맞는 collider 출입만 추적한다.
- `CombatTarget`: 타겟 가능·무적 상태를 판정하고 실제 health owner에 damage를 전달한다.
- `CombatPerception`: collider를 live `ICombatTarget` 후보로 변환하고 중복 collider를 합친다.
- `CombatTargeting`: 후보 중 nearest target을 계산하지만 runtime state를 변경하지 않는다.
- `CombatRuntimeState`: 선택 target 한 개와 sticky lifetime을 actor별로 소유한다.
- `CombatTargetHandle`: interface와 Unity owner를 묶어 생존성과 동적 위치를 제공한다.

## 현재 실행 흐름

```text
Trigger enter/exit
  -> ProximitySensor2D candidate list/event
  -> CombatPerception resolves CombatTarget(ICombatTarget) + Component owner
  -> selector calls CombatTargeting.TryFindNearestTarget
  -> selector decides whether to CombatRuntimeState.SetTarget
  -> CombatTargetHandle used as IMoveTarget
```

선택 target은 perception 범위를 벗어나도 살아 있는 동안 유지할 수 있다. 교체와 clear 정책은 Guard·Enemy selector가 각각 결정한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Actor/CombatTarget.cs` | health owner를 중복하지 않는 범용 target·무적·damage adapter |
| `Assets/Scripts/Interface/ICombatTarget.cs` | 공격 가능한 대상의 생존·target 가능 여부·위치·damage 계약 |
| `Assets/Scripts/Interface/IHealthState.cs` | CombatTarget에 주입되는 현재 health와 변경 capability |
| `Assets/Scripts/System/Actor/CombatPerception.cs` | sensor collider를 중복 없는 live combat 후보로 변환 |
| `Assets/Scripts/System/Actor/CombatRuntimeState.cs` | actor별 선택 target과 reusable handle 상태 |
| `Assets/Scripts/System/Actor/CombatTargetHandle.cs` | target interface와 Unity owner의 생존성·동적 위치 adapter |
| `Assets/Scripts/System/Actor/ProximitySensor2D.cs` | layer-filtered Trigger2D collider 후보 감지 |
| `Assets/Scripts/System/Lib/CombatTargeting.cs` | 후보를 mutation하지 않는 2D nearest-target 검색 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| sensor mask·trigger | `ProximitySensor2D.cs`, prefab sensor child, physics layer |
| target 가능·무적·damage 전달 | `CombatTarget.cs`, `ICombatTarget.cs`, `IHealthState.cs` |
| collider→target 변환 | `CombatPerception.cs`, `ICombatTarget.cs` |
| nearest target 규칙 | `CombatTargeting.cs`, 사용하는 role 문서 |
| sticky target·Unity null | `CombatRuntimeState.cs`, `CombatTargetHandle.cs`, role selector |
| target 추적 이동 | `CombatTargetHandle.cs`, [Movement](../NPC_Decision_and_Actions/Movement.md) |

## 불변 규칙

- sensor와 perception은 선택 target을 저장하지 않는다.
- `CombatTarget`은 health를 중복 저장하지 않고 명시적으로 주입된 `IHealthState`만 변경한다.
- 무적 target은 선택 가능한 상태를 유지한 채 damage만 거부하고, target 불가능 상태는 handle validity에서 제외한다.
- 비활성화된 `CombatTarget`은 health가 남아 있어도 target 불가능하다.
- `CombatTarget`은 죽은 GameObject를 직접 파괴하지 않고 `Died` event로 대상별 lifecycle owner에 알린다.
- `ICombatTarget`만으로 destroyed Unity object를 판단하지 않고 owner `Component`를 함께 보관한다.
- target의 여러 collider는 하나의 candidate로 de-duplicate한다.
- 공통 targeting helper는 caller의 runtime state를 직접 변경하지 않는다.
- buffer를 재사용해 replan hot path 할당을 피한다.

## Unity 배선과 검증 도구

NPC·Enemy prefab의 sensor child에는 trigger `CircleCollider2D`와 `ProximitySensor2D`가 필요하고 `CombatPerception`이 sensor를 참조한다. 피격 가능한 scene object에는 초기화된 `CombatTarget`이 필요하다. Guard는 Hostile, Enemy는 Friendly layer mask를 사용한다.

- `Assets/TestOnly/CombatTestDummy.cs`: Enemy가 감지할 임시 Friendly target.
- `Assets/Scenes/GuardTest.unity`: Guard·Enemy 양방향 감지와 target lifecycle 검증.

## 관련 문서

- [Attack Runtime](Attack_Runtime.md)
- [Guard](Guard.md)
- [Enemy](Enemy.md)
- [Movement](../NPC_Decision_and_Actions/Movement.md)

## 문서 갱신 조건

`CombatTarget`, health adapter, sensor, perception, target validity, selection helper 또는 sticky target ownership이 바뀌면 갱신한다.
