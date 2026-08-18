# Project Structure

## Purpose

This document records the current Unity NPC prototype structure as of 2026-08-01. The previous `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph, animation, combat, recruitment, and UI structures are no longer present in `Assets/Scripts`.

The current codebase is a fresh early skeleton. It is not yet a complete runtime architecture, but the current C# project builds successfully.

## Current File Structure

```text
Assets/
  Scripts/
    Actor/
      WorkerNPC.cs

    Enum/
      ActionType.cs
      NPCType.cs

    Interface/
      IAction.cs

    Manager/
      NPCManager.cs

    System/
      Action/
        EatAction.cs
        FarmingAction.cs
        MoveAction.cs
        SleepAction.cs

      Actor/
        BaseNPCActionSelector.cs
        FarmerActionSelector.cs
        NPCComponent.cs
        NPCStat.cs

      Lib/
        ActionPool.cs
        DestinationDB.cs
```

`Assets/BehaviorGraph` is not present in the current project tree.

## Current Architecture Direction

The new structure is moving toward a thinner Unity component layer with plain C# objects for runtime state and action logic.

```text
NPCManager
  -> owns or creates WorkerNPC instances by NPCType

WorkerNPC : MonoBehaviour
  -> represents the NPC actor instance
  -> owns narrow references to NPCStat and NPCComponent
  -> owns an Action tick delegate
  -> invokes registered callbacks from Update()

NPCComponent : MonoBehaviour
  -> intended place for Unity-facing NPC references such as Transform, Animator, Renderer, Collider, etc.
  -> currently empty

NPCStat
  -> plain C# state holder for name, health, max health, and move speed
  -> exposes read-only stat properties and mutation methods

BaseNPCActionSelector : MonoBehaviour
  -> base selector boundary
  -> owns or uses ActionPool

FarmerActionSelector
  -> role-specific selector skeleton
  -> currently empty

ActionPool
  -> maps ActionType to pooled IAction queues
  -> creates MoveAction, EatAction, FarmingAction, SleepAction

IAction implementations
  -> action lifecycle methods: Start, Tick, Pause, Resume, Stop, Clear
  -> currently mostly NotImplementedException stubs

DestinationDB
  -> intended Unity-side destination registry
  -> currently skeletal
```

## Script Roles

### Design Intent

The current direction intentionally avoids a single large NPC MonoBehaviour that exposes every NPC-related detail to every caller.

`WorkerNPC` should be the narrow actor root: code that only needs ticking should depend on the tick registration API, code that needs stats should use `NPCStat`, and code that needs Unity scene references should go through `NPCComponent` or a narrower context. This keeps callers from learning about unrelated NPC internals just because they need one capability.

Preferred split:

```text
WorkerNPC
  -> actor identity, lifecycle bridge, tick dispatch

NPCComponent
  -> Unity object/component references

NPCStat
  -> mutable stat values and stat invariants

Action runner or IAction
  -> behavior execution lifecycle

Selector
  -> behavior choice
