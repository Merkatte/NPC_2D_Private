# DestinationDecider Rational Utility Rewrite Plan

## 1. Purpose

Rewrite the internals of `DestinationDecider` so Farmer and Guard NPCs make rational,
position-aware decisions without a fixed "satisfied need" threshold and without machine
learning.

The target behavior is:

```text
Perform one semantic action
  -> observe the real resulting stats and position
  -> compare every currently valid alternative again
  -> stay at the current facility when another interaction has better marginal value
  -> return to the role activity when its value exceeds the benefit of staying
```

The rewrite may replace most of `DestinationDecider.cs`, but it must preserve the public
decision API and the existing action/queue execution architecture.

This plan supersedes only the scoring and decision-horizon portions of
`PublicMD/DestinationDecider_Refactor_Plan.md`. The already implemented concrete-item,
single-semantic-step, interaction-request, and bounded-work-batch rules remain authoritative.

## 2. User-Approved Decisions

The following decisions are fixed for this implementation:

1. Do not add machine learning or Unity ML-Agents.
2. Do not add a fixed recovery/satisfaction exit threshold.
3. Eat, Drink, and Sleep remain one semantic action per decision. Do not prequeue repeated
   supply actions.
4. After each supply action, use the real current stat values and real current position to
   decide again.
5. Complex calculation is acceptable. Prefer rational, explainable behavior over preserving
   the current scoring implementation.
6. A nonlinear need-risk curve and bounded future look-ahead are allowed.
7. Preserve this public API exactly:

   ```csharp
   public NPCDecision Decide(
       IStatView stat,
       NPCType npcType,
       Vector3 npcLoc,
       StatEffect workCost)
   ```

8. Preserve `NPCDecision`, `ActionContext`, `IAction`, `WorkerNPC`, and action queue lifecycle
   contracts unless a compile-time blocker is discovered.
9. Large changes must remain concentrated in `DestinationDecider.cs`. Small supporting changes
   to the Guard call policy and decision tuning are explicitly allowed.
10. Do not add public candidate-builder interfaces in this pass. Candidate types and evaluation
    helpers stay private inside `DestinationDecider` unless a concrete test seam requires an
    internal helper.

## 3. Confirmed Current Behavior and Root Causes

### 3.1 Guard bypasses the decider after one recovery action

`GuardActionSelector.RequestNewActionQueue(...)` calls `DestinationDecider` only while
`GuardActionCost.ShouldInterrupt(stat)` is true. If bread changes Hunger from 100 to 75, the
selector does not ask the decider whether another bread is worthwhile from the current Pub
position; it immediately builds the Guard queue.

No rewrite confined to `DestinationDecider.cs` can fix a call that never occurs. The Guard
selector gate therefore requires a small change.

### 3.2 Farmer does re-decide, but the current utility scale strongly favors Work

`FarmerActionSelector` already requests a new decision after a one-action supply queue finishes.
However, the current `WorkValue`, `RiskWeight`, `WorkCapacityWeight`, and `SwitchMargin` scale can
make Work dominate another nearby supply action immediately after the first recovery.

### 3.3 The current model is one-step and myopic

The current score compares the immediate selected option but does not sufficiently value this
sequence:

```text
Return to role activity now
  -> needs rise during role activity
  -> another supply trip becomes necessary soon
```

That omitted future trip is the main reason a locally reasonable decision can create globally
inefficient back-and-forth movement.

### 3.4 Runtime and prediction currently have limited authoritative costs

Current authoritative inputs include:

- current/max needs and health;
- current position and move speed;
- destination positions;
- concrete item `StatEffect` values;
- Farmer per-work-action `StatEffect`;
- Guard per-second need growth in `GuardActionCost`.

Movement does not currently mutate needs. The rewrite must not invent movement need consumption
that runtime actions do not apply. Travel still has elapsed-time and opportunity cost.

Eat, Drink, Sleep, and Farming durations are currently hardcoded in actions. Decision tuning may
contain explicitly named estimated durations for this prototype, but the implementation must not
claim they are authoritative runtime values. Unifying execution durations into shared action
tuning is a later task.

## 4. Required Architecture Boundary

```text
NPCStat / IStatView
  -> supplies a read-only current-state snapshot

DestinationDB / IInteractionProvider
  -> supplies destination positions and concrete available interaction options

DestinationDecider
  -> predicts copied states, evaluates candidates, and returns one NPCDecision

FarmerActionSelector / GuardActionSelector
  -> converts the decision into Queue<IAction>

IAction implementations
  -> execute the selected request and mutate the real NPCStat

WorkerNPC
  -> owns queue lifecycle and requests a new decision when the queue ends
```

