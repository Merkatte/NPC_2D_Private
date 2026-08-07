# Code Evaluation Result

## Purpose

Lead-programmer review of the current NPC action structure after introducing `ActionContext` and `DefaultAction`, using `PublicMD/ProjectStructure.md`, `PublicMD/CodeConvention.md`, and `.codex/skills/reviewing-npc-work-code/references/review-workflow.md` as standards. Production code was not modified.

## Review Snapshot

- Date: 2026-08-03
- Scope:
  - All current `.cs` files under `Assets/Scripts`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - Current action lifecycle files: `IAction`, `ActionContext`, `DefaultAction`, `MoveAction`, `ActionPool`, `FarmerActionSelector`, `WorkerNPC`
- Sources:
  - `rg --files Assets/Scripts`
  - Targeted `rg -n` and `Select-String` checks for action inheritance, pool creation, queue ownership, completion handling, stubs, and stale APIs
  - Direct source reads of relevant C# files
- Verification:
  - `Assets/BehaviorGraph/CustomActionNode` is absent.
  - `dotnet build Assembly-CSharp.csproj --no-restore` was run.
  - Build failed with 6 compile errors because `EatAction`, `DrinkAction`, and `SleepAction` no longer implement `IAction` while `ActionPool` still enqueues them into `Queue<IAction>`.

## Executive Summary

The `DefaultAction : IAction` direction is a strong improvement for this codebase. It removes duplicated lifecycle state from concrete actions and keeps `IAction` focused on the external action contract. Keeping `Complete()` as a protected method on `DefaultAction`, not on `IAction`, is also the right boundary.

The current implementation is not yet in a usable state because the action hierarchy is only partially migrated. `MoveAction` now follows the intended pattern, but `EatAction`, `DrinkAction`, and `SleepAction` are plain empty classes, `FarmingAction` still directly implements `IAction` with throwing stubs, and `ActionPool` expects all action classes to be assignable to `IAction`.

There is also a confirmed queue construction bug in `FarmerActionSelector`: it initializes one action instance, then enqueues a different uninitialized instance.

## Improvements Since Previous Review

- `ActionContext` now gives actions a single initialization parameter instead of requiring type-specific public init signatures everywhere.
- `DefaultAction` now owns common action state and lifecycle methods: component reference, pause/running/complete flags, `Init`, `Start`, `Pause`, `Resume`, `Stop`, `Clear`, `CheckComplete`, and protected `Complete`.
- `MoveAction` no longer duplicates the common lifecycle fields and correctly uses `override` for `Init`, `Start`, `Tick`, and `Clear`.
- `MoveAction` keeps movement-specific state, such as `_destination` and `_stoppingDistance`, in the concrete class instead of forcing all actions to carry destination data.
- `DestinationDB` has improved from the earlier malformed `List<DestinationDB>` shape to a serialized `List<DestinationInfo>`.
- `NPCStat` now assigns `_moveSpeed` in the constructor.

## Findings By Severity

### Critical

#### Finding 1: Project currently does not compile after partial action migration

- Severity: Critical
- Files:
  - `Assets/Scripts/System/Lib/ActionPool.cs:26`
  - `Assets/Scripts/System/Lib/ActionPool.cs:29`
  - `Assets/Scripts/System/Lib/ActionPool.cs:35`
  - `Assets/Scripts/System/Lib/ActionPool.cs:72`
  - `Assets/Scripts/System/Lib/ActionPool.cs:75`
  - `Assets/Scripts/System/Lib/ActionPool.cs:81`
  - `Assets/Scripts/System/Action/EatAction.cs:3`
  - `Assets/Scripts/System/Action/DrinkAction.cs:3`
  - `Assets/Scripts/System/Action/SleepAction.cs:3`
- Evidence:
  - `ActionPool` enqueues `new EatAction()`, `new DrinkAction()`, and `new SleepAction()` into `Queue<IAction>`.
  - Those classes are currently plain classes and do not implement `IAction` or inherit `DefaultAction`.
  - `dotnet build Assembly-CSharp.csproj --no-restore` fails with CS1503 conversion errors for those three action types.
