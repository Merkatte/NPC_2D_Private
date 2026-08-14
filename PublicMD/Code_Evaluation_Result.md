# Code Evaluation Result

## Purpose

Read-only review of the DestinationDecider Phase A–D refactor, prioritizing architecture, correctness, lifecycle, serialized-reference safety, and maintainability. No fixes were implemented.

## Review Snapshot

- Date: 2026-08-14
- Scope:
  - All listed changed runtime/data files
  - Direct dependencies including `WorkerNPC`, `BaseNPCActionSelector`, `NPCStat`, `NPCComponent`, `InteractableManager`, `IAction`, `MoveAction`, `SleepAction`, `WorkerPool`, and data-loading code
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Prefab/NPCGirl.prefab`
  - New ScriptableObject assets and test-only CSV
- Standards:
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `PublicMD/DestinationDecider_Refactor_Plan.md`
  - reviewing-npc-work-code audit workflow
- Verification:
  - `dotnet build Assembly-CSharp.csproj --no-restore`: 0 warnings, 0 errors
  - `git diff --check`: no whitespace errors
  - No `UnityEngine.Random` use remains under `Assets/Scripts` or `Assets/Data`
  - New C# files are included in `Assembly-CSharp.csproj`
  - No `.asmdef` files or automated Unity test declarations were found
  - No Unity Editor Play Mode or manual smoke test was run

`PublicMD/ARCHITECTURE.md` is mojibaked in the repository. Readable conclusions from that document were therefore corroborated against `ProjectStructure.md`, `CodeConvention.md`, the refactor plan, and repository code.

## Executive Summary

The refactor’s main responsibility split is sound. `DestinationDecider` owns candidate prediction and selection, providers own concrete option availability and execution, the selector translates one decision into actions, and actions apply effects. No high-severity architecture or dependency-direction violation was found in the new decision design.

The previous prediction/execution drift was substantially improved: concrete item IDs are carried through the decision, random substitution was removed, farming costs now share the same source data, full `StatEffect` values are simulated, work batches are iteratively bounded, and pooled action state is cleared more safely.

The implementation is not feature-complete. `SampleScene` does not reference the new tuning asset and contains no `InteractableManager`, so the decision feature cannot currently perform its intended runtime flow. The existing action lifecycle contract also remains unable to represent failure or cancellation safely.

Final verdict: code-level direction approved, runtime acceptance rejected until the two scene blockers are wired and smoke-tested. Automated decision tests remain required before treating the scoring logic as regression-safe.

## Priority Assessment

1. Architecture and responsibility placement: No high-severity issue found. The selector still contains one repeat-count policy leak described below.
2. Correctness, lifecycle, cancellation, and regression risk: Issues found, including the pre-existing action lifecycle contract and untested selection edge cases.
3. Unity scene, prefab, component, and serialized-reference safety: Two confirmed High-severity blockers found.
4. Convention, maintainability, dead code, and magic values: Low-severity issues found; no major convention regression.

## Improvements Since Previous Review

- `FarmingAction.Clear()` now resets `_currentWorkingTime`.
- The misspelled farming thirst field was migrated with `FormerlySerializedAs`, and the asset now stores the corrected field name.
- Farming prediction uses the same asset-derived deltas as execution instead of independent constants.
- Work projection applies its cost iteratively for the full bounded batch.
- Eat and Drink carry and execute a concrete item request; random item replacement was removed.
- `Pub` now safely returns no options/failure when item data is unavailable instead of indexing a null or missing category directly.
- `DefaultAction.Clear()` clears `ActionContext`, preventing pooled request/provider leakage.
- `DataManager` builds its cost dictionary in `Awake`, removing the identified `Start`-order race.
- `DestinationDB` now lazily initializes and deduplicates registered keys.
- Explicit Idle decisions prevent the normal no-decision path from allocating an empty queue every frame.
- Existing serialized enum values were preserved by appending `Idle`.

## Findings By Severity

### Critical

None found.

### High

#### H-01: The new decision tuning asset is not wired into `SampleScene`

- Severity: High
- Category: Unity serialized-reference safety
- Location:
  - `Assets/Scripts/System/Actor/FarmerActionSelector.cs:7`
  - `Assets/Scripts/System/Actor/FarmerActionSelector.cs:49-53`
  - `Assets/Scenes/SampleScene.unity:714-719`
  - `Assets/Data/ScriptableObject/NPCDecisionTuning.asset`
- Evidence:
  - `_decisionTuning` is a required serialized field.
  - The selector explicitly falls back to Idle when it is null.
  - The selector YAML ends after `_destinationDB`; no `_decisionTuning` entry exists.
  - Repository search found no scene or prefab consumer of the tuning asset GUID.
- Description: Every request in the current scene degrades to Idle, so none of the new scoring, item selection, or work batching runs.
- Recommended fix: Assign `NPCDecisionTuning.asset` to the scene selector and add `OnValidate` or startup validation that reports a missing required reference before NPC creation.
- Impact if unfixed: The refactor builds successfully but is completely disabled at runtime.

#### H-02: Interaction providers are still never initialized in `SampleScene`

- Severity: High
- Category: Unity scene composition
- Location:
  - `Assets/Scripts/Manager/InteractableManager.cs:5-18`
  - `Assets/Scripts/Actor/Pub.cs:15-25`
  - `Assets/Scenes/SampleScene.unity:661-672`
  - `Assets/Scenes/SampleScene.unity:1223-1235`
  - `Assets/Scenes/SampleScene.unity:1364-1375`
- Evidence:
  - `BaseInteractable.Init()` is called only by `InteractableManager.Start()`.
  - Two `Pub` components are registered as destination providers.
  - The scene contains no component using the `InteractableManager` script GUID.
  - With `_itemInfos == null`, `Pub.AppendOptions()` returns without adding candidates and `TryInteraction()` returns false.
- Description: The new defensive behavior prevents the previous null-reference crash, but it also means no Eat or Drink candidates exist.
- Recommended fix: Add `InteractableManager` to the scene, assign `DataManager`, and include both providers. Validate the references during scene initialization.
- Impact if unfixed: After tuning is wired, workers can perform Work but cannot discover supplies. Once work becomes unsafe or needs become critical, they fall back to Idle indefinitely.

#### H-03: The action lifecycle still cannot represent failure or cancellation safely

- Severity: High
- Category: Lifecycle and cancellation
- Location:
  - `Assets/Scripts/System/Action/DefaultAction.cs:14-23`
  - `Assets/Scripts/System/Action/DefaultAction.cs:61-65`
  - `Assets/Scripts/System/Actor/FarmerActionSelector.cs:69-74`
  - `Assets/Scripts/Actor/WorkerNPC.cs:22-45`
- Evidence:
  - `Init()` immediately invokes `Start()`, so every queued action is marked running during queue construction.
  - `WorkerNPC` understands only `CheckComplete()`.
  - `Stop()` leaves `_isComplete == false` while also preventing further ticks.
  - Provider rejection is represented as ordinary completion rather than an explicit failed result.
- Description: The single-decision queue improves replanning frequency but does not close the underlying lifecycle contract.
- Recommended fix: Make `Init` data-only, start actions when dequeued, and expose explicit Ready/Running/Succeeded/Failed/Cancelled states. Ensure cancellation returns both active and queued actions through one cleanup path.
- Impact if unfixed: A stopped action can permanently stall a worker, and future reservations, animations, or inventory failures cannot be handled reliably.

### Medium

#### M-01: The selector can still force an invalid Work decision to execute once

- Severity: Medium
- Category: Responsibility boundary and regression safety
- Location:
  - `Assets/Data/Struct/NPCDecision.cs:16-22`
  - `Assets/Scripts/System/Actor/FarmerActionSelector.cs:67-74`
- Evidence:
  - `NPCDecision` accepts any repeat count without validation.
  - The selector applies `Mathf.Max(1, decision.RepeatCount)` to every intent.
  - The current decider does not emit zero-repeat Work, but the selector independently restores the exact forced-single-action behavior the refactor intended to eliminate.
- Description: Work-batch validity belongs to the decider/decision contract, not queue conversion.
- Recommended fix: Preserve the supplied Work repeat count and reject or convert invalid Work decisions to Idle. Normalize one-shot intents when constructing the decision instead of applying a universal minimum in the selector.
- Impact if unfixed: A malformed, default, or future externally constructed Work decision with zero repeats silently becomes one Farming action.

#### M-02: Noncritical selection has no Idle utility baseline

- Severity: Medium
- Category: Decision correctness
- Location:
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:90-105`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:283-299`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:465-473`
  - `Assets/TestOnly/DecisionSmokeItemData.csv:6`