`DestinationDecider` must not create or rent actions, mutate the real `NPCStat`, execute an
interaction, own a queue, or access `WorkerNPC`.

## 5. Decision Model

### 5.1 Keep candidate types private

Replace or revise the current private `Candidate` so it can represent both supply and role
activity alternatives. A suggested internal shape is:

```csharp
private enum CandidateKind
{
    Idle,
    Supply,
    FarmerWork,
    GuardDuty,
}

private struct Candidate
{
    public CandidateKind Kind;
    public NPCIntent Intent;
    public BuildingType Key;
    public Vector3 Position;
    public ActionType ActionType;
    public int ItemId;
    public int RepeatCount;
    public float TravelTime;
    public float ActionTime;
    public float ActivityReward;
    public NeedSnapshot After;
    public float Score;
}
```

Names may be adjusted to match the final implementation, but candidate data must remain a plain
prediction record and must not contain `IAction` or queue references.

### 5.2 Candidate set by role

Every evaluation state must build these candidates when valid:

```text
All roles:
  - Idle/current-role fallback at the current position
  - every concrete Eat option
  - every concrete Drink option
  - Sleep when Inn is available

Farmer:
  - bounded Farming work batch at Farm

Guard:
  - one Guard duty evaluation slice at GuardPost

Cook:
  - no role activity candidate until Cook behavior exists; use Idle fallback
```

Do not add `NPCIntent.Guard` in this pass. An internal GuardDuty candidate may convert to
`NPCDecision.Idle(...)`; `GuardActionSelector` normally treats a non-supply result as a request to
build the existing Guard queue. The only exception is an interrupt-level need with no usable
supply, which uses the timed Idle fallback to avoid an immediate Guard/Replan loop.

### 5.3 One semantic action remains the runtime boundary

- Supply candidate: `RepeatCount = 1`.
- Farmer Work candidate: existing bounded safe `RepeatCount` is preserved.
- Guard duty candidate: represents a planning slice only; runtime still starts the existing
  continuous `GuardAction`.
- Future look-ahead is prediction only. Never convert predicted follow-up actions into the
  returned runtime queue.

## 6. Nonlinear Need Risk

Keep the critical threshold as a hard emergency gate, not a satisfaction threshold.

For ordinary utility, calculate a smooth nonlinear risk from normalized need `n`:

```text
baseRisk(n) = pow(clamp01(n), NeedRiskExponent)
```

Recommended initial exponent: 3 or 4. Store it in `NPCDecisionTuning`; do not hardcode a final
balance value.

Optionally retain an additional continuous danger-region term:

```text
dangerOver = max(0, n - DangerThreshold)
dangerRisk = dangerOver^2 * DangerPenaltyMultiplier

NeedRisk(n) = baseRisk(n) + dangerRisk
```

This gives a low marginal value to reducing already-low needs and a steep marginal value near the
maximum. Candidate benefit is derived from state-value differences; do not add a rule such as
"continue eating until Hunger is below X."

Health risk remains inverse:

```text
healthRisk = pow(1 - normalizedHealth, HealthRiskExponent) * HealthWeight
```

Use finite checks and clamp normalized inputs. Zero max values must not generate NaN or infinity.

## 7. Bounded Future Evaluation

### 7.1 Required horizon

Implement deterministic depth-2 look-ahead initially:

```text
root candidate
  -> predicted state and position
  -> best valid next candidate
  -> terminal state value
```

Expose the maximum depth in `NPCDecisionTuning`, clamped to 1..3. The initial asset value should be
2. Do not implement unbounded planning.

### 7.2 Suggested score

The exact helper decomposition is implementation-defined, but the model must include equivalent
terms:

```text
ImmediateScore(candidate) =
    candidate.ActivityReward
  - candidate.TravelTime * TravelWeight
  - candidate.ActionTime * ActionTimeWeight
  - riskExposureDuringCandidate

TotalScore(state, candidate, depth) =
    ImmediateScore(candidate)
  + discount * BestFutureScore(candidate.After, candidate.Position, depth - 1)
```

At the final depth, include terminal state value:

```text
TerminalValue(state) = -ComputeRisk(state) * TerminalRiskWeight
```

The implementation may use an algebraically equivalent formulation. Add comments explaining the
units and sign of every term.

### 7.3 Risk exposure during elapsed time

Because current runtime movement does not change needs, do not add fictional stat deltas during
travel. It is still rational to penalize spending time in a dangerous state:

```text
riskExposure = average(riskBefore, riskAfter) * elapsedTime * RiskExposureWeight
```