- Recommendation:
  - Make every pooled action inherit `DefaultAction`, even if `Tick()` is temporarily a no-op or immediate `Complete()`.

### High

#### Finding 2: `FarmerActionSelector` initializes one action but enqueues another

- Severity: High
- File: `Assets/Scripts/System/Actor/FarmerActionSelector.cs:44`
- Evidence:
  - `IAction action = GetAction(actionType);`
  - `action.Init(new ActionContext(component, stat, step.DestinationPos));`
  - `queue.Enqueue(GetAction(actionType));`
- Why it matters:
  - The initialized action is discarded.
  - The queued action has not received `Init`, so it can run with missing context or default state.
  - It also rents twice from the pool per loop iteration.
- Recommendation:
  - Enqueue the same initialized instance: `queue.Enqueue(action);`.

#### Finding 3: `WorkerNPC.SetNextAction()` leaves `_currentAction` null for a frame after queue creation

- Severity: High
- File: `Assets/Scripts/Actor/WorkerNPC.cs:36`
- Evidence:
  - When the queue is null or empty, `SetNextAction()` requests a queue and immediately returns without dequeuing the first action.
  - `Update()` then returns as well.
- Why it matters:
  - The worker idles for at least one frame after every queue request.
  - If the selector returns an empty queue repeatedly, `_currentAction` remains stale or null with no explicit failed/idle state.
- Recommendation:
  - After requesting a non-empty queue, immediately dequeue the first action in the same method.
  - If the queue is empty, set `_currentAction = null` and leave a clear idle path.

#### Finding 4: Non-move actions still have unsafe lifecycle behavior

- Severity: High
- File: `Assets/Scripts/System/Action/FarmingAction.cs:3`
- Evidence:
  - `FarmingAction` directly implements `IAction` and every lifecycle method throws `NotImplementedException`.
- Why it matters:
  - Any selected work action will crash when initialized, ticked, cleared, or returned to the pool.
- Recommendation:
  - Convert `FarmingAction` to `DefaultAction` and implement safe placeholder behavior until real farming logic exists.

### Medium

#### Finding 5: `DefaultAction.Init()` starts actions immediately

- Severity: Medium
- File: `Assets/Scripts/System/Action/DefaultAction.cs:10`
- Evidence:
  - `Init(ActionContext context)` ends by calling `Start()`.
- Why it matters:
  - It merges configuration and execution lifecycle.
  - A selector that only builds a queue now starts every action during queue construction, before the runner makes it current.
  - This weakens the intended single action runner ownership described in `ProjectStructure.md`.
- Recommendation:
  - Prefer `Init()` for data setup only.
  - Let `WorkerNPC` or a future `NPCActionRunner` call `Start()` when the action becomes current.

#### Finding 6: `ActionContext.HasComponent` should use Unity truthiness for `NPCComponent`

- Severity: Medium
- File: `Assets/Scripts/System/Lib/ActionContext.cs:9`
- Evidence:
  - `HasComponent => Component != null`
- Why it matters:
  - `NPCComponent` is a `MonoBehaviour`. Unity destroyed-object semantics are handled more accurately by Unity truthiness.
- Recommendation:
  - Use `public bool HasComponent => Component;` if this compiles cleanly with the project Unity version.

#### Finding 7: `NPCComponent` methods assume optional serialized references are assigned

- Severity: Medium
- File: `Assets/Scripts/System/Actor/NPCComponent.cs:29`
- Evidence:
  - `Flip()` calls `_spriteRenderer.flipX` without checking `_spriteRenderer`.
  - `EnableObject()` calls `_gameObject.SetActive()` without checking `_gameObject`.
- Why it matters:
  - Missing inspector references cause runtime `NullReferenceException`.
- Recommendation:
  - Validate required references in `Awake()` or guard optional references before use.

### Low

#### Finding 8: `DefaultAction` is the right structure, but it should stay minimal

- Severity: Low
- File: `Assets/Scripts/System/Action/DefaultAction.cs:3`
- Evidence:
  - The base class currently contains only genuinely common lifecycle state and behavior.
- Recommendation:
  - Keep it this way. Do not move movement-only fields like destination or stopping distance back into the base class unless all actions truly need them.

