# Targeting and Perception

## 기능 목적

물리 trigger 후보를 전투 target으로 변환하고, 가장 가까운 target을 선택하며, actor별 선택 target을 안전하게 유지하는 공통 전투 기반을 설명한다.

## 책임 경계

- `ProximitySensor2D`: layer mask에 맞는 collider 출입만 추적한다.
- `CombatPerception`: collider를 live `ICombatTarget` 후보로 변환하고 중복 collider를 합친다.
- `CombatTargeting`: 후보 중 nearest target을 계산하지만 runtime state를 변경하지 않는다.
- `CombatRuntimeState`: 선택 target 한 개와 sticky lifetime을 actor별로 소유한다.
- `CombatTargetHandle`: interface와 Unity owner를 묶어 생존성과 동적 위치를 제공한다.

## 현재 실행 흐름

```text
Trigger enter/exit
  -> ProximitySensor2D candidate list/event
  -> CombatPerception resolves ICombatTarget + Component owner
  -> selector calls CombatTargeting.TryFindNearestTarget
  -> selector decides whether to CombatRuntimeState.SetTarget
  -> CombatTargetHandle used as IMoveTarget
```

선택 target은 perception 범위를 벗어나도 살아 있는 동안 유지할 수 있다. 교체와 clear 정책은 Guard·Enemy selector가 각각 결정한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Interface/ICombatTarget.cs` | 공격 가능한 대상의 생존·위치·damage 계약 |
| `Assets/Scripts/System/Actor/CombatPerception.cs` | sensor collider를 중복 없는 live combat 후보로 변환 |
| `Assets/Scripts/System/Actor/CombatRuntimeState.cs` | actor별 선택 target과 reusable handle 상태 |
| `Assets/Scripts/System/Actor/CombatTargetHandle.cs` | target interface와 Unity owner의 생존성·동적 위치 adapter |
| `Assets/Scripts/System/Actor/ProximitySensor2D.cs` | layer-filtered Trigger2D collider 후보 감지 |
| `Assets/Scripts/System/Lib/CombatTargeting.cs` | 후보를 mutation하지 않는 2D nearest-target 검색 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| sensor mask·trigger | `ProximitySensor2D.cs`, prefab sensor child, physics layer |
| collider→target 변환 | `CombatPerception.cs`, `ICombatTarget.cs` |
| nearest target 규칙 | `CombatTargeting.cs`, 사용하는 role 문서 |
| sticky target·Unity null | `CombatRuntimeState.cs`, `CombatTargetHandle.cs`, role selector |
| target 추적 이동 | `CombatTargetHandle.cs`, [Movement](../NPC_Decision_and_Actions/Movement.md) |

## 불변 규칙

- sensor와 perception은 선택 target을 저장하지 않는다.
- `ICombatTarget`만으로 destroyed Unity object를 판단하지 않고 owner `Component`를 함께 보관한다.
- target의 여러 collider는 하나의 candidate로 de-duplicate한다.
- 공통 targeting helper는 caller의 runtime state를 직접 변경하지 않는다.
- buffer를 재사용해 replan hot path 할당을 피한다.

## Unity 배선과 검증 도구

NPC·Enemy prefab의 sensor child에는 trigger `CircleCollider2D`와 `ProximitySensor2D`가 필요하고 `CombatPerception`이 sensor를 참조한다. Guard는 Hostile, Enemy는 Friendly layer mask를 사용한다.

- `Assets/TestOnly/CombatTestDummy.cs`: Enemy가 감지할 임시 Friendly target.
- `Assets/Scenes/GuardTest.unity`: Guard·Enemy 양방향 감지와 target lifecycle 검증.

## 관련 문서

- [Attack Runtime](Attack_Runtime.md)
- [Guard](Guard.md)
- [Enemy](Enemy.md)
- [Movement](../NPC_Decision_and_Actions/Movement.md)

## 문서 갱신 조건

sensor, perception, target validity, selection helper 또는 sticky target ownership이 바뀌면 갱신한다.