```

### WorkerNPC

File: `Assets/Scripts/Actor/WorkerNPC.cs`

Current role:

- Represents the scene NPC MonoBehaviour.
- Stores a plain `NPCStat` reference.
- Stores an `NPCComponent` reference for Unity-facing component access.
- Provides `RegisterTick(Action)` and `UnReigsterTick(Action)` to add/remove tick callbacks.
- Calls `_onTick` during `Update()`.

Current issues to resolve before further architecture work:

- `UnReigsterTick` is misspelled and should become `UnregisterTick`.
- `OnClear()` is not connected to Unity lifecycle yet. It should be called from `OnDestroy()` or be renamed into `OnDestroy()` if its only role is cleanup.
- This class should remain a thin runner/Unity bridge. It should not grow decision logic or concrete action behavior.

### NPCComponent

File: `Assets/Scripts/System/Actor/NPCComponent.cs`

Current role:

- Placeholder MonoBehaviour for Unity-facing NPC components and references.

Intended role:

- Own serialized Unity references such as `Transform`, `Animator`, sprite/visual root, collider, and other scene components.
- Provide those references to plain C# runtime objects through explicit initialization.

Should not:

- Become a large behavior implementation class.
- Own detailed action rules, stat mutation rules, or role decision priority.

### NPCStat

File: `Assets/Scripts/System/Actor/NPCStat.cs`

Current role:

- Plain C# state object for NPC name, health, max health, and move speed.
- `ChangeHealth(float)` clamps health between zero and max health.
- `ChangeMoveSpeed(float)` is intended to adjust movement speed.

Current issues:

- Constructor accepts `moveSpeed` but does not assign `_moveSpeed`.
- Public read-only properties use `Get...` names. Prefer `CurrentHealth`, `MaxHealth`, and `MoveSpeed`.
- `ChangeMoveSpeed` clamps using the current `_moveSpeed` as its max, so positive increases cannot work. A separate max speed or stat modifier model is needed.
- `using System.Runtime.InteropServices;` is unused.

### IAction

File: `Assets/Scripts/Interface/IAction.cs`

Current role:

- Defines the action lifecycle:

```csharp
void Start();
void Tick();
void Pause();
void Resume();
void Stop();
void Clear();
```

Design note:

- This contract currently cannot report whether an action is running, succeeded, or failed.
- If `WorkerNPC` or an action runner is responsible for advancing a queue, `Tick()` should eventually return an action state or the action should expose a clear completion result.

Recommended direction:

```csharp
ActionState Tick(NPCActionContext context);
```

or an equivalent explicit result contract.

### Action Implementations

Files:

- `Assets/Scripts/System/Action/MoveAction.cs`
- `Assets/Scripts/System/Action/EatAction.cs`
- `Assets/Scripts/System/Action/FarmingAction.cs`
- `Assets/Scripts/System/Action/SleepAction.cs`

Current role:

- Concrete `IAction` skeletons.

Current status:

- Most methods throw `System.NotImplementedException`.
- `MoveAction.Clear()` is the only non-throwing cleanup stub.

Should own:

- The execution details and completion/failure/cancel rules of one behavior.

Should not own:

- Worker action queue advancement.
- High-level behavior selection.
- Direct registration/unregistration into `WorkerNPC` unless the architecture deliberately moves tick ownership into actions.

### ActionPool

File: `Assets/Scripts/System/Lib/ActionPool.cs`

Current role:

- Maintains a `Dictionary<ActionType, Queue<IAction>>`.
- Creates actions on demand and returns them to their typed queues.

Current issues:

- `using NUnit.Framework;` is present in production code and should be removed.
- `_actionDictionary` should be private.
- `ReturnAction` repeats identical enqueue logic in every switch case.
- No default case handles unsupported `ActionType`.
- Returning an action requires the caller to pass the correct `ActionType`; a mismatch can put an action into the wrong pool.

### BaseNPCActionSelector / FarmerActionSelector

Files:

- `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs`
- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`

Current role:

- Selector boundary skeleton.
- `BaseNPCActionSelector` stores a protected `ActionPool`.
- `FarmerActionSelector` currently adds no behavior.

Recommended direction:

- Selectors should decide intent and request actions from `ActionPool`.
- Selectors should not execute actions directly.
- Selectors should not mutate `NPCStat` directly except through action planning decisions.

### DestinationDB / DestinationInfo

File: `Assets/Scripts/System/Lib/DestinationDB.cs`

Current role:

- Placeholder destination registry.
- Contains a serialized `List<DestinationDB>` named `Destinations`.
- Defines `DestinationInfo` with name, transform, and object references.

Current issues:

- The serialized list likely should be `List<DestinationInfo>`, not `List<DestinationDB>`.
- `DestinationInfo` is not marked `[System.Serializable]`, so Unity will not serialize it as an Inspector list element.
- Field names are public PascalCase, but serialized private fields are preferred for new runtime data.

### NPCManager

File: `Assets/Scripts/Manager/NPCManager.cs`

Current role:

- Placeholder manager with `Dictionary<NPCType, List<WorkerNPC>>`.
- Initializes the dictionary in `Awake()`.
- Contains an empty `CreateNPC()`.

Recommended direction:

- Keep this class as a composition/spawn/registry manager.
- Do not put action selection, stat rules, or per-action execution details here.

## Dependency Direction

Preferred direction for the new skeleton:

```text
NPCManager
  -> WorkerNPC

WorkerNPC
  -> NPCStat
  -> NPCComponent
  -> current action runner or tick delegate

Callers
  -> depend on the narrow object they actually need
  -> avoid taking WorkerNPC when NPCStat, NPCComponent, or a focused context is enough

Selector
  -> ActionPool
  -> IAction

IAction
  -> context/capabilities needed for execution

NPCStat
  -> no dependency on selectors, actions, managers, or Unity scene objects
```

## Recommended Runtime Flow

