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
