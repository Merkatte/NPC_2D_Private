# Code Evaluation Result

## Purpose

Review the Farm Production Gauge vertical slice against `PublicMD/ARCHITECTURE.md`, `PublicMD/ProjectStructure.md`, `PublicMD/CodeConvention.md`, and `PublicMD/Farm_Production_Gauge_Plan.md`. This was a read-only audit; no fixes or report-file edits were performed.

## Review Snapshot

- Date: 2026-08-21
- Scope:
  - All 10 new farm, inventory, random-source, definition, and TestOnly scripts
  - Modified `ActionContext`, `DestinationDB`, `FarmerActionSelector`, and `FarmingAction`
  - Direct dependency surfaces: `DefaultAction`, `WorkerNPC`, `BaseNPCActionSelector`, `ActionPool`, `DestinationDecider`, `NPCManager`, `NPCComponent`, `NPCStat`, action result contracts, and current scene YAML
  - New script/folder `.meta` files and `Assembly-CSharp.csproj` entries
- Excluded:
  - Unrelated Guard/Enemy/scene/recovery-file worktree changes, except the farm wiring present in `GuardTest.unity`
  - The external `.claude` plan, because the requested audit workflow explicitly prohibits reading `.claude`
- Repository state:
  - Farm files are uncommitted.
  - Unrelated user-owned changes were detected and left untouched.
- Verification:
  - `git status`, `git diff`, `git diff --check`, targeted `rg`, source inspection, metadata inspection, and scene/prefab YAML inspection completed.
  - All 10 new scripts and both new folders have `.meta` files.
  - No duplicate GUID was found among `Assets/**/*.meta`.
  - All 10 scripts are included in `Assembly-CSharp.csproj`.
  - Existing `Assembly-CSharp.dll` is newer than the reviewed source files, corroborating a Unity compile after the changes.
  - The reported `dotnet build --no-restore` result was not independently rerun because the sandbox is read-only and a build writes artifacts.
  - No direct `UnityEngine.Random` or `Random.Range` use was found in the new farm slice.
  - No repeated component or scene lookup occurs inside `FarmingAction.Tick()`.
  - The documented 100/10/10 state-machine scenario matches the code by inspection.
  - Unity Play Mode, TestOnly-window interaction, and real Farmer end-to-end execution remain unverified.

## Executive Summary

The core design is sound. Runtime gauge state belongs to `FarmWorkSite`, immutable tuning belongs to `FarmProductionDefinition`, quantities belong to `WarehouseInventory`, and `FarmingAction` invokes a narrow capability without learning the concrete facility implementation. Growth and harvesting transitions are correct, current warehouse mutation occurs before gauge reduction, pooled action state is cleared, and the selector—not `WorkerNPC`—continues to own capability wiring.

The repository is not yet runtime-ready, however. Neither current scene wires `FarmWorkSite`, `WarehouseInventory`, `SeededRandomSource`, or a `FarmProductionDefinition` asset. Because the modified selector now requires the provider, every Work decision in the current scenes is converted to Idle. This is a confirmed regression in the checked-in scene state, not only a missing validation step.

Additional medium risks concern the partial-acceptance inventory contract, Unity destroyed/disabled component lifetime, missing stat validation before farm mutation, and duplicated farming-duration sources.

Final verdict: **changes required before runtime acceptance**. The architecture is conditionally acceptable, but the vertical slice is not operational in the current repository scenes.

## Review Priority Assessment

1. Architecture and responsibility placement:
   - No Critical or High responsibility-boundary violation found.
   - M-01 and Low configuration/placement findings remain.
2. Correctness, lifecycle, cancellation, and regression:
   - No Critical issue found.
   - M-01 through M-04 apply.
   - Cancellation itself is safe: incomplete actions do not call the provider.
3. Unity scene, prefab, component, and serialized-reference safety:
   - No Critical issue found.
   - H-01 and M-02 apply.
4. Code convention, maintainability, dead code, and magic values:
   - No Critical or High convention issue found.
   - M-04 and L-02/L-03 apply.
   - No new dead code or unused import was identified in the farm slice.

## Improvements Since Previous Review

- `FarmingAction` is no longer a log-only action; successful completion now produces an observable domain result.
- Production state is correctly kept out of `WorkerNPC`, selectors, and ScriptableObject runtime data.
- The trailing optional `ActionContext` parameter preserves existing constructor call sites.
- The provider is resolved during destination initialization/selection and cached by the action; no lookup was added to its tick.
- Global Unity random state is avoided.
- The TestOnly tool observes and invokes public feature APIs without taking ownership of gameplay state.
- The previous IMP-027 Guard/utility findings were outside this audit and should not be considered resolved by this report.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — Current gameplay scenes cannot execute farming

