# Code Evaluation Result

## Purpose

현재 NPC stat 구조에서 공용 stat, 직업별 성장 능력치, action tuning의 책임 경계를 검토한다.

이 문서는 이후 Claude Code 또는 Codex가 stat 분리 작업을 계획할 때 사용하는 handoff 문서다. 현재 요청은 설계 검토와 문서 정리이며 production C# 구현은 포함하지 않는다.

## Review Snapshot

- Date: 2026-08-20
- Scope:
  - `NPCStat`, `IStatView`, `ActionContext`
  - `GuardActionCost`, `AttackActionCost`, `FarmingActionCost`
  - `GuardAction`, `AttackAction`, `GuardActionSelector`
  - `NPCComponent`, `GuardRuntimeState`, `GuardPerception`, `ProximitySensor2D`
  - `DefaultStatContext`, `DataManager`, `NPCManager`
  - `NPCGirl.prefab`의 Guard 감지 collider
- Sources:
  - `PublicMD/CodeConvention.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/Game_Plan.md`
  - `PublicMD/PLAN.md`
- Verification:
  - 관련 C# 및 prefab YAML 정적 검사
  - `dotnet build Assembly-CSharp.csproj --no-restore`
  - Build result: 0 warnings, 0 errors
- Production code changes: 없음

## Executive Summary

공유 `ScriptableObject`에는 action 실행 비용과 판정 tuning을 두고, NPC마다 달라지거나 성장하는 능력치는 runtime stat에 두려는 방향은 타당하다.

현재 확인된 핵심 문제는 다음과 같다.

- `NPCStat`과 `IStatView`가 공격력과 공격 속도를 공용 stat으로 강제하므로 Farmer도 사용하지 않는 전투 필드를 가진다.
- `GuardActionCost`와 `AttackActionCost`에 공유 action tuning과 개체별 성장 후보 값이 섞여 있다.
- 감지 범위는 stat/data에 없고 `NPCGirl.prefab`의 `CircleCollider2D.radius`에만 저장되어 있다.
- `ActionContext.Stat`은 concrete `NPCStat`이므로 role capability를 명시적으로 전달할 경계가 없다.
- `DataManager.GetStat()`은 단일 `DefaultStatContext`만 사용하며 `NPCManager`는 enum index로 selector를 선택한다.

권장 방향은 `NPCStat`을 공용 base runtime state로 유지하고, `GuardStat : NPCStat`, `FarmStat : NPCStat` 같은 role subclass가 필요한 capability interface를 구현하는 것이다. 이 구조에서 Guard 한 명이 `NPCStat`, `CombatStat`, `GuardStat` 객체를 각각 가지는 것은 아니다. 실제 runtime 객체는 `GuardStat` 하나이며 여러 base/interface reference가 같은 객체를 가리킨다.

## Clarified Stat Model

### Interface inheritance does not create additional stat objects

다음 선언은 계약의 포함 관계를 뜻하며 runtime 객체 수를 늘리지 않는다.

```csharp
public interface IStatView
{
    float CurrentHealth { get; }
    float MoveSpeed { get; }
}

public interface ICombatStatView : IStatView
{
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackRange { get; }
}

public interface IGuardStatView : ICombatStatView
{
    float DetectionRange { get; }
}

public class NPCStat : IStatView
{
    // Common runtime state and mutation.
}

public class GuardStat : NPCStat, IGuardStatView
{
    // Guard/combat runtime state and mutation.
}
```

Guard 한 명을 생성하면 `GuardStat` 객체 하나만 존재한다.

```csharp
GuardStat guardStat = new GuardStat(/* ... */);

NPCStat npcStat = guardStat;
IStatView commonView = guardStat;
ICombatStatView combatView = guardStat;
IGuardStatView guardView = guardStat;
```

위 네 변수는 모두 같은 객체를 참조한다.

```text
GuardStat instance 1개
├─ NPCStat reference로 사용 가능
├─ IStatView reference로 사용 가능
├─ ICombatStatView reference로 사용 가능
└─ IGuardStatView reference로 사용 가능
```

