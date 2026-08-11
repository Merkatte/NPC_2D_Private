# Code Evaluation Result

## Purpose

Lead-programmer review of the current NPC/stat/action skeleton after the `DefaultStatContext` work. Standards used:

- `PublicMD/CodeConvention.md`
- `PublicMD/ProjectStructure.md`
- `.codex/skills/reviewing-npc-work-code/references/review-workflow.md`

## Review Snapshot

- Date: 2026-08-11
- Scope:
  - All relevant C# scripts under `Assets/Scripts`
  - `DefaultStatContext` ScriptableObject definition and asset under `Assets/Data/ScriptableObject`
  - `Assets/Scenes/SampleScene.unity` serialized wiring for managers, selectors, stat assets, and destination data
  - `Assets/BehaviorGraph/CustomActionNode` checked; path is absent
- Sources:
  - `git status --short`
  - `git diff --stat`
  - Direct reads of manager, stat, selector, action, data, interface, and provider scripts
  - Targeted `rg` searches for `DefaultStatContext`, `_statInfo`, `_defaultStatContext`, `DataManager.instance`, `GetStat`, action cost fields, and scene GUID references
- Verification:
  - `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors

## Executive Summary

The data-backed stat direction is sound: `DefaultStatContext.CreateStat()` creates a fresh `NPCStat`, and `NPCStat` keeps stat invariants in the runtime state object. That matches the project rule that ScriptableObjects must not hold mutable runtime state.

The main runtime risk is now wiring and ownership drift around stat creation. `NPCManager.CreateNPC()` no longer uses its own `_defaultStatContext` or fallback method; it directly calls `DataManager.instance.GetStat()`. In the checked scene, `DataManager` has no `_statInfo` reference serialized, so the first NPC spawn through this path will null-reference.

There are also confirmed behavior mismatches in the need/action loop: `FarmingAction` applies need costs that do not match `DestinationDecider` predictions, and `SleepAction` reduces fatigue using hunger instead of fatigue.

## Improvements Since Previous Review

- `NPCStat` now initializes and clamps move speed, health, and need current/max values in its constructor.
- `DefaultStatContext` now exists as a concrete ScriptableObject asset source and creates per-NPC runtime `NPCStat` instances.
- `DestinationDB` is now wired in `SampleScene` and contains `eatPlace`, `sleepPlace`, `drinkPlace`, and `farmPlace`.
- The command-line C# build currently passes.

## Findings By Severity

### Critical

None found.

### High

#### Finding 1: NPC spawn path can null-reference because it depends on an unwired global `DataManager`

- Severity: High
- File: `Assets/Scripts/Manager/NPCManager.cs:34`, `Assets/Scripts/Manager/DataManager.cs:8`, `Assets/Scripts/Manager/DataManager.cs:28`, `Assets/Scripts/Manager/DataManager.cs:30`, `Assets/Scenes/SampleScene.unity:958`, `Assets/Scenes/SampleScene.unity:970`
- Evidence:
  - `NPCManager.CreateNPC()` calls `DataManager.instance.GetStat()`.
  - `DataManager.GetStat()` directly calls `_statInfo.CreateStat()`.
  - The `DataManager` scene block serializes `_costInfos`, but no `_statInfo` field is present.
  - `rg` found the `DefaultStatContext` asset GUID only in the asset itself, not in `SampleScene`.
- Why it matters:
  - In the current scene, calling `CreateNPC()` reaches an unassigned `_statInfo` and throws before a worker can initialize.
  - This also bypasses `NPCManager.CreateStat()`, so the fallback path in `NPCManager` is currently dead code.
- Recommended fix:
  - Choose one stat creation owner.
  - If `DataManager` owns stat creation, wire `_statInfo` in the scene and make `NPCManager` depend on an explicit serialized/service reference with validation.
  - If `NPCManager` owns spawn composition, call `CreateStat()` and remove the global `DataManager.instance` dependency from the spawn path.

### Medium

#### Finding 2: `NPCManager._defaultStatContext` and `CreateStat()` are now unused dead composition code

- Severity: Medium
- File: `Assets/Scripts/Manager/NPCManager.cs:9`, `Assets/Scripts/Manager/NPCManager.cs:38`
- Evidence:
  - `_defaultStatContext` is serialized but never read by `CreateNPC()`.
  - `CreateStat()` is private and has no callers after `CreateNPC()` was changed to `DataManager.instance.GetStat()`.
- Why it matters:
  - The Inspector now exposes a stat asset slot that cannot affect spawned NPCs.
  - This contradicts the progress note that `NPCManager._defaultStatContext` should be connected.
- Recommended fix:
  - Remove `_defaultStatContext` and `CreateStat()` from `NPCManager` if `DataManager` is the intended source.
  - Or restore `CreateNPC()` to use `CreateStat()` and keep `DataManager` out of NPC spawning.

#### Finding 3: Work need costs differ between planning and execution

- Severity: Medium
- File: `Assets/Scripts/System/Action/FarmingAction.cs:32`, `Assets/Scripts/System/Action/FarmingAction.cs:33`, `Assets/Scripts/System/Action/FarmingAction.cs:34`, `Assets/Scripts/System/Lib/DestinationDecider.cs:20`, `Assets/Scripts/System/Lib/DestinationDecider.cs:21`, `Assets/Scripts/System/Lib/DestinationDecider.cs:22`
- Evidence:
  - `FarmingAction` applies fatigue/hunger/thirst deltas of `8f/8f/8f`.
  - `DestinationDecider` predicts work deltas of `8f/5f/7f`.
- Why it matters:
  - The selector can enqueue a work count based on a lower predicted hunger/thirst cost than the action actually applies.
  - Need-based behavior will drift from the plan once the loop runs.
- Recommended fix:
  - Put work need costs in one source, preferably a data asset/provider, and have both the decider and action read the same values.

#### Finding 4: `SleepAction` restores fatigue using hunger

- Severity: Medium
- File: `Assets/Scripts/System/Action/SleepAction.cs:33`
- Evidence:
  - `SleepAction.UpdateCompletion()` calls `_stat.ChangeFatigue(-_stat.GetHunger)`.
  - `EatAction` and `DrinkAction` use their matching need values (`GetHunger`, `GetThirst`) for recovery.
- Why it matters:
  - A hungry but rested NPC can lose fatigue incorrectly.
  - A fatigued NPC with low hunger may barely recover from sleep.
- Recommended fix:
  - Change the recovery expression to use fatigue, e.g. `_stat.ChangeFatigue(-_stat.GetFatigue)`, or a data-driven sleep recovery amount.

### Low

#### Finding 5: `DefaultStatContext` script definition lives under the data asset tree

- Severity: Low
- File: `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs:3`
- Evidence:
  - `CodeConvention.md` says ScriptableObject asset instances belong under `Assets/Data/<Domain>`, while class definitions should stay under the owning script domain.
  - The `.asset` and the `.cs` definition both live under `Assets/Data/ScriptableObject`.
- Recommended fix:
  - Keep `DefaultStatContext.asset` under `Assets/Data`, but move the class definition to the NPC/stat script domain.

#### Finding 6: Default stat values are duplicated across constructor defaults, asset defaults, and fallback code

- Severity: Low
- File: `Assets/Scripts/System/Actor/NPCStat.cs:5`, `Assets/Scripts/System/Actor/NPCStat.cs:6`, `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs:6`, `Assets/Scripts/Manager/NPCManager.cs:43`
- Evidence:
  - Need defaults appear in the four-argument `NPCStat` constructor.
  - Matching defaults appear in `DefaultStatContext`.
  - Another fallback default appears in `NPCManager.CreateStat()`.
- Recommended fix:
  - Once the stat asset is mandatory, remove silent code fallbacks.
  - If fallback is required, centralize it in one named factory/default definition.

#### Finding 7: Existing `NPCStat.ChangeMoveSpeed()` still cannot increase speed

- Severity: Low
- File: `Assets/Scripts/System/Actor/NPCStat.cs:80`, `Assets/Scripts/System/Actor/NPCStat.cs:82`
- Evidence:
  - The method clamps `_moveSpeed + val` with `_moveSpeed` as the maximum.
- Recommended fix:
  - Add a separate max/current speed model, or rename the method if speed increases are intentionally unsupported.

#### Finding 8: Several touched scripts have convention cleanup issues

- Severity: Low
- File: `Assets/Scripts/Manager/NPCManager.cs:2`, `Assets/Scripts/Actor/WorkerNPC.cs:4`, `Assets/Scripts/System/Action/EatAction.cs:2`, `Assets/Scripts/Manager/DataManager.cs:13`
- Evidence:
  - `Unity.VisualScripting`, `UnityEngine.UIElements`, and `UnityEngine.PlayerLoop` are imported but not used.
  - `DataManager.instance` is public static state, which conflicts with the architecture direction of explicit references over hidden global lookup.
- Recommended fix:
  - Remove unused usings.
  - Replace global access with serialized references or explicit initialization before more systems consume `IDataManager`.

## Findings By File

### `Assets/Scripts/Manager/NPCManager.cs`

- High: `CreateNPC()` depends on `DataManager.instance.GetStat()` without validating `DataManager.instance`.
- Medium: `_defaultStatContext` and `CreateStat()` are currently unused.
- Low: unused `Unity.VisualScripting` import and large commented-out switch block.

### `Assets/Scripts/Manager/DataManager.cs`

- High: `GetStat()` dereferences `_statInfo` with no scene wiring and no null guard.
- Low: `public static IDataManager instance` introduces hidden global dependency.
- Low: `_itemInfos` is initialized but unused in the reviewed code.

### `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs`

- Low: class definition is under the data asset folder rather than the owning script domain.
- Positive: `CreateStat()` creates a new runtime object and does not mutate the asset.

### `Assets/Scripts/System/Actor/NPCStat.cs`

- Low: `ChangeMoveSpeed()` still cannot increase speed.
- Low: constructor defaults duplicate asset defaults.
- Positive: constructor clamping belongs in `NPCStat` and is implemented there.

### `Assets/Scripts/System/Action/FarmingAction.cs`

- Medium: runtime work need deltas do not match the decider's predicted work deltas.
- Low: `_workingTime` and need costs are hardcoded while action cost assets already exist.

### `Assets/Scripts/System/Action/SleepAction.cs`

- Medium: fatigue recovery uses hunger instead of fatigue.

### `Assets/Scenes/SampleScene.unity`

- High: `DataManager` has no serialized `_statInfo` reference.
- Medium: `NPCManager` has no serialized `_defaultStatContext` reference, and that field is currently unused anyway.
- Positive: `FarmerActionSelector._destinationDB` is assigned, and `DestinationDB` has supply/farm destinations serialized.

## Cross-Cutting Findings

- Stat runtime ownership is mostly correct: `NPCStat` remains plain state and `DefaultStatContext` remains definition data.
- Spawn composition is now split ambiguously between `NPCManager` and `DataManager`; this should be resolved before adding more NPC creation paths.
- Need behavior is not yet single-source-of-truth: the decider, actions, action cost assets, and stat defaults all carry related numbers independently.
- The project is still in a skeleton phase, but now that actions can run through `WorkerNPC`, confirmed need/action bugs should be fixed before tuning.

## Positive Notes

- The project builds with 0 warnings and 0 errors.
- `DestinationDB` is now scene-wired for the farmer selector.
- `DefaultStatContext` does not store runtime state and correctly creates a separate `NPCStat`.
- Selector/action responsibility remains mostly intact: the decider chooses, the selector builds queues, and actions mutate stats.

## Recommended Next Actions

1. Fix the spawn stat source: either wire and validate `DataManager._statInfo`, or restore `NPCManager.CreateStat()` usage and remove `DataManager.instance` from spawn.
2. Remove the unused `NPCManager._defaultStatContext` path or make it the actual spawn path.
3. Correct `SleepAction` to recover fatigue from fatigue, not hunger.
4. Unify farming need costs between `DestinationDecider`, `FarmingAction`, and the existing action cost data.
5. Move `DefaultStatContext.cs` out of `Assets/Data` into the owning NPC/stat script domain.
6. Remove unused imports, dead commented code, and duplicated fallback stat constants.

## Final Verdict

Build passes, but the current runtime spawn path is not safe in the checked scene. The `DefaultStatContext` model itself is architecturally acceptable; the main required fix is deciding who owns stat creation and wiring that dependency explicitly. After that, fix the need-cost mismatch and `SleepAction` recovery bug before relying on autonomous need-based NPC behavior.
