# Code Evaluation Result

> 현재 경로: `PublicMD/Status/Code_Evaluation_Result.md`

## Purpose

Read-only audit of IMP-033, which moves building-entry presentation ownership from destination metadata to the Eat/Drink/Sleep action lifecycle. The review prioritized architecture, lifecycle correctness, Unity serialized-reference safety, and maintainability. No files were modified.

## Review Snapshot

- Date: 2026-08-27
- Standards:
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `reviewing-npc-work-code` audit workflow
- Primary scope:
  - `Assets/Scripts/System/Action/BaseBuildingAction.cs`
  - `Assets/Scripts/System/Action/DefaultAction.cs`
  - `Assets/Scripts/System/Action/EatAction.cs`
  - `Assets/Scripts/System/Action/DrinkAction.cs`
  - `Assets/Scripts/System/Action/SleepAction.cs`
  - `Assets/Data/Struct/ActionContext.cs`
  - `Assets/Scripts/System/Lib/DestinationDB.cs`
  - `Assets/Scripts/System/Actor/FarmerActionSelector.cs`
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs`
  - `Assets/Scenes/FarmerTest.unity`
  - `Assets/Scenes/GuardTest.unity`
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/Status/PROGRESS.md`
- Direct dependency surfaces:
  - All `DefaultAction` subclasses
  - `WorkerNPC`
  - `ActionPool`
  - `BaseNPCActionSelector`
  - `DestinationDecider`
  - `NPCComponent`
  - `WorkerPool`
  - `NPCGirl_Move.controller`
  - `NPCGirlAnimatorControllerConfigurator`
  - `NPCGirl.prefab`
  - `Assembly-CSharp.csproj`
- Excluded:
  - Unrelated skill, UI, farming, and accumulated scene changes except where they affected serialization or delivery verification
  - `.agents` and `Assets/_Recovery`
  - `Assets/BehaviorGraph/CustomActionNode`, which is absent

## Verification

- Ran `git status`, `git diff`, `git diff --stat`, `git diff --name-status`, `git diff --check`, and targeted source/YAML searches.
- Confirmed that:
  - `BaseBuildingAction` is the only action implementation that calls `SetInsideBuilding`.
  - Eat, Drink, and Sleep inherit `BaseBuildingAction`.
  - Move, Farming, Idle, Guard, and Attack inherit `DefaultAction` directly.
  - `BaseBuildingAction` clears presentation through `Complete`, `RequestReplan`, `Fail`, `Stop`, and `Clear`.
  - `Clear` calls `ExitBuilding` before `DefaultAction.Clear` removes `ActionContext`.
  - `ShouldUseBuildingAnimation` and `UsesBuildingAnimation` have no remaining references under `Assets`, `ARCHITECTURE.md`, or `ProjectStructure.md`.
  - `ActionContext` and `DestinationInfo` contain no presentation metadata.
  - Both current scene `DestinationDB` blocks contain only building type, destination Transform, destination object, and manager wiring.
  - `Assembly-CSharp.csproj` includes `BaseBuildingAction.cs`.
  - The new script GUID is unique and is not serialized into scenes or prefabs, which is expected for this plain C# action base.
  - `NPCGirl.prefab` retains valid `NPCComponent`, Animator, Controller, and worker-prefab references.
- The previous Farmer defect is statically resolved:
  - Farmer may still share one `ActionContext` between Move and the semantic action, but Move cannot trigger building presentation because it does not inherit `BaseBuildingAction`.
- The exact slice-local scene delta cannot be reconstructed from Git because the removed metadata existed only in an earlier uncommitted working-tree state. Current scene data confirms the fields are absent.
- The reported `dotnet build Assembly-CSharp.csproj --no-restore` result of 0 warnings and 0 errors was not independently rerun because the sandbox is read-only and builds write generated output.
- Play Mode was not run. Travel continuity, animation timing, cancellation visuals, and second-use pooling behavior remain **NOT VERIFIED**.
- Whole-tree `git diff --check` still fails on accumulated Unity YAML trailing-space lines. No trailing whitespace was found in the IMP-033 handwritten C# or documentation files.

## Executive Summary

