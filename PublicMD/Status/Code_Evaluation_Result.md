# Code Evaluation Result

## Purpose

Review the Builder citizen source candidate against documented responsibility boundaries, lifecycle contracts, Unity serialization rules, and code conventions.

**Review only. No files were modified, and no fixes or Unity setup actions were executed.**

## Review Snapshot

- **Date:** 2026-09-26
- **Scope:** Supplied changed C# files, new Builder selector/action/cost and Editor setup source, changed documentation, and directly affected dependencies.
- **Standards:** `reviewing-npc-work-code` and its audit workflow, `PublicMD/ProjectStructure.md`, `PublicMD/CodeConvention.md`, relevant Systems leaves, and the approved Builder implementation plan.
- **Evidence:** Git status/diff, current source, targeted prefab/scene/animation YAML, new metadata, and `.harness-runs/builder-citizen-20260926/`.
- **Additional observed changes:** `PublicMD/Status/PROGRESS.md` and `BuilderCitizenSetup.cs.meta` appeared during review. Their relevant contents were inspected; neither was modified.
- **Validation boundary:** Source review only. Unity connection and execution were explicitly deferred by the user.

## Executive Summary

**No actionable defect was confirmed in the reviewed source candidate.**

The change preserves the existing selector → action → provider responsibilities. `WorkerNPC` does not acquire Builder decision logic, and actor-specific timers and path state remain outside shared ScriptableObjects.

| Review priority | Result |
|---|---|
| 1. Architecture and responsibility placement | No issue found |
| 2. Correctness, lifecycle, cancellation, regression | No confirmed issue found through source inspection |
| 3. Unity components and serialized references | No confirmed source-level issue found; resulting Builder assembly remains **NOT VERIFIED** |
| 4. Convention, maintainability, dead code, magic values | No actionable issue found in the changed code |

This conclusion does **not** establish gameplay completion or successful Unity integration.

## Improvements Since Previous Review

The previous report covered seed planting, so its findings are outside this candidate’s scope. Their resolution was not re-evaluated.

Compared with the current Git baseline, this candidate adds:

- A dedicated Builder selector and bounded Wander action using existing decision and movement services.
- Role-based tool selection in the presentation adapter, including reset behavior.
- Recruitment UI support for a variable card count with duplicate-role rejection.
- Documentation that distinguishes source implementation from deferred Unity assembly and execution.

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

None found.

### Low

None found.

## Findings By File

| File or group | Evidence and assessment |
|---|---|
| `Assets/Scripts/System/Actor/BuilderActionSelector.cs:26` | Reuses `DestinationDecider` for needs decisions. Builds separate movement and interaction contexts, returns partial rentals on failure, and uses timed Idle fallback. No duplicated utility formula or provider transaction was introduced. |
| `Assets/Scripts/System/Action/WanderAction.cs:14` | Owns execution, path following, elapsed needs, movement duration and rest. Pause prevents ticking; Stop clears the follower; Clear resets follower, cost, timers, flags and inherited context. |
| `Assets/Data/ScriptableObject/Script/WanderActionCost.cs` | Stores shared duration/rate tuning. Defaults match the approved 5-second movement, 1-second rest and 0.3-per-second need changes. Validation clamps serialized values; actor state is not stored in the asset. |
| `Assets/Scripts/Actor/TilemapNavigation.cs:113` | Uses injected randomness, bounded candidate attempts, and existing `TryBuildPath` validation. Candidate nodes derive from ground costs after obstacle processing. New caches are cleared on disable. |
| `Assets/Scripts/Actor/WorkerNPC.cs:131` | Passes the role to `NPCComponent.ApplyRoleTool`; does not choose Builder behavior or execute presentation details itself. |
| `Assets/Scripts/System/Actor/NPCComponent.cs:94`, `:148` | Resets tool state and selects sprites through serialized role mappings. Unconfigured Farmer mappings retain the original sprite; other unconfigured roles hide the tool. |
| `Assets/Scripts/Enum/NPCType.cs`, `ActionType.cs` | Builder and Wander are appended, preserving existing numeric values. |
| `Assets/Scripts/System/Lib/ActionPool.cs` | Wander has a matching factory case. Existing return logic calls Clear before reuse. |
| `Assets/Scripts/UI/TownHallPopup.cs`, `TownHallRecruitCard.cs` | Supports the added role without moving cooldown, spending or reservation ownership into UI. Card validation rejects null entries and duplicate roles. Existing listener lifecycle remains intact. |
| `Assets/TestOnly/TestNPCSpawnWindow.cs` | Builder uses the existing `NPCManager.CreateNPC` entry point. It requires the deferred creation entry, as documented. |
| `Assets/TestOnly/Editor/BuilderCitizenSetup.cs` | Explicit menu entry with no automatic import execution hook. Configures role entries, independent stat/cost assets, tool mapping and recruitment cards. Source was inspected; execution and saved results were not verified. |
| New runtime `.meta` files and hammer metadata | The four supplied new GUIDs have no duplicate definitions in the searched Assets metadata. No current scene/prefab/SO references to them were found. |
| Changed plan, SPEC, routing and Systems documents | Ownership and deferred integration status agree with inspected source and current saved assets. The additional PROGRESS entry preserves the same verification boundary. |