### `CombatStat` class is optional

`ICombatStatView`를 만든다고 해서 반드시 `CombatStat : NPCStat` concrete class를 만들어야 하는 것은 아니다.

현재처럼 Guard만 전투 stat을 사용한다면 다음 구조가 가장 단순하다.

```text
NPCStat
├─ GuardStat : IGuardStatView
└─ FarmStat : IFarmStatView
```

여러 전투 직업이 생기고 공통 전투 mutation 구현까지 공유해야 할 때만 중간 concrete class를 고려한다.

```text
NPCStat
└─ CombatStat : ICombatStatView
   ├─ GuardStat : IGuardStatView
   └─ SoldierStat : ISoldierStatView
```

중간 class의 도입 기준은 공통 getter가 아니라 공통 state와 mutation 구현의 존재 여부다.

### Strict ISP alternative

`AttackAction`이 health, move speed, needs를 전혀 사용하지 않는다면 `ICombatStatView`가 `IStatView`를 상속하지 않도록 더 작게 분리할 수도 있다.

```csharp
public interface ICombatStatView
{
    float AttackPower { get; }
    float AttackSpeed { get; }
    float AttackRange { get; }
}

public class GuardStat : NPCStat, ICombatStatView, IGuardStatView
{
}
```

두 형태 모두 객체는 `GuardStat` 하나다. 최종 선택 기준은 소비자가 공용 stat과 전투 stat을 항상 함께 필요로 하는지 여부다.

## Data Ownership Decision

`Cost`와 `Stat`의 2분법보다 다음 세 소유권으로 분류한다.

| Data kind | Lifetime | Recommended owner | Examples |
|---|---|---|---|
| Action/decision tuning | 공유 immutable asset | `GuardActionTuning`, `AttackActionTuning` | 욕구 증가율, interrupt threshold, 도착 허용 거리, 순찰점 수 |
| Per-NPC runtime capability | NPC instance | `GuardStat`, `FarmStat`, combat capability | 공격력, 공격 속도, 탐지 범위, 숙련도 |
| Assignment/equipment definition | 배치 또는 장비 instance/definition | Guard post assignment, weapon definition | 순찰 반경, 무기 공격 범위 |

값별 권장 판단은 다음과 같다.

| Current value | Current owner | Recommended owner |
|---|---|---|
| `HungerPerSecond` 등 | `GuardActionCost` | 공유 Guard action tuning |
| need interrupt thresholds | `GuardActionCost` | 공유 Guard decision/action tuning |
| `PatrolArrivalDistance` | `GuardActionCost` | 공유 Guard action tuning |
| `PatrolPointCount` | `GuardActionCost` | 공유 Guard action tuning |
| `GetAttackPower` | base `NPCStat` | per-NPC combat/Guard stat |
| `GetAttackSpeed` | base `NPCStat` | per-NPC combat/Guard stat |
| detection range | prefab collider only | per-NPC Guard stat, Unity collider에 투영 |
| `AttackRange` | `AttackActionCost` | 성장 대상이면 per-NPC combat stat, 무기 영향이면 weapon definition |
| `GuardRadius` | `GuardActionCost` | 개인 성장치인지 GuardPost/assignment 반경인지 먼저 기획 결정 |

판정 기준까지 보관하는 asset은 `Cost`보다 `Tuning` 또는 `Policy`가 더 정확한 이름이다.

## Action Context Boundary

### Recommended cast location

Guard capability 검증은 `GuardActionSelector.RequestNewActionQueue(...)` 또는 NPC role composition 단계에 둔다.

```csharp
if (stat is not IGuardStatView guardStat)
{
    // Configuration error or safe Idle fallback.
}
```

검증된 `IGuardStatView`는 typed Guard action context/request에 전달한다. Guard/Attack action의 `Tick()`에서 반복 cast하지 않는다.

### Do not cache per-NPC stat in selector fields