- Severity: High
- Category: Unity serialized-reference safety / runtime regression
- Location:
  - `Assets/Scenes/SampleScene.unity:871`
  - `Assets/Scenes/SampleScene.unity:1286`
  - `Assets/Scenes/GuardTest.unity:1208`
  - `Assets/Scenes/GuardTest.unity:1625`
  - `Assets/Scripts/System/Actor/FarmerActionSelector.cs:58-72`
  - `Assets/Scripts/System/Lib/DestinationDB.cs:101-102`
- Evidence:
  - Both scenes register a Farm destination, but the referenced farm GameObject contains only `Transform` and `SpriteRenderer`.
  - The GUIDs for `FarmWorkSite`, `WarehouseInventory`, `SeededRandomSource`, and `TestFarmProductionWindow` occur in no scene or prefab.
  - No `FarmProductionDefinition` asset instance exists.
  - `DestinationDecider` can still choose Work from the Farm position, after which `FarmerActionSelector` fails provider lookup and replaces the decision with Idle.
- Description:
  - The C# slice exists, but the current playable composition cannot instantiate its dependency graph. Existing Farmer work is therefore disabled rather than upgraded.
- Recommended fix:
  - Create a valid `FarmProductionDefinition` asset.
  - Wire `FarmWorkSite`, an `IInventory`-implementing warehouse component, and `SeededRandomSource` in the gameplay scene.
  - Ensure the Farm `DestinationObject` is the GameObject carrying `FarmWorkSite`.
  - Wire the TestOnly window temporarily if it will be used for validation.
  - Save and inspect the resulting YAML, then run the documented Play Mode scenarios.
- Impact if unfixed:
  - Farmers repeatedly choose Work and receive Idle instead.
  - No gauge progress, phase transition, yield, or warehouse deposit can occur in the current scenes.
  - The feature cannot meet its runtime Definition of Done despite compiling.

### Medium

#### M-01 — Partial inventory acceptance cannot be rolled back atomically

- Severity: Medium
- Category: Architecture / transaction correctness
- Location:
  - `Assets/Scripts/Interface/IInventory.cs:1-5`
  - `Assets/Scripts/System/Farming/FarmWorkSite.cs:64-70`
- Evidence:
  - `TryAdd` exposes `acceptedQuantity`, which permits an implementation to accept less than requested.
  - `FarmWorkSite` calls `TryAdd` first, then treats `acceptedQuantity != yield` as total failure.
  - `IInventory` exposes no removal or rollback operation.
- Description:
  - A future capacity-limited inventory can validly mutate by a partial amount before returning. `FarmWorkSite` would then preserve the gauge and report failure while the partial output remains deposited.
  - The current unlimited `WarehouseInventory` always accepts all-or-nothing, so the defect is dormant with that concrete implementation.
- Recommended fix:
  - Define and enforce an exact-add contract for production, such as `TryAddExact`, where failure guarantees zero mutation; or redesign `TryApplyWork` to treat accepted partial quantity coherently.
  - Document the mutation invariant directly on `IInventory`.
- Impact if unfixed:
  - A capacity-aware inventory can create partial or duplicated output across retries while the farm gauge remains unchanged.

#### M-02 — Provider validity becomes stale after lookup

- Severity: Medium
- Category: Unity lifecycle / serialized-reference safety
- Location:
  - `Assets/Scripts/System/Lib/DestinationDB.cs:56-68`
  - `Assets/Data/Struct/ActionContext.cs:12`
  - `Assets/Scripts/System/Action/FarmingAction.cs:22-25`
  - `Assets/Scripts/System/Farming/FarmWorkSite.cs:20`
- Evidence:
  - `DestinationDB` correctly uses concrete `FarmWorkSite` truthiness at lookup.
  - The provider is then stored as `IFarmWorkProvider`, for which `_farmWorkProvider == null` does not apply Unity fake-null semantics.
  - `FarmWorkSite.CanApplyWork` returns only the `Awake`-cached `_isOperational`; it does not check `this`, `isActiveAndEnabled`, or current dependency validity.
- Description:
  - Destruction or disabling between queue creation, movement, action start, and completion is not detected reliably.