- Evidence:
  - `TryPickBestSupply` selects the highest-ranked candidate regardless of whether its utility is negative.
  - If Work is unavailable, `Decide` returns that supply candidate unconditionally.
  - Utility can be negative because of travel and health penalties.
  - The smoke CSV includes an item with `healthDelta = -20`.
- Description: Health loss is penalized, but a harmful option still wins when every available option is worse than doing nothing.
- Recommended fix: Compare noncritical candidates against an explicit Idle utility baseline, normally zero. Keep critical fallback handling separate so a lifesaving option may still be selected when appropriate.
- Impact if unfixed: Workers can travel to consume a net-harmful item or repeatedly perform unnecessary supply actions when Work is unavailable.

#### M-03: Action duration is absent from tuning and utility calculation

- Severity: Medium
- Category: Plan conformance and tuning correctness
- Location:
  - `Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs:19-38`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:31-42`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:465-473`
  - `Assets/Scripts/System/Action/FarmingAction.cs:5`
  - `Assets/Scripts/System/Action/EatAction.cs:5`
  - `Assets/Scripts/System/Action/DrinkAction.cs:5`
- Evidence:
  - The tuning asset has no action-time weight.
  - `Candidate` stores travel time but no execution duration.
  - Utility subtracts travel cost only.
  - Actual actions have materially different durations: Farming 10 seconds, Eat 2 seconds, Drink 1 second.
  - Phase B and the utility model in the approved plan include action-time weighting.