The current `RegisterTick(Action)` model can work for a low-level tick callback, but action queue progression needs a single lifecycle owner.

Recommended action queue flow:

```text
WorkerNPC or NPCActionRunner
  if no current action:
    ask selector for the next action or action queue
    call currentAction.Start(context)

  each tick:
    state = currentAction.Tick(context)

  if state == Running:
    keep current action

  if state == Success:
    clear/return current action
    start next queued action

  if state == Failed:
    cancel/clear current action plan
    ask selector again later
```

Action implementations should not need to register themselves into `WorkerNPC` just to be ticked. A central runner is easier to cancel, clear, and debug.

## Extension Guide

### Adding A New Action

1. Add an `ActionType` value if the action must be selected or pooled by type.
2. Create the new `IAction` implementation under `Assets/Scripts/System/Action`.
3. Add creation support to `ActionPool`.
4. Ensure `Clear()` resets all reusable runtime state before returning to the pool.
5. Add a completion result path before using the action in a queue.

### Adding NPC Runtime State

1. Put mutable plain state in a plain C# class such as `NPCStat`.
2. Expose read-only properties for observation.
3. Expose explicit mutation methods such as `ChangeHealth`, `SetMoveSpeed`, or `ApplyDelta`.
4. Keep Unity object references out of pure stat objects unless there is a clear ownership reason.

### Adding Unity Component References

1. Add serialized private fields to `NPCComponent` or the owning MonoBehaviour.
2. Validate required references during `Awake()` or explicit `Init`.
3. Pass references into runtime context objects explicitly.
4. Avoid hidden `FindObjectOfType`, scene searches, and repeated `GetComponent` inside per-frame action ticks.

## Known Design Notes

- The project is currently in a reset/skeleton phase.
- The old worker architecture documentation has been removed from this file because the corresponding files are no longer present.
- `Assembly-CSharp.csproj` currently references the new script set.
- `dotnet build Assembly-CSharp.csproj --no-restore` passed on 2026-08-01 with 0 warnings and 0 errors.
- This document's sections above still describe the 2026-08-01 skeleton and predate `DestinationDecider`, `NPCDecisionTuning`, `DataManager`, and the Guard combat slice below. They have not been rewritten as part of this change; see `PublicMD/PROGRESS.md` for the intervening history (IMP-022 onward).

## Guard Combat Vertical Slice (2026-08-19)

`PublicMD/Guard_Action_Implementation_Plan.md`의 Phase A~D 구현 결과. `WorkerNPC → BaseNPCActionSelector.RequestNewActionQueue(...) → Queue<IAction>` 구조는 그대로 유지하고, action 결과 계약과 Guard 전용 실행/감지/전투 인프라를 추가했다.

### Action 결과 계약 (Phase A)

- `Assets/Scripts/Enum/ActionResult.cs` — `Running` / `Completed` / `ReplanRequested` / `Failed`.
- `IAction.CheckComplete()`가 `ActionResult Result { get; }`로 대체됐다.
- `DefaultAction`은 `Init()`에서 더 이상 `Start()`를 호출하지 않는다. `Complete()` / `RequestReplan()` / `Fail(reason)` 세 개의 protected 종료 경로를 갖는다.
- `WorkerNPC`가 queue lifecycle을 단독 소유한다: 현재 action을 dequeue 시점에만 `Start()`하고, `ReplanRequested`/`Failed` 시 현재 action과 남은 queue 전체를 `Stop()`+반환한 뒤 `RequestNewActionQueue`를 다시 호출한다. `NPCType`을 저장하며 더 이상 `NPCType.Farmer`를 하드코딩하지 않는다. 풀 재사용을 고려해 `_isInitialized` 가드와 `NPCComponent.ResetRuntimeState()` 호출을 `Init()`/`OnDisable()` 양쪽에 둔다.

### Guard 순찰과 욕구 (Phase B)

- `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` — 경계 반경/순찰 도착 거리/순찰점 개수, 초당 욕구 증가량, Guard 전용 중단 임계치(0..1)를 데이터로 소유한다.
- `Assets/Scripts/System/Action/GuardAction.cs` — 경계 중심을 순회하며 장기 실행되는 순찰 action. 결정적 순찰점 순환(반경 위 N개 점, `UnityEngine.Random` 미사용)을 쓰고, 매 Tick 욕구를 직접 증가시키며(`StatEffect` 신규 할당 없음), 정상 순찰 중에는 `Complete()`를 반환하지 않는다.
- `Assets/Scripts/Enum/BuildingType.cs`에 `GuardPost`를 끝에 추가(기존 직렬화 값 보존).
- `Assets/Scripts/System/Actor/GuardActionSelector.cs`가 Farmer 복사본에서 전면 재작성됐다. 우선순위는 전투 → 욕구 → 순찰이며(전투 분기는 Phase D에서 연결), 큐 조립은 대여 실패 시 전체 롤백하는 트랜잭션 방식이다.