- Recommended fix:
  - Make `CanApplyWork` dynamically include Unity lifetime/enablement and required-reference checks, and repeat that guard inside `TryApplyWork`.
  - If facilities become destructible, expose an explicit Unity owner/handle or cancellation signal rather than relying on an interface null check.
- Impact if unfixed:
  - An action can call into a disabled or destroyed component wrapper, causing ghost managed-state mutation, a missing-reference failure, or success against a facility that no longer exists.

#### M-03 — Missing `NPCStat` can mutate the farm before throwing

- Severity: Medium
- Category: Correctness / safe failure
- Location:
  - `Assets/Scripts/System/Action/FarmingAction.cs:16-26`
  - `Assets/Scripts/System/Action/FarmingAction.cs:54-75`
  - `Assets/Scripts/System/Action/DefaultAction.cs:28-38`
- Evidence:
  - `DefaultAction.Start()` validates only `NPCComponent`.
  - `FarmingAction.Start()` validates only the provider.
  - On completion, the provider mutates farm/inventory before `stat.ChangeFatigue`, `ChangeHunger`, and `ChangeThirst` are called.
  - `ActionContext.Stat` can be null.
- Description:
  - An invalid context with a valid component, cost, and provider applies farm work and then throws `NullReferenceException` while applying the NPC cost.
  - Normal `NPCManager` creation prevents this path, but it violates the required action-level safe-failure contract.
- Recommended fix:
  - Validate `Stat`, `FarmingActionCost`, and provider during `Start()`, before timing begins.
  - Keep defensive completion checks before calling `TryApplyWork`.
- Impact if unfixed:
  - Misconfigured tests or alternative runners can partially mutate world state and leave the action queue in an exception path.

#### M-04 — Runtime and decision layers use separate farming-duration values