IMP-033 places building presentation responsibility correctly. Building entry is an execution property of the semantic indoor action rather than destination metadata, and the new base class contains meaningful shared lifecycle behavior rather than forming an empty inheritance layer. `DefaultAction`, `ActionContext`, selectors, and `DestinationDB` are correspondingly narrower.

The previous high-severity Farmer bug is resolved structurally: `MoveAction` has no path to `SetInsideBuilding`, even when it shares a context with Eat, Drink, Sleep, or Farming.

No architecture or dependency-direction issue was found in IMP-033. The lifecycle cleanup implementation is idempotent and correctly preserves context until presentation cleanup is attempted.

Acceptance of the complete working tree remains blocked by untracked required files. In particular, the modified Eat/Drink/Sleep classes depend on an untracked `BaseBuildingAction.cs`; a patch containing only tracked Git diffs will not compile. The earlier untracked animation and configurator assets also remain unresolved.

A pre-existing Animator cancellation defect remains directly relevant: setting `IsInsideBuilding` to false during `EnterBuilding` cannot interrupt that state. The C# cleanup signal is correct, but the Controller cannot react until the entry clip finishes.

## Priority Assessment

1. Architecture and responsibility placement: **No issue found.**
2. Correctness, lifecycle, cancellation, and regression risks: **M-01 found.** No additional correctness defect was found in the IMP-033 action lifecycle.
3. Unity scene, prefab, component, and serialized-reference safety: **H-01 found.** No stale building-presentation field or broken current prefab/scene reference was found.
4. Code convention, maintainability, dead code, and magic values: **M-02, L-01, and L-02 found.** No new dead code, gameplay magic value, repeated component lookup, scene search, or naming violation was found in IMP-033.

## Improvements Since Previous Review

- The previous Farmer context-scoping finding is resolved structurally.
- Building presentation metadata has been removed from `ActionContext`, `DestinationDB`, and scene destination rows.
- `DefaultAction` is again limited to common action lifecycle and result handling.
- Eat, Drink, and Sleep explicitly advertise their indoor lifecycle through inheritance.
- Movement and non-building actions cannot accidentally opt into presentation through shared context data.
- Building cleanup remains centralized and idempotent across completion, failure, replan, cancellation, and pool return.
- Documentation now describes the action-owned presentation model consistently.
- No new manager lookup, singleton dependency, concrete facility dependency, serialized field, or enum migration was introduced.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — Required runtime and animation files remain untracked

- Severity: High
- Category: Changeset completeness / Unity serialized-reference safety
- Location:
  - Untracked IMP-033 files:
    - `Assets/Scripts/System/Action/BaseBuildingAction.cs`
    - `Assets/Scripts/System/Action/BaseBuildingAction.cs.meta`
  - Modified dependants:
    - `Assets/Scripts/System/Action/EatAction.cs:3`
    - `Assets/Scripts/System/Action/DrinkAction.cs:3`
    - `Assets/Scripts/System/Action/SleepAction.cs:3`
  - Untracked earlier animation/configurator files:
    - `Assets/Animation/DefaultAnim.meta`
    - `Assets/Animation/DefaultAnim/*`
    - `Assets/TestOnly/Editor.meta`
    - `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs`
    - `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs.meta`
  - Deleted tracked animation paths:
    - `Assets/Animation/NPCGirl_Idle.anim`
    - `Assets/Animation/NPCGirl_Idle.anim.meta`
    - `Assets/Animation/NPCGirl_Move.anim`
    - `Assets/Animation/NPCGirl_Move.anim.meta`
  - Controller motion references:
    - `Assets/Animation/NPCGirl_Move.controller:150,268,295,322,349`
- Evidence:
  - `git status --untracked-files=all` reports `BaseBuildingAction.cs` and its `.meta` as untracked.
  - `git ls-files --error-unmatch` confirms neither new base file is known to Git.
  - Eat, Drink, and Sleep now inherit that new type.
  - `Assembly-CSharp.csproj` includes the local file, explaining why the reported local build can pass.
  - The five Controller motion GUIDs resolve only to files under the untracked `DefaultAnim` directory.
  - The original tracked Idle and Move paths remain deleted.
