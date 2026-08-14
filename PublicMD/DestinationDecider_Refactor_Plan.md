# DestinationDecider Refactor Plan

## 1. Purpose

Refactor `DestinationDecider` so an NPC can choose between multiple concrete items and activities by considering:

- the NPC's current needs;
- every stat delta produced by an item or action;
- the NPC's current position and each destination's travel cost;
- how many consecutive work actions remain safe after the choice;
- action time and configurable utility weights.

The resulting behavior must avoid the loop where an NPC performs one work action, leaves to satisfy a need, then returns for one more work action.

This document is an implementation plan. Do not edit runtime implementation files until the plan has been reviewed and approved.

## 2. Execution Requirements

1. Perform the planning pass in Claude Opus Plan Mode.
2. Before implementation, read these documents in order:
   - `PublicMD/ARCHITECTURE.md`
   - `PublicMD/ProjectStructure.md`
   - `PublicMD/CodeConvention.md`
   - this document
3. Confirm the implementation scope and receive explicit approval.
4. Perform the implementation pass with Claude Sonnet.
5. After implementation, compile, run the relevant tests, launch the independent Codex review agent required by the project skill, and update `PublicMD/PROGRESS.md`.

## 3. Current Problems

### 3.1 Decision predictions do not match execution

`DestinationDecider` predicts fixed effects:

- Drink sets thirst to zero.
- Eat sets hunger to zero.
- Sleep sets fatigue to zero.

Actual food and drink behavior applies a complete `StatEffect`. One item may reduce hunger and fatigue while increasing thirst. The decider currently cannot represent or score that result.

### 3.2 The NPC does not select a concrete item

`Pub.TryInteraction` randomly chooses an item after the NPC arrives. The decision contains only `Eat` or `Drink`, so the NPC cannot compare individual items or ensure that the predicted item is the item consumed.

### 3.3 Plans remain committed after uncertain state changes

The current decision can enqueue multiple supply destinations followed by work. `WorkerNPC` requests a new decision only after the complete queue is empty. An Eat or Drink result can therefore invalidate the remaining precomputed plan without causing replanning.

### 3.4 One work action is forced

`DestinationDecider` uses `Mathf.Max(1, workCount)`. Even when zero safe work actions are predicted, one work action is queued. This directly permits the unwanted "work once, leave, return" behavior.

### 3.5 Work prediction is internally inconsistent

The decision records `workCount`, but projected post-work needs apply the work cost only once. Scoring, `NextRequiredIntent`, and the queued repeat count can therefore describe different final states.

### 3.6 Hardcoded tuning has drifted from runtime data

Travel and work need deltas are constants inside `DestinationDecider`, while farming execution reads `FarmingActionCost`. Prediction and execution need one source of truth.

## 4. Approved Design Direction

### 4.1 Use a short decision horizon

One decision may select only one semantic destination/activity:

```text
Decide
  -> Move to selected destination
  -> Execute selected interaction or a bounded work batch
  -> End queue
  -> Re-read actual position and stats
  -> Decide again
```

Do not precompute `Eat -> Drink -> Work` across multiple destinations. Replanning from the new position naturally gives nearby follow-up destinations a lower travel cost and preserves the "handle another need while nearby" behavior.

Work is the only activity allowed to contain multiple repeats in one decision. Its repeat count must be bounded and predicted as one batch.

### 4.2 Evaluate concrete interaction options

The decision candidate is not merely `Eat` or `Drink`. It must identify the concrete option that will execute:

```text
InteractionOption
  ActionType
  ItemId
  StatEffect
  estimated duration or action cost
```

Destination position and provider are supplied by `DestinationDB`/`DestinationInfo`. Availability belongs to the interaction provider. Priority and utility belong to `DestinationDecider`.

The provider must execute the selected item ID. It must not silently replace it with a random item.

If an option becomes unavailable between decision and execution, the action completes as failed/no-effect and the empty queue causes a fresh decision. Do not substitute another item without reevaluation.

### 4.3 Separate hard gates from utility scoring

Apply hard safety rules before comparing scores:

1. Predict travel to the workplace.
2. Predict the maximum safe consecutive work count using the actual farming cost data.
3. If the safe count is below `MinimumWorkBatch`, Work is not a valid candidate.
4. If a need is critical, reject candidates that do not improve that critical need enough to remain safe.
5. If no supply candidate is available and Work is unsafe, return no decision/idle fallback. Never force one Work action.

After filtering, compare valid candidates using utility.

### 4.4 Utility model

Use raw stat values for prediction and normalize only while calculating risk. `StatEffect` values are raw deltas and must be clamped against each stat's actual maximum.

Recommended utility shape:

```text
utility =
    riskBefore - riskAfter
  + gainedSafeWorkCount * workCapacityWeight
  + producedWorkValue
  - travelTime * travelWeight
  - actionDuration * actionTimeWeight
  - resourceCost * resourceWeight
```

Risk rules:

- Hunger, thirst, and fatigue become more expensive nonlinearly near their danger threshold.
- Health loss must receive a penalty so an item is not selected only because it improves needs.
- Every `StatEffect` field supported by `NPCStat` must be applied during prediction.
- `MoodDelta` is excluded until mood exists in `NPCStat`; document this explicitly in code.

Candidate comparison must use a stable tie-break order so equal scores do not produce frame-to-frame nondeterminism. Suggested order: higher utility, shorter travel time, lower item ID, then lower enum value.

### 4.5 Minimum work batch

Add designer-controlled values:

- `MinimumWorkBatch`: Work cannot be selected below this predicted repeat count.
- `MaximumWorkBatch`: caps queue length and forces periodic replanning.
- `CriticalNeedThreshold`: hard safety threshold.
- `SwitchMargin`: a supply option must beat Work by this margin before causing a noncritical detour.

Do not hardcode final game-balance values. Store provisional prototype values in a dedicated decision-tuning asset and label them as tuning data.

## 5. Responsibility Boundaries

### `DestinationDecider`

Owns candidate construction, state prediction, hard gates, safe work-count estimation, utility scoring, and deterministic tie-breaking.

It must not create `IAction`, mutate `NPCStat`, consume items, execute interactions, or manage the active queue.

### `DestinationDB`

Owns destination records and typed access to a destination position plus its interaction capability. It must not score destinations or choose items.

### `IInteractionProvider` / `Pub`

Owns available concrete interaction options, validation, and execution of the requested option. It must not decide which option is best for the NPC.

### `FarmerActionSelector`

Converts one `NPCDecision` into `Move + selected action`, or `Move + N FarmingAction` for a work batch. It must contain no utility, distance, threshold, or need-prediction formulas.

### `WorkerNPC`

Continues owning action-queue lifecycle. Because each queue now represents one semantic decision, an empty queue becomes the replanning boundary.

### Actions

`EatAction` and `DrinkAction` execute the exact `InteractionRequest` stored in `ActionContext`. `FarmingAction` continues applying authoritative farming costs. Actions do not choose the next behavior.

## 6. Data Contract Changes

Names may be adjusted to match local style, but preserve these responsibilities.

### Add `InteractionOption`

Suggested location: `Assets/Data/Struct/InteractionOption.cs`.

Required data:

- `ActionType Type`
- `int ItemId`
- `StatEffect Effect`
- optional estimated action duration/resource cost only when those values have an authoritative source

This is read-only decision input. It must not expose mutable facility inventory.

### Revise `InteractRequest`

Revise `Assets/Data/Struct/InteractStruct.cs` so the request identifies the selected `ActionType` and `ItemId`. Remove the unused `WorkerNPC` dependency unless an immediate execution requirement proves it necessary.

### Revise `IInteractionProvider`

The provider contract must support both:

- querying available options for an `ActionType` without mutation;
- executing a specific `InteractRequest`.

Use `Try...` failure semantics. Avoid returning internal mutable lists directly; use `IReadOnlyList<T>`, a copied snapshot, or a caller-supplied buffer.

### Revise `ActionContext`

Add the selected interaction request without turning `ActionContext` into a service locator. Clear/reset pooled actions so a previous request cannot leak into the next execution.