`NPCManager`는 같은 selector scene instance를 동일 role의 여러 Worker에게 전달한다. 따라서 `GuardActionSelector` field에 특정 NPC의 `GuardStat`을 저장하면 NPC 간 state가 섞인다.

### Rejected locations

- `NPCComponent`: Unity `Transform`, `Animator`, renderer, collider 등 scene reference holder다. stat compatibility 판정 책임을 추가하지 않는다.
- 현재 `GuardRuntimeState`: 선택된 combat target을 보관하는 per-NPC transient state다. stat까지 넣지 않는다.
- shared selector field: 여러 NPC가 공유하므로 per-instance state를 저장하지 않는다.

현재 `IAction.Init(ActionContext)` 구조 때문에 typed context 분리가 과도한 변경이라면, 각 role action의 `Init()` 또는 `Start()`에서 capability를 한 번 검증하는 것은 허용 가능하다. 작은 runtime cast를 없애기 위해 공용 `ActionContext`에 role별 nullable property를 계속 추가하는 것은 피한다.

## Detection Range Synchronization

현재 감지 범위의 source of truth는 `Assets/Prefab/NPCGirl.prefab`의 `CircleCollider2D.m_Radius = 3`이다. stat 생성 또는 성장 시 collider를 갱신하는 경로가 없다.

권장 책임 흐름은 다음과 같다.

```text
GuardStat.DetectionRange
        ↓
GuardPerception.SetDetectionRange(float)
        ↓
CircleCollider2D.radius
```

규칙:

- `GuardStat`은 `Collider2D`, `MonoBehaviour`, prefab을 참조하지 않는다.
- `GuardPerception` 또는 별도 `GuardPerceptionRangeBinding`이 Unity collider 갱신을 담당한다.
- `ProximitySensor2D`는 Guard stat을 모르는 범용 sensor로 유지한다.
- 초기화와 level-up 적용 후 명시적으로 범위를 동기화한다.
- mutation 진입점이 많아지면 stat change event를 도입할 수 있다.
- event를 사용하면 pooled NPC의 `OnEnable`/`OnDisable` 또는 명시적 `Bind`/`Unbind`에서 구독 lifecycle을 정리한다.

## Progression Ownership

레벨업 정책 전체를 `GuardStat`에 넣지 않는다.

| Responsibility | Owner |
|---|---|
| 현재 level/experience/stat 값 | per-NPC role stat 또는 progression state |
| clamp와 invariant | `GuardStat` mutation method |
| 경험치 지급 조건 | progression system |
| 레벨업 판정과 성장 적용 순서 | progression system |
| 경험치 요구량, 성장량, 상한 | shared `ScriptableObject` 또는 CSV definition |
| collider/UI 반영 | Unity binding/presenter |

권장 흐름:

```text
Valid work/combat result
        ↓
GuardProgressionSystem
        ↓ reads
GuardProgressionDefinition
        ↓ calls
GuardStat.ApplyGrowth(...)
        ↓ notify or explicit refresh
GuardPerception/UI binding
```

`GuardStat`은 값과 불변식을 소유하고, progression system은 언제 왜 얼마나 성장하는지를 소유한다.

## NPC Creation Direction

현재 `NPCManager`는 `(int)npcType`으로 `_selectors` list를 인덱싱하고, `DataManager.GetStat()`은 npcType과 무관하게 단일 `DefaultStatContext`에서 `NPCStat`을 생성한다.

`GetStat(NPCType)`로 확장하는 것은 최소 변경으로 가능하지만 enum switch 또는 parallel list를 늘리는 방식은 권장하지 않는다.

권장 registration:

```text
NPCType
└─ NPC creation entry
   ├─ Worker prefab/pool
   ├─ BaseNPCActionSelector
   └─ NPCStatDefinition / runtime factory
```

각 entry는 `Awake()` 또는 `OnValidate()`에서 다음을 검증한다.

- duplicate `NPCType`
- missing selector
- missing stat definition/factory
- selector와 stat capability 불일치
- missing worker pool/prefab