If the runtime later gains a global needs-over-time system, prediction must be updated in the same
feature slice that adds that runtime behavior.

### 7.4 Role activity transitions

Farmer Work:

- use the passed `workCost` as the authoritative cost per Farming action;
- preserve safe repeat estimation;
- apply the cost for every predicted repeat;
- reward produced work using the existing Work value or its replacement tuning;
- preserve `MinimumWorkBatch` and `MaximumWorkBatch` hard validity rules.

Guard duty:

- `GuardActionSelector` creates one `StatEffect` representing the authoritative Guard need rates
  multiplied by `GuardDutyEvaluationSeconds`;
- pass that effect through the existing `workCost` argument;
- `DestinationDecider` interprets the value as one Guard duty planning slice when
  `npcType == NPCType.Guard`;
- candidate destination is `BuildingType.GuardPost`;
- candidate duration is `GuardDutyEvaluationSeconds`;
- candidate reward is `GuardDutyValuePerSecond * GuardDutyEvaluationSeconds`.

This preserves the public method signature and avoids copying Guard need rates into decision
tuning.

### 7.5 Why "while already out" emerges

At a Pub after one Eat action:

- another Eat candidate has approximately zero travel time;
- Guard duty or Farmer Work includes the trip back to GuardPost/Farm;
- duty/work increases future needs;
- depth-2 evaluation can see whether another supply trip will soon be needed.

The NPC stays only while the marginal value of another interaction exceeds the role activity
alternative. No satisfaction threshold or prebuilt repeat count is involved.

## 8. Hard Safety Rules

Apply safety filtering before ordinary score comparison:

1. Identify needs strictly above `CriticalNeedThreshold`.
2. While any need is critical, Farmer Work and Guard duty are invalid.
3. Prefer supply candidates that do not worsen any currently critical need and improve at least
   one critical need.
4. Preserve the existing ordered critical fallback behavior when no strictly safe option exists:
   reduce peak critical need, then reduce total critical risk.
5. If there is no valid improving supply, return the role fallback/Idle safely; do not force Work.
6. Work remains invalid when predicted safe repeats are below `MinimumWorkBatch`.

Critical filtering must also be applied in simulated future nodes, not only at the root.

## 9. Determinism and Performance

- Do not use `UnityEngine.Random`.
- Preserve deterministic tie-breaking: score descending, travel time ascending, item ID ascending,
  action type ascending, building type ascending.
- Use an epsilon when comparing floating-point scores so insignificant noise does not change the
  chosen action.
- Limit look-ahead depth to the tuning range 1..3.
- The decider runs at replanning boundaries, not every action tick. Still avoid LINQ and avoid
  unnecessary per-node allocations.
- Reuse candidate and interaction buffers where practical. Do not introduce a complex generic
  planner framework solely for allocation reduction.
- Protect all score calculations against NaN and infinity. Invalid candidates must be rejected
  with a clear diagnostic in development builds.

## 10. File-Level Implementation Plan

### 10.1 Major rewrite

Modify:

- `Assets/Scripts/System/Lib/DestinationDecider.cs`

Required work:

1. Preserve `Init(...)` and `Decide(...)` public signatures.
2. Preserve concrete item option lookup and exact `StatEffect` projection.
3. Replace the current one-step utility calculation with the bounded evaluation described above.
4. Add internal role candidates for Farmer Work and Guard duty.
5. Preserve one returned semantic action.
6. Preserve bounded Farmer work repeat calculation.
7. Remove the current one-directional `bestSupply >= work + SwitchMargin` branch. Every valid
   candidate must participate in one comparable scoring model after hard safety filtering.
8. Keep scoring, prediction, and tie-break helpers pure with respect to real runtime state.
9. Return the root candidate only; never return predicted future actions.

### 10.2 Small Guard integration change

Modify:

- `Assets/Scripts/System/Actor/GuardActionSelector.cs`

Required work:

1. Add a cached plain `StatEffect` for one Guard duty planning slice. Build it once in `Start()`
   from `GuardActionCost.HungerPerSecond`, `ThirstPerSecond`, and `FatiguePerSecond` multiplied by
   `NPCDecisionTuning.GuardDutyEvaluationSeconds`.
2. Preserve combat as the highest-priority branch.
3. After combat selection fails, call `DestinationDecider.Decide(...)` on every queue request,
   not only when `ShouldInterrupt(stat)` is currently true.
4. Pass the cached Guard duty cost through the existing `workCost` argument.
5. If the result is Eat/Drink/Sleep, build the existing one-action need queue.
6. For a non-supply result, build the existing Guard queue only when
   `GuardActionCost.ShouldInterrupt(stat)` is false. If an interrupt-level need has no usable
   supply candidate, build the existing timed Idle fallback so `GuardAction` does not immediately
   request another replan every frame.