### Revise `NPCDecisionStep`

Carry the selected interaction request or item ID for supply decisions. Preserve `RepeatCount` for Work. Establish and test the invariant that one decision contains at most one semantic step.

## 7. File-Level Implementation Plan

### Phase A: Interaction choice and execution parity

Modify:

- `Assets/Data/Struct/InteractStruct.cs`
- `Assets/Data/Struct/ActionContext.cs`
- `Assets/Scripts/Interface/IInteractionProvider.cs`
- `Assets/Scripts/Actor/BaseInteractable.cs`
- `Assets/Scripts/Actor/Pub.cs`
- `Assets/Scripts/System/Action/EatAction.cs`
- `Assets/Scripts/System/Action/DrinkAction.cs`

Create if needed:

- `Assets/Data/Struct/InteractionOption.cs`

Required result:

- `Pub` exposes Food and Drink items as concrete options.
- Eat/Drink consumes the requested item ID and applies exactly that option's `StatEffect`.
- Unsupported, missing, or unavailable requests return `false` without applying a substitute effect.
- Remove direct use of `UnityEngine.Random` from the selection path.

### Phase B: Decision tuning data

Create:

- `Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs`
- a corresponding asset under `Assets/Data/ScriptableObject`

Move the following policy values out of `DestinationDecider`:

- thresholds;
- need-risk weights;
- work capacity/value weight;
- travel and action-time weights;
- `MinimumWorkBatch` and `MaximumWorkBatch`;
- `SwitchMargin`.

Do not duplicate farming need deltas in this asset. Farming deltas remain authoritative in `FarmingActionCost` and are passed into decision prediction as plain read-only values.

Movement-related need deltas are excluded from this refactor because `MoveAction` currently does not apply them. Distance still contributes travel-time utility. Add movement need consumption in a separate task so prediction and execution can be changed together.

### Phase C: Rewrite `DestinationDecider`

Modify:

- `Assets/Scripts/System/Lib/DestinationDecider.cs`
- `Assets/Data/Struct/NPCDecision.cs`

Implementation steps:

1. Replace percentage-only `NeedSnapshot` prediction with raw current/max values.
2. Add a pure helper that applies a complete `StatEffect` to a copied snapshot with the same clamp rules as `NPCStat`.
3. Obtain concrete interaction options from each available provider.
4. Build candidates for every available item, Sleep, and Work.
5. Predict each candidate from the NPC's current position, including travel time.
6. Calculate danger/risk reduction and gained safe work capacity.
7. Estimate safe Work repeats using the authoritative farming deltas.
8. Reject Work when repeats are below `MinimumWorkBatch`.
9. Cap accepted Work repeats at `MaximumWorkBatch`.
10. Remove `Mathf.Max(1, workCount)` and remove the fixed `ApplyEat`/`ApplyDrink` assumptions.
11. Apply Work cost `N` times, or use mathematically equivalent clamped projection, when calculating the final work state.
12. Select one candidate with deterministic tie-breaking and return one semantic step.

Keep the decider free of `IAction`, `ActionPool`, queue creation, and stat mutation.

### Phase D: Selector and queue boundary

Modify:

- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`
- `Assets/Scripts/Actor/WorkerNPC.cs` only if required to make the replanning boundary explicit

Required result:

- Supply decision queue: `MoveAction`, then exactly one Eat/Drink/Sleep action.
- Work decision queue: `MoveAction`, then `RepeatCount` Farming actions.
- No decision queue contains multiple destinations.
- Once the queue completes, the next request uses actual current position and actual `NPCStat` values.
- Empty/unsafe decisions do not spin by allocating a new queue every frame; preserve or add a minimal idle/backoff policy if necessary.

Do not add scoring formulas to `FarmerActionSelector`.

### Phase E: Documentation and cleanup

Update:

- `PublicMD/NPC_Decision_System_Plan.md`
- `PublicMD/PROGRESS.md` after validation

Remove or revise obsolete concepts such as multi-destination greedy-chain documentation, fixed supply effects, and unused `NextRequiredIntent` if it has no remaining caller.

Do not modify `PublicMD/Code_Evaluation_Result.md`; the independent review agent owns it.

## 8. Tests

Add table-driven Edit Mode tests for pure decision behavior. Do not introduce `NUnit` references into runtime assemblies.

Required decision cases:

1. Healthy needs and sufficient work capacity select a Work batch.
2. Safe work count below `MinimumWorkBatch` never selects Work.
3. Safe work count zero never produces a forced single Work action.
4. Work repeat count is capped by `MaximumWorkBatch`.
5. A food with multiple beneficial effects can beat a food with a larger hunger-only effect.
6. A food that increases thirst includes that penalty in its score.
7. A farther high-effect item can lose to a nearer adequate item due to travel cost.
8. Critical hunger cannot be ignored by a high Work value.
9. No safe work and no available supply returns no decision/idle fallback.
10. Equal-score candidates use deterministic tie-breaking.
11. Projected Work state applies the cost for every queued repeat.
12. Different stat maxima produce correct normalized risk from the same raw delta.

Required interaction/action cases:

1. `Pub` returns the requested item ID, not a random item.
2. A missing item ID fails without changing stats.
3. Eat/Drink applies all supported `StatEffect` fields.
4. A pooled Eat/Drink action does not retain the previous request after `Clear()` and reinitialization.
5. Completing one supply queue causes the next decision to observe the real post-effect stats and current position.

Required manual Unity smoke cases:

1. Place food and drink destinations at visibly different distances and confirm the selected destination changes with need state.
2. Use an item that reduces hunger/fatigue but increases thirst and confirm the next decision reflects all three changes.
3. Configure a state that permits fewer than `MinimumWorkBatch` jobs and confirm the NPC satisfies a need before returning to work.
4. Configure no usable supply and unsafe Work and confirm the NPC does not perform one forced job.
5. Let the NPC complete a bounded work batch and confirm it replans afterward.

## 9. Acceptance Criteria

The refactor is complete only when all of the following are true:

- The NPC selects and executes a concrete item rather than a category-only random result.
- Decision prediction applies the same `StatEffect` that execution applies.
- Every supply action ends the current semantic plan and triggers reevaluation before Work is appended.
- A Work decision is impossible when predicted repeats are below `MinimumWorkBatch`.
- No code path forces one Work action when the safe count is zero.
- Work prediction applies the farming cost for the complete queued batch.
- Distance, compound item effects, work capacity, and risk all affect selection.
- Tuning values are data-driven and farming costs have one source of truth.
- Selector/action/provider responsibility boundaries remain intact.
- `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with zero errors.
- Relevant Edit Mode tests pass.
- Scene/prefab serialized references for new tuning data are assigned and verified.
- `PublicMD/PROGRESS.md` records changed files, decisions, validation results, and review-agent launch status.

## 10. Explicit Exclusions

Do not include these in this refactor:

- full GOAP or exhaustive multi-step route search;
- multiple facilities of the same `BuildingType`;
- NavMesh/path-obstacle-aware distance estimation;
- movement-driven need consumption;
- mood scoring before mood exists in `NPCStat`;
- full facility inventory reservation or economy pricing;
- combat/emergency preemption during an already-running work batch;
- renaming `DestinationDecider` solely for terminology;
- unrelated `WorkerNPC`, pooling, manager, or UI refactors.

If one of these becomes necessary to satisfy a required acceptance criterion, stop and request a scope revision rather than silently expanding the task.

## 11. Known Risks

- An option can disappear after planning. Execution must fail cleanly and replan without substituting another item.
- Shared provider collections can be mutated accidentally. Expose read-only options or snapshots.
- Long Work batches delay reaction to external state changes. Keep `MaximumWorkBatch` bounded; emergency preemption remains a later task.
- Incorrect sign handling can reverse need effects. Tests must use both negative improvements and positive penalties.
- New serialized tuning references can be null in existing scenes. Validate them and provide a clear diagnostic instead of a null exception.
- Adding test assembly definitions can affect Unity compilation boundaries. Do not migrate the entire runtime assembly as part of this task merely to add tests.