- Description: The implemented utility model is incomplete relative to the approved Phase A–D plan.
- Recommended fix: Establish an authoritative duration source shared by prediction and execution, then add action duration and its tuning weight. If duration was intentionally removed from scope, update the governing plan explicitly.
- Impact if unfixed: Options with equal need effects and travel cost are ranked without considering their different time commitments, biasing utility comparisons.

#### M-04: Core decision and interaction behavior has no automated regression coverage

- Severity: Medium
- Category: Validation
- Location:
  - `Assets/Scripts/System/Lib/DestinationDecider.cs`
  - `Assets/TestOnly/TestNPCSpawnWindow.cs`
  - `Assets/TestOnly/DecisionSmokeItemData.csv`
- Evidence:
  - No `.asmdef` files exist.
  - No `[Test]`, `[UnityTest]`, NUnit assertion, or equivalent automated Unity test declaration was found.
  - The added CSV has not been exercised.
  - Only command-line compilation has been completed.
- Description: The largest changed file contains threshold, normalization, clamp, iteration, critical-tier, and tie-break logic with no executable specification.
- Recommended fix: Add table-driven Edit Mode tests for the required plan cases, interaction request identity, and pooled context clearing. Add a minimal Play Mode scene-wiring smoke test.
- Impact if unfixed: Sign errors, threshold boundary regressions, forced Work reintroduction, and tie-break changes can all pass compilation unnoticed.

### Low

#### L-01: Eat and Drink do not validate that the request type matches the action

- Severity: Low
- Category: Contract robustness
- Location:
  - `Assets/Scripts/System/Action/EatAction.cs:9-17`
  - `Assets/Scripts/System/Action/DrinkAction.cs:11-19`
- Evidence:
  - Each action verifies that a request exists and that the provider supports the action.
  - Neither verifies `Request.Value.Type == GetMyActionType()`.
  - Execution passes the request type directly to the provider.
- Description: The current selector constructs consistent pairs, but malformed contexts can make an Eat action execute a Drink request or vice versa.
- Recommended fix: Validate request/action type equality during `Start()` and fail cleanly when it does not match.
- Impact if unfixed: Future callers or tests can violate the action contract without an immediate diagnostic.

#### L-02: The new Idle duration is an unexplained mutable magic value

- Severity: Low
- Category: Maintainability and magic values
- Location: `Assets/Scripts/System/Action/IdleAction.cs:5-6`
- Evidence: `_idleTime` is hardcoded to `1f`, is neither constant nor serialized, and is labeled only `//Temp`.
- Description: The value is policy because it controls how frequently an idle worker reevaluates its situation.
- Recommended fix: Make the backoff a named constant for a fixed prototype rule or place it in decision tuning if designers should control it.
- Impact if unfixed: Replanning frequency cannot be tuned consistently and the purpose of the value remains unclear.