가능한 stat definition 계약:

```csharp
public abstract class NPCStatDefinition : ScriptableObject
{
    public abstract NPCStat CreateRuntimeStat();
}
```

```text
DefaultNPCStatDefinition → NPCStat
GuardStatDefinition      → GuardStat
FarmStatDefinition       → FarmStat
```

직업 변경을 runtime에 지원할 경우 `NPCType`이 actor archetype과 current job을 동시에 의미하지 않도록 분리해야 한다. 상속 구조를 유지하면 직업 변경 시 기존 stat을 새 subtype으로 migration해야 한다. 직업별 숙련도를 계속 보존해야 한다면 공용 `NPCStat`과 role stat module의 합성이 장기적으로 더 적합하다.

## Improvements Since Previous Review

- stat interface 상속과 runtime 객체 수의 관계를 명확히 기록했다.
- `CombatStat` concrete class가 필수가 아니라는 기준을 추가했다.
- action tuning, per-NPC capability, assignment/equipment의 3개 데이터 소유권을 구분했다.
- Guard stat cast 위치와 shared selector에 per-NPC state를 저장하면 안 되는 이유를 명시했다.
- detection range의 stat-to-collider 동기화 책임을 구체화했다.
- 기존 파일에 섞여 있던 과거 Guard 전체 감사 snapshot은 이번 stat architecture 검토와 범위가 달라 제거했다.

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

#### M-01 — Shared action tuning and per-NPC capability are mixed

- Location:
  - `Assets/Data/ScriptableObject/Script/GuardActionCost.cs`
  - `Assets/Data/ScriptableObject/Script/AttackActionCost.cs`
- Evidence: shared asset의 `GuardRadius`와 `AttackRange`가 모든 NPC에 동일하게 적용되며 개체별 성장 차이를 표현할 수 없다.
- Recommendation: 값의 실제 소유자를 action tuning, per-NPC capability, assignment/equipment 중 하나로 결정하고 분리한다.

#### M-02 — Base stat contract forces combat data onto non-combat roles

- Location:
  - `Assets/Scripts/Interface/IStatView.cs`
  - `Assets/Scripts/System/Actor/NPCStat.cs`
  - `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs`
- Evidence: Farmer를 포함한 모든 `NPCStat`이 attack power와 attack speed를 저장하고 노출한다.
- Recommendation: 공용 stat과 combat/role capability interface를 분리한다.

#### M-03 — Detection radius has no runtime synchronization path

- Location:
  - `Assets/Prefab/NPCGirl.prefab:177`
  - `Assets/Scripts/System/Actor/GuardPerception.cs`
  - `Assets/Scripts/System/Actor/ProximitySensor2D.cs`
- Evidence: 감지 범위는 prefab의 `CircleCollider2D.m_Radius = 3`에만 존재한다.
- Recommendation: `GuardPerception.SetDetectionRange(float)` 또는 dedicated binding을 추가하고 initialization/progression에서 호출한다.

#### M-04 — NPC creation can pair a role selector with the wrong stat type

- Location:
  - `Assets/Scripts/Manager/NPCManager.cs`
  - `Assets/Scripts/Manager/DataManager.cs`
- Evidence: selector는 enum index로 선택하지만 stat은 단일 `GetStat()`으로 생성한다.
- Recommendation: `NPCType` keyed creation registration에서 selector와 stat definition을 함께 구성·검증한다.

### Low

#### L-01 — `Cost` naming no longer describes all stored data

- Location:
  - `Assets/Data/ScriptableObject/Script/DefaultActionCost.cs`
  - `Assets/Data/ScriptableObject/Script/GuardActionCost.cs`
- Evidence: cost뿐 아니라 threshold, arrival tolerance, patrol geometry가 포함된다.
- Recommendation: 분리 후 남는 책임에 따라 `ActionCost`, `ActionTuning`, `ActionPolicy` 이름을 선택한다.

## Findings By File