### 감지·타겟 인프라 (Phase C)

- `Assets/Scripts/Interface/ICombatTarget.cs`, `Assets/Scripts/Interface/IMoveTarget.cs` — 최소 전투/이동 대상 계약.
- `Assets/Scripts/System/Actor/CombatTargetHandle.cs` — `ICombatTarget` + `Component Owner` 쌍으로 유효성을 판정하는 plain C# handle(`IMoveTarget` 구현). Unity 파괴 객체 null 판정 문제를 `Owner` truthiness로 해결한다.
- `Assets/Scripts/System/Actor/ProximitySensor2D.cs` — LayerMask 기반 범용 Trigger2D 감지기. Guard/Enemy/ICombatTarget을 전혀 참조하지 않는다.
- `Assets/Scripts/System/Actor/GuardPerception.cs` — sensor collider를 살아 있는 `ICombatTarget` 후보로 변환·중복 제거(`Dictionary<Collider2D, TargetEntry>` 역방향 매핑)하는 Guard 전용 adapter. 타겟을 직접 선택·저장하지 않는다.
- `Assets/Scripts/System/Actor/GuardRuntimeState.cs` — NPC별 `CombatTargetHandle` 하나를 소유. perception이 후보를 잃어도 selector가 명시적으로 지우기 전까지 타겟을 유지한다(사거리 이탈 시 타겟 유지 요구사항).
- `Assets/Data/Struct/MoveRequest.cs` — 고정 좌표 또는 `IMoveTarget` 기반 동적 목적지. `MoveAction`은 이를 통해서만 동적 대상을 알고, 전투 타입을 직접 참조하지 않는다.
- `Assets/Scripts/Actor/Enemy.cs` — 검증용 최소 `ICombatTarget` 구현(이동/반격 없음).
- `NPCComponent`가 `GuardPerception` 참조와 per-NPC `GuardRuntimeState` 인스턴스를 소유·노출한다. Farmer 계열 프리팹에서는 `GuardPerception`이 비어 있을 수 있으므로 모든 소비 지점이 null을 허용한다.

### 전투 (Phase D)

- `Assets/Data/ScriptableObject/Script/AttackActionCost.cs` — 공격 반경.
- `Assets/Scripts/System/Action/AttackAction.cs` — 하나의 action이 타겟이 죽거나 범위를 벗어날 때까지 반복 타격한다. `NPCStat.GetAttackSpeed`로 interval을 계산하고(첫 타격은 한 interval 후), 타격 직후 생존을 재검사해 이미 죽은 대상에 중복 피해를 주지 않는다. 한 Tick당 최대 타격 횟수 상한(`MaxHitsPerTick`)으로 긴 프레임에서의 공격 폭주를 막는다. `Clear()`는 timer만 초기화하고 타겟은 지우지 않는다(사거리 이탈 재계획에서 타겟 유지).
- `NPCStat`/`IStatView`/`DefaultStatContext`에 공격력(`GetAttackPower`)과 공격 속도(`GetAttackSpeed`, attacks/sec)가 추가됐다(생성자 끝에 append, 기존 호출부 보존).
- `GuardAction`이 매 Tick `GuardPerception.HasCandidate`만 확인해 후보가 있으면 재계획을 요청한다(적 감지가 욕구 임계보다 우선).
- `GuardActionSelector`가 재계획 시 `GuardRuntimeState`에 유효 타겟이 있으면 우선 사용하고, 없으면 `GuardPerception`의 후보 중 가장 가까운 살아있는 적을 선택해 저장한다. 공격 범위 밖이면 `Move(dynamic MoveRequest) → Attack`, 안이면 `Attack`만 큐에 넣는다.

### 씬 배선 (미완료)

C# 코드와 ScriptableObject 클래스 정의만 구현했다. `SampleScene.unity`/`NPCGirl.prefab`의 GameObject·collider·Layer·asset 인스턴스 연결은 사용자가 Unity 에디터에서 직접 수행해야 한다. 남은 배선 목록은 `PublicMD/PROGRESS.md`를 참고한다.