- Description:
  - The current filesystem can compile and resolve animation references because all required files exist locally.
  - A changeset produced from tracked `git diff` alone omits the new base class and required animation replacements.
  - IMP-033 therefore strengthens the existing delivery risk: without the new base file, the three modified semantic actions do not compile in a clean checkout.
- Recommended fix:
  - Include `BaseBuildingAction.cs` and its `.meta` in the delivered changeset.
  - Include all required animation clips, their `.meta` files, folder metadata, and the Editor configurator.
  - Verify from a clean checkout that the runtime and Editor projects compile and all Controller motion GUIDs resolve.
- Impact if unfixed:
  - A clean checkout can fail compilation because `BaseBuildingAction` is missing.
  - Animator states can have missing motion references.
  - Idle and Move animation references can be broken.
  - Validation tooling documented by the project may be absent.

### Medium

#### M-01 — `EnterBuilding` cannot react promptly to cancellation

- Severity: Medium
- Category: Lifecycle / cancellation correctness / animation regression
- Location:
  - `Assets/Scripts/System/Action/BaseBuildingAction.cs:19-46`
  - `Assets/Animation/NPCGirl_Move.controller:106-127`
  - `Assets/Animation/NPCGirl_Move.controller:281-285`
  - `Assets/Scripts/Actor/WorkerNPC.cs:61-65`
  - `Assets/Scripts/Actor/WorkerNPC.cs:105-124`
- Evidence:
  - `BaseBuildingAction` correctly sets `IsInsideBuilding` to false on completion, replan, failure, stop, and clear.
  - `EnterBuilding` has exactly one outgoing transition.
  - That transition is unconditional, waits for exit time 1, targets `InsideBuilding`, and has no interruption source.
  - There is no `EnterBuilding -> ExitBuilding` or restoration transition conditioned on `IsInsideBuilding == false`.
  - `WorkerNPC` can discard the failed/replanned action and start a replacement queue immediately.
- Description:
  - The C# lifecycle emits the correct cancellation signal, but the Animator cannot consume it while `EnterBuilding` is active.
  - The entry clip must finish, the Controller must enter `InsideBuilding`, and only then can the false condition trigger `ExitBuilding`.
  - Gameplay movement or another action can therefore resume while the NPC is still playing the entry/exit visual sequence.
  - This is pre-existing Controller behavior, not a new IMP-033 ownership defect.
- Recommended fix:
  - Add an explicit cancellation/restoration transition from `EnterBuilding` when `IsInsideBuilding` becomes false.
  - Define whether cancellation should enter `ExitBuilding` or restore directly to an external locomotion state.
  - Update the configurator and its validator to own that transition.
  - Run Play Mode cases for stop, replan, transaction failure, disable, and pool return during entry.
- Impact if unfixed:
  - A cancelled NPC can remain shrinking, hidden, or exiting after gameplay has resumed.
  - Locomotion presentation can be delayed after failure or replan.
  - C# cleanup appears immediate while the actual visual cleanup is not.

#### M-02 — Animator validation still accepts configurations outside its claimed contract

- Severity: Medium
- Category: Validation reliability / maintainability
- Location:
  - `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs:32-41`
  - `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs:80-110`
  - `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs:204-248`
- Evidence:
  - `Validate` checks that each required source/destination pair exists exactly once.
  - It does not reject other managed-state edges or Any State transitions.
  - Motion validation checks only that each state has a non-null motion; it does not verify the expected clip or GUID.
  - It does not validate transition offset, interruption source, or ordered-interruption settings.
  - `Configure` returns without repair whenever this incomplete validation succeeds.
- Description:
  - The validator proves that required pieces exist but does not prove that the Controller exactly matches the accepted state machine.
  - This becomes more important if the M-01 cancellation transition is added and the tool is expected to remain authoritative.
- Recommended fix:
  - Compare the complete managed transition edge set against the expected set.
  - Reject unexpected Any State and managed-state transitions.
  - Validate expected motion assets and all settings owned by the configurator.
  - Include the intended cancellation path in both configuration and validation.
- Impact if unfixed:
  - Controller drift or accidental clip replacement can pass validation.
  - The configurator can incorrectly report “already configured” and decline to repair a divergent Controller.
  - Reviewers can overestimate what the isolated validation proves.

### Low