- `IStatView.cs`: 공용 contract에서 combat getter를 분리해야 한다.
- `NPCStat.cs`: 공용 runtime state만 남기는 방향이 적절하다.
- `DefaultStatContext.cs`: 역할별 runtime stat factory/definition 확장이 필요하다.
- `ActionContext.cs`: role capability의 명시적 전달 경계가 필요하지만 role별 nullable field 누적은 피해야 한다.
- `GuardActionCost.cs`: action tuning과 patrol/assignment 값을 재분류해야 한다.
- `AttackActionCost.cs`: `AttackRange`의 owner가 actor stat인지 weapon definition인지 결정해야 한다.
- `GuardActionSelector.cs`: role stat compatibility를 검증하기 좋은 경계지만 per-NPC stat field cache는 금지한다.
- `AttackAction.cs`: 최종적으로 `ICombatStatView`에 의존하는 것이 적절하다.
- `GuardPerception.cs`: detection range를 Unity collider에 투영할 책임 후보이다.
- `GuardRuntimeState.cs`: combat target state만 유지한다.
- `NPCManager.cs` / `DataManager.cs`: type-safe creation registration이 필요하다.

## Cross-Cutting Findings

- interface inheritance는 계약의 포함 관계이며 객체 조합이나 객체 수를 자동으로 결정하지 않는다.
- class inheritance를 선택하면 Guard 한 명당 `GuardStat` 객체 하나만 생성할 수 있다.
- 직업 변경과 직업별 성장 상태 보존은 stat inheritance 선택에 영향을 주는 별도 기획 결정이다.
- shared `ScriptableObject`는 base value, curve, tuning definition으로 사용하고 runtime current value는 저장하지 않는다.
- plain C# stat은 Unity collider/UI를 직접 참조하지 않고 binding 계층을 통해 반영한다.

## Positive Notes

- `NPCStat`은 현재 plain C# runtime state로 Unity scene object를 직접 참조하지 않는다.
- `WorkerNPC`가 `NPCStat` base reference를 보관하므로 `GuardStat` subtype 한 개를 그대로 받을 수 있다.
- `GuardPerception`과 `ProximitySensor2D`의 Guard adapter / 범용 sensor 분리는 유지할 가치가 있다.
- `GuardRuntimeState`는 이미 per-NPC target state로 분리되어 있다.
- shared ScriptableObject에서 runtime stat을 새 객체로 생성하는 원칙 자체는 올바르다.

## Recommended Next Actions

1. 직업이 생성 후 고정인지, runtime 변경 가능한지 먼저 확정한다.
2. `AttackRange`와 `GuardRadius`가 actor 성장치인지 equipment/assignment 값인지 결정한다.
3. `IStatView`, `ICombatStatView`, `IGuardStatView`, `IFarmStatView`의 정확한 getter 목록을 승인한다.
4. 현재 prototype에서는 `GuardStat : NPCStat, IGuardStatView`와 `FarmStat : NPCStat, IFarmStatView`를 우선 검토한다.
5. 공통 전투 mutation 구현이 실제로 두 개 이상의 class에서 필요할 때만 `CombatStat : NPCStat`을 추가한다.
6. selector에서 role capability를 검증하고 action에 전달할 typed context/request 설계를 결정한다.
7. `GuardPerception` 또는 binding을 통한 detection range 동기화 경로를 설계한다.
8. `NPCType` keyed creation registration과 stat definition/factory 구조를 설계한다.
9. 승인된 설계로 별도 implementation plan을 작성한 뒤 production code를 변경한다.

## Final Verdict

**Direction approved with design decisions pending.**

공유 action tuning과 per-NPC 성장 stat의 분리는 승인 가능하다. `GuardStat : NPCStat` 방식도 Guard당 stat 객체 하나만 생성하는 올바른 선택이다. 다만 직업 변경 정책, `AttackRange`/`GuardRadius`의 실제 소유자, interface 상속 범위, typed action context 형태를 확정하기 전에는 구현을 시작하지 않는다.