## Findings By File

### `DestinationDecider.cs`

- Architecture is appropriately isolated from action creation and stat mutation.
- Medium: no Idle utility baseline.
- Medium: action duration is omitted.
- Medium: complex policy remains untested.

### `FarmerActionSelector.cs`

- Correctly limits itself mostly to decision-to-queue translation.
- High: depends on an unwired tuning reference.
- Medium: `Mathf.Max(1, RepeatCount)` leaks policy back into queue construction.

### `Pub.cs` / `IInteractionProvider.cs`

- Concrete option enumeration and exact request execution are well placed.
- Defensive missing-data behavior is improved.
- High scene blocker: providers are never initialized in `SampleScene`.

### `DefaultAction.cs` / Eat / Drink / Farming / Idle

- Context clearing and Farming timer reset are correct improvements.
- High: failure/cancellation lifecycle remains incomplete.
- Low: request/action identity and Idle timing contracts are weak.

### `DestinationDB.cs`

- Lazy initialization and deduplicated key enumeration are appropriate.
- No issue found in the changed initialization behavior.

### `NPCDecisionTuning.cs`

- Threshold and batch cross-field validation is appropriate.
- Serialized field style follows convention.
- Medium: action-time tuning required by the plan is absent.

### `SampleScene.unity`

- Existing `DestinationDB`, `DataManager`, pool, and selector references remain intact.
- High: `_decisionTuning` is unassigned.
- High: `InteractableManager` is absent.

### `NPCGirl.prefab`

- `WorkerNPC._component`, the movement transform, GameObject, and sprite renderer remain assigned.
- No refactor-related prefab issue found.

## Cross-Cutting Findings

- The refactor has successfully moved decision policy into the decider, but its invariants are not yet closed at DTO and selector boundaries.
- Build success is not meaningful evidence for scoring correctness or Unity scene composition.
- Defensive null handling now prevents the previous interaction crash, but safe failure can still conceal incomplete scene setup unless composition is validated explicitly.
- The current action lifecycle is the largest remaining architectural debt directly affecting reliable replanning, cancellation, and provider failure handling.
- `ProjectStructure.md` remains stale, but Phase E documentation work was explicitly excluded from this scope.

## Positive Observations

- Concrete item selection is deterministic and execution uses the selected `ItemId`.
- Complete `StatEffect` prediction mirrors `NPCStat.ApplyStatEffect` clamp behavior.
- Health is included in utility risk and mood is explicitly excluded until the stat exists.
- Work counts are simulated iteratively and capped by tuning.
- Work is gated below `MinimumWorkBatch` in the decider.
- Critical evaluation uses the need set captured at judgment start, avoiding the described side-effect deadlock.
- Tie-breaking is stable across utility, travel time, item ID, action type, and building type.
- Farming cost prediction and execution now derive from the same asset.
- Enum additions preserve existing serialized numeric values.
- Per-decision reusable buffers avoid repeated candidate-list allocation.
- No repeated scene searches, LINQ, or randomness were introduced into decision or action ticks.
- Command-line compilation remains clean.

## Recommended Next Actions

1. Wire `NPCDecisionTuning.asset` into `FarmerActionSelector`.
2. Add and configure `InteractableManager` with both providers and `DataManager`.
3. Run the required manual Unity smoke cases before declaring feature completion.
4. Remove the selector’s forced minimum repeat and validate Work decision invariants.
5. Add an Idle utility baseline for noncritical selection.
6. Resolve or explicitly rescope action-time utility.
7. Add table-driven Edit Mode tests and a scene-wiring Play Mode smoke test.
8. Address the pre-existing action result/cancellation lifecycle before adding reservations, animation, or inventory consumption.

## Final Verdict

The Phase A–D refactor is structurally strong and resolves several important defects from the previous implementation. No Critical issue and no high-severity responsibility-placement issue were found.

It is not ready for runtime acceptance. The scene currently disables the decision system through a null tuning reference, and providers cannot expose supply options because their initializer is absent. After those blockers are fixed, the decision behavior still requires manual smoke validation and automated coverage, with the forced-repeat and negative-utility edge cases addressed before the refactor should be considered complete.