7. Keep `GuardActionCost.ShouldInterrupt(...)` inside `GuardAction`; it remains the trigger that
   interrupts a running continuous patrol and requests the initial need replan.
8. Do not add utility formulas to the selector.

### 10.3 Decision tuning

Modify:

- `Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs`
- `Assets/Data/ScriptableObject/NPCDecisionTuning.asset`

Retain fields that remain meaningful and add or replace fields needed for:

- `NeedRiskExponent`;
- optional `HealthRiskExponent`;
- `TerminalRiskWeight`;
- `RiskExposureWeight`;
- `ActionTimeWeight`;
- `FutureDiscount`;
- `LookAheadDepth` clamped to 1..3;
- estimated Eat, Drink, Sleep, and Farming durations;
- `GuardDutyEvaluationSeconds`;
- `GuardDutyValuePerSecond`;
- deterministic score comparison epsilon if it must be designer-tunable.

Remove `SwitchMargin` if it is no longer used. Do not leave a serialized tuning field with zero
callers. Existing serialized asset values must be reviewed because Unity retains fields by name
and silently defaults newly added fields.

Estimated action durations must be clearly named and documented as prediction-only until runtime
action duration tuning is unified.

### 10.4 Files expected to remain unchanged

Do not modify unless a concrete blocker is found:

- `Assets/Scripts/Actor/WorkerNPC.cs`
- `Assets/Scripts/Interface/IAction.cs`
- `Assets/Scripts/System/Action/DefaultAction.cs`
- `Assets/Scripts/System/Action/MoveAction.cs`
- `Assets/Scripts/System/Action/EatAction.cs`
- `Assets/Scripts/System/Action/DrinkAction.cs`
- `Assets/Scripts/System/Action/SleepAction.cs`
- `Assets/Scripts/System/Action/FarmingAction.cs`
- `Assets/Data/Struct/ActionContext.cs`
- `Assets/Data/Struct/NPCDecision.cs`
- `Assets/Data/Struct/InteractStruct.cs`
- `Assets/Scripts/Interface/IInteractionProvider.cs`
- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`
- `Assets/Scripts/System/Action/GuardAction.cs`
- `Assets/Scripts/System/Lib/DestinationDB.cs`
- `Assets/Scripts/System/Actor/NPCStat.cs`

If one of these files must change, stop and explain the blocker before broadening the scope.

## 11. Implementation Sequence

1. Record current decision outputs for representative Farmer and Guard states before editing.
2. Add and validate the required `NPCDecisionTuning` fields and asset values.
3. Rewrite `DestinationDecider` prediction and scoring helpers while keeping `Decide(...)`
   compiling at each stage.
4. Preserve or reimplement critical safety tests before adding future look-ahead.
5. Add role activity candidates.
6. Add depth-2 future evaluation and deterministic selection.
7. Make the small Guard selector integration change.
8. Run the build and targeted code searches.
9. Run deterministic decision scenarios and Play Mode smoke tests.
10. Update `PublicMD/PROGRESS.md` and launch the independent Codex review agent according to the
    project implementation skill after validation.

## 12. Required Decision Scenarios

Add automated tests if the current test assembly can support them without restructuring runtime
assemblies. Otherwise add a deterministic TestOnly probe and record its output.

### Farmer scenarios

1. Healthy Farmer at Farm chooses a bounded Work batch.
2. Critical Hunger rejects Work and chooses a valid Food option.
3. Farmer at Pub after one bread may choose another bread when its marginal value plus avoided
   travel exceeds returning to Farm.
4. After further recovery, Farmer eventually chooses Work without consulting a satisfaction
   threshold.
5. The same stat state can choose differently at Pub and at Farm because travel cost differs.
6. Unsafe work count below `MinimumWorkBatch` never forces one Farming action.

### Guard scenarios

1. Healthy Guard at GuardPost chooses Guard duty.
2. Critical need interrupts patrol and chooses a matching supply action.
3. Guard at Pub after one supply action invokes the decider again even below the Guard interrupt
   threshold.
4. Guard may perform another supply action while already at the facility when it is rational.
5. Guard eventually returns to GuardPost when duty value exceeds another supply interaction.
6. Guard at GuardPost with a moderate need does not leave when the round trip costs more than the
   marginal recovery value.
7. Combat remains higher priority than supply and Guard duty.

### General scenarios

1. Equal-score candidates always produce the same result.
2. A farther high-effect item can lose to a nearer adequate item.
3. Compound item effects include all supported `StatEffect` fields.
4. Health-damaging supplies include the health penalty.
5. Zero max stats and extreme tuning values do not produce NaN or infinity.
6. Look-ahead depth 1 and 2 both terminate and produce valid decisions.
7. No destination/provider produces a safe Idle/role fallback rather than an exception or spin.

## 13. Manual Unity Verification

Use `GuardTest.unity` after preserving the user's existing uncommitted scene changes.

1. Set Guard need growth to the current stress-test rate of 10 per second.
2. Confirm the Guard leaves for a critical need.
3. Observe at least two consecutive post-supply decisions while it remains near the facility.
4. Confirm it does not mechanically return to GuardPost immediately after the first item.
5. Confirm it eventually returns instead of consuming forever.
6. Move the facility farther away and confirm the departure decision changes at moderate needs.
7. Spawn an enemy and confirm combat still preempts the ordinary utility decision.
8. Repeat equivalent Farmer checks between Pub and Farm.

The implementation must expose enough development logging to print, on demand rather than every
frame:

```text
NPC type / current position / normalized needs
candidate intent and destination
travel cost / action-time cost / activity reward / risk terms / future score / total score
selected candidate and deterministic tie-break reason
```

Logging must be disabled by default or gated by a serialized development flag.

## 14. Validation

Required before completion:

1. `dotnet build Assembly-CSharp.csproj --no-restore` succeeds with 0 errors.
2. Targeted search confirms the public `Decide(...)` signature and all existing call sites remain
   valid.
3. Targeted search confirms `SwitchMargin` and any replaced tuning fields have no stale callers.
4. Targeted search confirms no `UnityEngine.Random` was added to decision selection.
5. Automated decision tests or the deterministic TestOnly scenario probe pass.
6. Manual Farmer and Guard scenarios above are recorded.
7. Existing user changes in `GuardTest.unity`, `Enemy.cs`, Guard stat assets, and recovery files are
   not overwritten or accidentally staged.

## 15. Acceptance Criteria

The task is complete only when all of these are true:

- Supply actions remain one action per semantic decision.
- After each supply action, actual position and actual stats are read again.
- Guard asks the decider again after a supply action even after dropping below its patrol interrupt
  threshold.
- No fixed satisfaction/recovery exit threshold exists.
- Farmer Work and Guard duty compete with supply candidates in an explainable score model.
- Current location materially affects the decision.
- At a supply facility, another useful local interaction can beat an inefficient round trip.
- The NPC eventually returns to role activity when the marginal supply benefit becomes lower.
- Critical needs remain hard safety constraints.
- Look-ahead is bounded, deterministic, and does not create runtime action chains.
- Prediction does not mutate runtime state or invent movement need deltas absent from execution.
- The external `DestinationDecider.Decide(...)` API and `NPCDecision` contract are unchanged.
- The action/selector/runner responsibility boundary remains intact.

## 16. Explicit Exclusions

Do not include:

- machine learning, reinforcement learning, or neural inference;
- a fixed satisfied-need threshold;
- repeated Eat/Drink/Sleep actions prequeued from one decision;
- full GOAP, A*, or unbounded route/action search;
- pathfinding-obstacle-aware travel distance;
- real movement-driven need consumption;
- runtime action-duration unification;
- Cook role behavior before its rules exist;
- combat targeting, target reservation, or chase/attack fixes;
- PatrolArea changes;
- unrelated stat, manager, pooling, UI, or progression refactors;
- public generic candidate-builder/plugin architecture.

If an excluded item becomes necessary, stop and request a scope revision rather than silently
expanding the implementation.

## 17. Known Risks

- Depth-2 utility can still be locally optimal rather than globally optimal. Increase depth only
  after a failing reproducible scenario; never default to unbounded search.
- Poorly scaled activity rewards can still cause permanent work or permanent consumption. Use the
  decision trace and scenario tests to tune units before adding more weights.
- Estimated action durations can drift from runtime hardcoded durations. Keep them explicitly
  labeled as estimates and schedule authoritative duration unification separately.
- `workCost` has role-dependent units: per Farming action for Farmer and per duty evaluation slice
  for Guard. Document this at the public method and call sites while preserving the required API.
- Recursive evaluation must not share mutable candidate state between branches.
- New serialized tuning values default silently in existing assets. Inspect
  `NPCDecisionTuning.asset` after Unity serialization and validate values in `OnValidate()`.
- The worktree already contains unrelated user changes. Implementation must stage and commit only
  files belonging to this plan unless the user explicitly expands the commit scope.