#### L-01 — Architecture documents still reference a nonexistent Farmer scene

- Severity: Low
- Category: Documentation drift
- Location:
  - `PublicMD/ARCHITECTURE.md:5`
  - `PublicMD/ARCHITECTURE.md:403`
  - `PublicMD/ProjectStructure.md:453`
- Evidence:
  - These locations identify `SampleScene.unity` as the Farmer execution scene.
  - `Assets/Scenes/SampleScene.unity` does not exist.
  - `Assets/Scenes/FarmerTest.unity` exists and is listed in the actual file tree.
- Description:
  - The stale scene name remains in documentation directly edited for IMP-033.
- Recommended fix:
  - Replace the stale `SampleScene.unity` references with `FarmerTest.unity`.
- Impact if unfixed:
  - Implementers and reviewers can inspect or attempt to configure the wrong integration scene.

#### L-02 — The complete working tree still fails the documented diff-quality gate

- Severity: Low
- Category: Repository hygiene / verification
- Location:
  - `Assets/Animation/NPCGirl_Move.controller`
  - `Assets/Scenes/FarmerTest.unity`
  - `Assets/Scenes/GuardTest.unity`
- Evidence:
  - Whole-tree `git diff --check` reports trailing whitespace on empty Unity YAML scalar lines.
  - `CodeConvention.md` includes `git diff --check` in the minimum validation combination.
  - A targeted trailing-whitespace search found no violation in the IMP-033 handwritten C# or documentation files.
- Description:
  - The failures are accumulated Unity YAML patterns and were not introduced by the new base class.
  - Nevertheless, the current repository snapshot does not pass the project’s stated whole-tree gate.
- Recommended fix:
  - Normalize the affected serialization where practical, or document a narrowly scoped Unity-YAML exception.
  - Rerun `git diff --check` against the final delivered changeset.
- Impact if unfixed:
  - CI or automated review checks can fail.
  - Genuine whitespace defects become harder to distinguish from serialization noise.

## Findings By File

- `Assets/Scripts/System/Action/BaseBuildingAction.cs`
  - Responsibility placement and naming follow the project conventions.
  - The base provides real shared lifecycle behavior.
  - Cleanup is idempotent, handles partial initialization, and runs before context removal.
  - H-01 applies because the file is untracked.
  - M-01 applies through the downstream Animator response.
- `Assets/Scripts/System/Action/DefaultAction.cs`
  - No building-presentation policy remains.
  - No issue found.
- `Assets/Scripts/System/Action/EatAction.cs`
  - Correctly inherits `BaseBuildingAction`.
  - Dependency validation failure routes through the overridden `Fail` cleanup.
  - No new issue found.
- `Assets/Scripts/System/Action/DrinkAction.cs`
  - Correctly inherits `BaseBuildingAction`.
  - Dependency validation failure routes through the overridden `Fail` cleanup.
  - No new issue found.
- `Assets/Scripts/System/Action/SleepAction.cs`
  - Correctly inherits `BaseBuildingAction` and remains sealed.
  - No new issue found.
- `Assets/Data/Struct/ActionContext.cs`
  - Contains no destination-presentation metadata.
  - The current context remains an execution dependency bundle rather than presentation policy.
  - No issue found.
- `Assets/Scripts/System/Lib/DestinationDB.cs`
  - Contains no building-animation lookup or field.
  - It remains limited to destination and interaction-provider lookup.
  - No issue found.
- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`
  - The shared Move/semantic context no longer leaks presentation into Move.
  - The previous high-severity Farmer defect is resolved.
  - No issue found.
- `Assets/Scripts/System/Actor/GuardActionSelector.cs`
  - Supply movement and semantic action construction remain correct.
  - No issue found.
- `Assets/Scripts/Actor/WorkerNPC.cs`
  - Stop precedes action return/clear on cancellation paths.
  - That ordering allows `BaseBuildingAction` to release presentation before context reset.
  - M-01 applies to the Animator’s delayed response.
- `Assets/Scripts/System/Lib/ActionPool.cs`
  - `ReturnAction` calls `Clear` before enqueueing, preserving pooled-state safety.
  - No issue found.
- `Assets/Scripts/System/Actor/NPCComponent.cs`
  - Remains a thin actor-local Unity adapter.
  - `ResetRuntimeState` explicitly restores external animation state.
  - No new issue found.
- `Assets/Scenes/FarmerTest.unity`, `Assets/Scenes/GuardTest.unity`
  - No stale `UsesBuildingAnimation` or `ShouldUseBuildingAnimation` field remains.
  - DestinationDB script GUID and current destination rows match the current class.
  - L-02 applies to accumulated YAML whitespace.
- `Assets/Animation/NPCGirl_Move.controller`
  - Current motion GUIDs and prefab Controller reference are valid in the local filesystem.
  - H-01 and M-01 apply.
- `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs`
  - Correctly isolated as Editor tooling.
  - H-01 and M-02 apply.
- `Assets/Prefab/InGame/NPCGirl.prefab`
  - `NPCComponent`, Animator, Controller, Transform, visual, and Guard-perception references remain intact.
  - No issue found.
- `PublicMD/ARCHITECTURE.md`, `PublicMD/ProjectStructure.md`
  - The BaseBuildingAction ownership descriptions match the implementation.
  - L-01 applies.
- `PublicMD/Status/PROGRESS.md`
  - The current implementation description matches the inspected source.
  - The build result remains user-reported rather than independently reproduced.

## Cross-Cutting Findings

- Building presentation is now structurally tied to semantic action type, eliminating context-data leakage into unrelated actions.
- Lifecycle cleanup requires cooperation between action state and Animator transition topology. A correct boolean reset does not guarantee prompt visual restoration.
- Local build success does not establish delivery completeness when required source and asset files remain untracked.
- Animator validation should distinguish “required pieces exist” from “the Controller exactly matches the accepted state machine.”

## Positive Notes

- `BaseBuildingAction` is a justified abstract base with meaningful common lifecycle behavior.
- The implementation follows the project’s `Base...` naming convention and action folder placement.
- The new base has no manager, registry, provider, scene-search, or serialized dependency.
- `DefaultAction` remains generic and reusable by non-building actions.
- Move, Farming, Idle, Guard, and Attack have no route to building presentation.
- Eat, Drink, and Sleep share identical entry/cleanup boundaries without duplicating code.
- Entry is applied only after `DefaultAction.Start` validates `NPCComponent`.
- Failure after Eat/Drink provider validation clears presentation through virtual dispatch.
- `Stop` and `Clear` are safe before `Start`.
- `Clear` preserves `ActionContext` until after presentation release.
- The internal flag makes repeated completion/stop/clear cleanup idempotent.
- Worker disable and reinitialization include an additional `NPCComponent.ResetRuntimeState` safety boundary.
- No serialized destination migration remains necessary for building presentation.
- No enum value or script GUID migration was introduced.
- Scene `DestinationDB` rows match the current `DestinationInfo` shape.
- No new dead code, repeated hot-path lookup, scene search, or gameplay tuning magic value was introduced.

## Recommended Next Actions

1. Include all required untracked runtime, animation, metadata, and Editor files in the delivered changeset.
2. Add and validate an explicit cancellation/restoration path from `EnterBuilding`.
3. Run Play Mode verification for:
   - Move-state continuity while traveling to Pub or Inn
   - Entry beginning only when Eat, Drink, or Sleep starts
   - Exit after normal completion
   - Failure, replan, stop, and disable during entry and inside states
   - Second pooled spawn starting external and Idle
   - Move, Farming, Guard, Attack, and Idle never triggering building presentation
4. Make the Animator configurator validate the complete accepted state machine and exact motion assets.
5. Correct the stale `SampleScene.unity` documentation.
6. Resolve or formally handle the accumulated Unity YAML whitespace failures.
7. Rebuild runtime and Editor projects from a clean checkout and verify all Controller GUIDs resolve.

## Final Verdict

**Changes requested for the complete working tree.**

IMP-033’s architecture and C# ownership model are approved. The previous Farmer movement-time entry defect is statically resolved, and no new architecture problem was found.

Delivery acceptance remains blocked by H-01 because the new base class and required animation assets are untracked. Runtime lifecycle acceptance also remains incomplete because M-01 is still present and all requested Play Mode scenarios are **NOT VERIFIED**.