#### Finding 9: Style cleanup remains before the skeleton grows

- Severity: Low
- Files:
  - `Assets/Scripts/System/Lib/ActionPool.cs:10`
  - `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs:6`
  - `Assets/Scripts/System/Lib/DestinationDB.cs:6`
  - `Assets/Scripts/System/Lib/CONST.cs:7`
- Evidence:
  - `_actionDictionary` lacks explicit `private`.
  - `actionPool` is a protected serialized field but does not follow `_camelCase`.
  - `_destionations` and `destionationName` are misspelled.
  - `COSNT_ASDF` appears to be placeholder/dead constant data.
- Recommendation:
  - Clean these now while the skeleton is small.

## Findings By File

- `Assets/Scripts/Interface/IAction.cs`: Good external contract shape. Keep internal helper methods like `Complete()` out of this interface.
- `Assets/Scripts/System/Lib/ActionContext.cs`: Reasonable `readonly struct` context. Consider Unity truthiness for `HasComponent`.
- `Assets/Scripts/System/Action/DefaultAction.cs`: Good base-class direction. The main concern is `Init()` calling `Start()`.
- `Assets/Scripts/System/Action/MoveAction.cs`: Much cleaner after moving common lifecycle state to `DefaultAction`. Movement-specific state is now correctly local to `MoveAction`.
- `Assets/Scripts/System/Action/EatAction.cs`: Does not compile with `ActionPool` because it is not an `IAction`.
- `Assets/Scripts/System/Action/DrinkAction.cs`: Does not compile with `ActionPool` because it is not an `IAction`.
- `Assets/Scripts/System/Action/SleepAction.cs`: Does not compile with `ActionPool` because it is not an `IAction`.
- `Assets/Scripts/System/Action/FarmingAction.cs`: Still uses the old direct-`IAction` stub style and throws from lifecycle methods.
- `Assets/Scripts/System/Lib/ActionPool.cs`: Pool responsibility is correct, but it currently assumes action types that no longer implement the required interface.
- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`: Correctly uses `ActionContext` for `MoveAction`, but has a concrete bug where the initialized action is not the action enqueued.
- `Assets/Scripts/Actor/WorkerNPC.cs`: The queue runner direction is reasonable, but queue creation and first-action dequeue should happen as one lifecycle step.
- `Assets/Scripts/System/Actor/NPCComponent.cs`: Thin Unity-facing capability holder is appropriate. Needs null validation for serialized references.
- `Assets/Scripts/System/Actor/NPCStat.cs`: Move speed constructor issue is fixed. Existing `Get...` property naming remains a style mismatch.
- `Assets/Scripts/System/Lib/DestinationDB.cs`: Better destination storage shape than before. Still has spelling/style cleanup.

## Cross-Cutting Findings

- The action inheritance direction is sound: `IAction` as the public contract, `DefaultAction` as reusable lifecycle implementation, concrete actions as behavior-specific logic.
- The migration must be completed consistently. A mixed state where some actions inherit `DefaultAction`, some implement `IAction` directly, and some do neither breaks both compile safety and readability.
- Queue ownership is close but not fully clean yet. Selectors should build initialized action requests or initialized action instances, while the runner should decide when an action actually starts.
- Placeholder actions should be safe no-ops or immediate-complete actions, not throwing stubs, once they can be selected or pooled.

## Positive Notes

- `Complete()` being protected on `DefaultAction` is the right encapsulation.
- Removing `_destination` from `DefaultAction` and keeping it in `MoveAction` is a good correction.
- `ActionContext` is a reasonable small value type because it mostly carries references and optional request data.
- `MoveAction` now reads much closer to a focused behavior implementation.

## Recommended Next Actions

1. Make `EatAction`, `DrinkAction`, `SleepAction`, and `FarmingAction` inherit `DefaultAction`.
2. Fix `FarmerActionSelector` to enqueue the initialized `action` instance.
3. Separate `Init()` from `Start()` so actions begin only when the runner makes them current.
4. Update `WorkerNPC.SetNextAction()` to dequeue immediately after creating a non-empty queue.
5. Add null validation or guards for `NPCComponent` serialized references.
6. Clean small naming/style issues while the codebase is still small.