Direct dependencies inspected included `BaseNPCActionSelector`, `DefaultAction`, `NPCPathFollower`, `ActionContext`, needs actions, `DestinationDecider`, `NPCStat`, stat definitions, `SeededRandomSource`, `NPCManager`, `WorkerPool`, and `TownHallRecruitment`.

## Cross-Cutting Findings

No cross-system ownership violation was found:

- **Selection:** Builder selector chooses and assembles the queue.
- **Execution:** Wander owns movement/rest progress and need changes.
- **Navigation:** TilemapNavigation validates reachability; the follower owns per-action path progress.
- **Transactions:** Existing needs providers and Town Hall recruitment retain their domain responsibilities.
- **Presentation:** NPCComponent owns role tool selection.
- **Lifecycle:** WorkerNPC retains action queue execution and cancellation.

The bounded random search can fail to find a destination within its attempt limit and return Idle. This is explicitly documented behavior, not a guarantee that every query finds an existing reachable destination.

## Positive Notes

- Partial queue construction returns already-rented actions.
- Wander’s pooled state is explicitly cleared.
- Critical unmet needs prevent replacement with wandering.
- New gameplay randomness is injected rather than using global Unity randomness.
- Existing tool carry/work clips have empty object-reference curves; those inspected clips do not overwrite the selected sprite.
- Recruitment costs and cooldowns remain in recruitment configuration.
- Documentation does not equate compilation with gameplay completion.

## Verification

| Check | Result |
|---|---|
| Git status, changed diff and new source inspection | Completed |
| `git diff --check` | PASS; line-ending normalization warnings only |
| Targeted ownership, lifecycle, factory and reference searches | Completed |
| Supplied new runtime/art GUID duplicate search | No duplicate definitions found |
| Existing scene/prefab inspection | FarmerTest creation entries and Town Hall cards remain Farmer/Guard; Builder assembly is absent |
| Builder stat and cost asset existence | Both absent, consistent with Setup not having run |
| Stored `compile.log` | Runtime and Editor assembly outputs shown; **0 warnings, 0 errors** |
| Supplemental MSBuild targets | Explicit inclusion of new runtime and Setup sources inspected |
| Independent build | Not run |
| Setup, Unity import, Play, FarmerScene.Structure gate | **NOT VERIFIED** |

## Verification Limits

- Builds were not rerun because they write output and intermediate files, contrary to the read-only instruction. The saved log supports the reported build result; it is not an independently reproduced build of this review snapshot.
- Existing YAML was inspected to understand dependencies and confirm deferred assembly. No resulting Builder scene, prefab or SO assembly exists to validate.
- The hammer’s current metadata differs from the intended Setup import settings. Those settings remain unapplied; this is not treated as a completed importer configuration.
- Sprite appearance, grip alignment, card layout, animation, recruitment execution, navigation behavior and repeated pooling were not observed in Unity.
- No focused Builder runtime checks, native Play checks, current structure gate or candidate acceptance gate were executed.
- This report does not replace `FarmerScene.Structure` or `Harness.ReviewEvidence` acceptance.

## Recommended Next Actions

When the deferred Unity work resumes:

1. Run the explicit Setup entry and inspect the resulting scene, prefab, SO and importer references.
2. Verify Builder recruitment, independent cooldown, needs visits and return to wandering.
3. Exercise Wander cancellation/reuse and Farmer/Builder/Guard tool reset, including existing-role regressions.
4. Verify hammer presentation and recruitment layout in Unity, then run the required structure and acceptance gates.

## Final Verdict

**Approve the source candidate within the reviewed scope.**

No actionable source defect was confirmed. **Unity integration, runtime behavior and gameplay completion remain NOT VERIFIED.**