- Severity: Medium
- Category: Maintainability / magic values / prediction correctness
- Location:
  - `Assets/Scripts/System/Action/FarmingAction.cs:8`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:355`
  - `Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs:46`
  - `Assets/Data/ScriptableObject/NPCDecisionTuning.asset:33`
- Evidence:
  - Runtime duration is hardcoded as `_workingTime = 3f`.
  - Utility scoring reads the separately serialized `EstimatedFarmingSeconds`, currently also 3.
- Description:
  - The two values agree only by convention. Changing the tuning asset changes planning cost without changing actual action duration.
- Recommended fix:
  - Establish one authoritative farming-duration definition consumed by both action execution and decision prediction.
- Impact if unfixed:
  - Balance edits can make Work utility, travel comparisons, and future-action scoring diverge from actual gameplay timing.

### Low

#### L-01 — Inclusive random range overflows at `int.MaxValue`

- Severity: Low
- Category: Correctness / edge-case validation
- Location:
  - `Assets/Scripts/System/Lib/SeededRandomSource.cs:9-16`
  - `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs:23-24`
- Evidence:
  - `NextInclusive` computes `maximum + 1`.
  - `FarmProductionDefinition` considers `MaximumYield == int.MaxValue` valid.
- Description:
  - The addition overflows before it reaches `System.Random.Next`.
- Recommended fix:
  - Implement an overflow-safe inclusive range or explicitly reject `int.MaxValue`.
- Impact if unfixed:
  - An otherwise accepted extreme definition causes an exception during harvesting.

#### L-02 — Duplicate destination keys can pair a stale farm provider with a different destination

- Severity: Low
- Category: Configuration validation / dependency integrity
- Location: `Assets/Scripts/System/Lib/DestinationDB.cs:88-102`
- Evidence:
  - Duplicate keys overwrite `_destinationDB[key]`.
  - `_farmWorkSites[key]` is updated only when the current row contains a `FarmWorkSite`; a later duplicate without one does not remove the earlier cached site.
  - Current inspected scenes contain no duplicate destination keys.
- Description:
  - A duplicate row can make position and capability come from different facility rows.
- Recommended fix:
  - Reject and report duplicate `BuildingType` rows, or replace/remove all associated cached capability entries atomically when a key is overwritten.
- Impact if unfixed:
  - A configuration mistake can send a Farmer to one object while applying work to another.

#### L-03 — Definition class is outside its owning script domain

- Severity: Low
- Category: Code convention / maintainability
- Location: `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs`
- Evidence:
  - `CodeConvention.md` places ScriptableObject class definitions in their owning script domain and reserves `Assets/Data/<Domain>` for asset instances.
  - The Farming domain now has enough scripts to justify `Assets/Scripts/System/Farming`.
- Description:
  - The file follows the repository’s legacy action-cost layout and the implementation plan, but not the current domain-ownership convention.
- Recommended fix:
  - During a deliberate structure cleanup, move the class definition into the Farming script domain while preserving its `.meta` GUID. Place future asset instances under a Farming data folder.
- Impact if unfixed:
  - Configuration ownership remains less discoverable and the generic `Data/ScriptableObject/Script` folder continues to grow.

## Findings By File

- `FarmWorkSite.cs`: State ownership and transition logic are appropriate. M-01 and M-02 apply.
- `FarmingAction.cs`: Provider invocation and pooled cleanup are correct. M-03 and M-04 apply.
- `FarmerActionSelector.cs`: Capability wiring belongs at this layer, and Idle fallback prevents fail/replan spam. H-01 makes that fallback the only current scene behavior.
- `DestinationDB.cs`: Initialization-time component caching is appropriate and avoids tick lookups. L-02 applies.
- `IInventory.cs` / `WarehouseInventory.cs`: Current unlimited implementation safely rejects invalid input and overflow before mutation. The generic contract has the M-01 ambiguity.
- `SeededRandomSource.cs`: Deterministic lazy initialization is appropriate. L-01 applies.
- `FarmProductionDefinition.cs`: Single-purpose immutable configuration shape is good. L-03 applies.
- `FarmWorkResult.cs`, `FarmWorkPhase.cs`, `IFarmWorkProvider.cs`, `IRandomSource.cs`: No direct issue found.
- `ActionContext.cs`: Trailing optional capability preserves call-site compatibility. No direct issue found; continued growth should still be monitored to prevent service-locator drift.
- `TestFarmProductionWindow.cs`: Correctly isolated under `TestOnly` and uses public APIs. No code defect found, but it is not wired or executed.
- `SampleScene.unity` / `GuardTest.unity`: H-01 applies.

## Cross-Cutting Findings

- The narrow provider boundary is justified even with one concrete implementation because it separates pooled action execution from facility state and supplies a clear test seam.
- Adding one farm capability to `ActionContext` does not yet make it a service locator. Additional role-specific capabilities should trigger a role-context review.
- Direct farm-to-warehouse deposit is a documented vertical-slice concession. It should not become the assumed final carry/deposit architecture.
- The provisional non-catalogue integer item key is acceptable for this isolated warehouse skeleton. Not adding Wheat to `ItemData.csv` is reasonable because the current catalogue drives Food/Drink interaction candidates. A separate item-definition source will be required before economy, carry, or general inventory integration.
- The concrete `SeededRandomSource` field is a documented concession; no additional issue is raised for it.
- The new minimal script `.meta` format matches existing repository examples and all reviewed GUIDs are unique.

## Positive Notes

- `WorkerNPC`, `IAction`, `ActionResult`, `ActionPool`, `DestinationDecider`, and `NPCStat` were not polluted with farm-specific state.
- Growing and harvesting apply exactly one state-machine step per successful provider call.
- Reaching maximum growth changes phase without producing output.
- Each successful harvest deposits output before decreasing the gauge, including the final harvest.
- Failed current `WarehouseInventory` additions leave both quantity and gauge unchanged.
- Multiple queued `FarmingAction` instances share one facility state through the same provider.
- NPC need costs apply only after successful farm work.
- Cancelled or unfinished actions never call `TryApplyWork`.
- `Clear()` resets the cached provider and timer before pool reuse.
- No runtime progress or warehouse quantity is stored in the ScriptableObject.
- No global Unity random state, scene search, or per-tick component lookup was introduced.

## Recommended Next Actions

1. Wire the farm composition in the actual gameplay scene and create the required definition asset.
2. Run the TestOnly window and real Farmer Play Mode acceptance scenarios.
3. Harden Unity lifetime checks in `FarmWorkSite` and validate all required action dependencies before work begins.
4. Clarify or replace the partial-acceptance inventory contract.
5. Unify runtime and predicted farming duration.
6. Add repository-owned tests for the state cycle, deterministic yield, failed inventory add, pooled action reuse, and destroyed/disabled provider behavior.
7. Address the low-severity random-range, duplicate-key, and file-placement findings during cleanup.

## Final Verdict

**Changes required before runtime acceptance.**

The responsibility split and core state-machine implementation are good, with no Critical architectural issue. Approval is blocked by the confirmed lack of scene composition, which currently converts all Farmer Work decisions into Idle. Resolve H-01 and the medium transaction/lifecycle safety issues, then complete Unity Play Mode validation before treating the Farm Production Gauge vertical slice as finished.