# Code Evaluation Result

## Purpose

IMP-030 후속 pointer/raycast hover adapter를 읽기 전용으로 감사했다. 아키텍처·책임 경계, 상태 전환과 Unity lifecycle, 직렬화 참조 안전성, 코드 규칙을 우선순위대로 검토했으며 구현 수정은 수행하지 않았다.

## Review Snapshot

- Date: 2026-08-25
- Standards:
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `reviewing-npc-work-code` audit workflow
- Primary changed scope:
  - `Assets/Scripts/Interface/IHoverInfoSource.cs`
  - `Assets/Scripts/System/Farming/FarmWorkSite.cs`
  - `Assets/Scripts/UI/PointerHoverRouter.cs`
  - `Assets/Scripts/UI/PointerHoverRouter.cs.meta`
  - `Assets/Scripts/UI/FarmGaugeHover.cs`
  - `ProjectSettings/TagManager.asset`
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
- Direct dependency surfaces:
  - `IUIService`, `HoverInfo`, `HoverType`
  - `HoverBase`, `UIManager`, `PopBase`
  - `FarmGauge.prefab`
  - `FarmerTest.unity`
  - `Assembly-CSharp.csproj`
  - `PublicMD/PROGRESS.md`
- Excluded:
  - `.agents` additions and `Assets/_Recovery` artifacts
  - unrelated NPC actions, selectors, and providers
  - `Assets/BehaviorGraph/CustomActionNode` is absent

## Verification

- `git status`, changed-file diffs, untracked router source, and targeted reference searches were inspected.
- `Assembly-CSharp.csproj` currently includes all four relevant scripts, including `PointerHoverRouter.cs:134`. The implementation note saying the router is absent from the explicit compile list is no longer true for the reviewed snapshot.
- `dotnet build` was not rerun because this audit has a read-only sandbox and a build can write generated output. The reported earlier 0-warning/0-error build therefore remains user-supplied evidence and did not independently cover the router at the time it was run.
- Generation of a `.meta` file confirms Unity discovered the asset, but does not prove successful C# compilation.
- `git diff --check` currently fails only on newly generated `FarmerTest.unity` lines 313, 344, 980, 997, and 1329.
- `TagManager.asset` contains exactly 32 layer entries, with `Hoverable` only at index 7.
- The router GUID is unique in searched project assets.
- Play Mode was not run.
- `FarmerTest.unity` changed during the audit while the Editor was open. Scene findings therefore describe the final observed snapshot, not an atomic implementation snapshot.

## Executive Summary

Responsibility placement is sound. `PointerHoverRouter` is a UI-side input adapter, depends only on shared contracts and Unity input/physics types, and does not introduce concrete farm or concrete UI dependencies. Having `IHoverInfoSource` self-declare `HoverType` matches the newly documented routing model and keeps domain-type branching out of the adapter.

The current change is not ready for runtime acceptance. The existing `FarmGauge.prefab` was not migrated for the renamed class and new required fields. It retains the previous class identifier and contains neither `_progressFill` nor `_worldCamera`. More importantly, `FarmGaugeHover` treats this configuration failure as an `ApplyInfo` no-op while the base class still reports `TryShow` success, allowing the facade and router to believe an invisible view is active.

Two lifecycle gaps also remain in the candidate/active state machine: interface-held sources are not checked through `Owner`, and router state is not synchronized when `HoverBase` or `UIManager` hides an already active view independently.

## Priority Assessment

1. Architecture and responsibility placement: **None found.**
2. Correctness, lifecycle, cancellation, and regression risk: H-01, M-01, and M-02 apply.
3. Unity scene, prefab, component, and serialized-reference safety: H-01 and L-01 apply.
4. Convention, maintainability, dead code, and magic values: L-01 and L-02 apply. **No dead-code or magic-value issue was found in the changed C# code.**

## Improvements Since Previous Review

- `FarmGaugeHover.cs` now has a matching primary class/file name.
- The throwing `ApplyInfo` stub and empty `Start`/`Update` methods were removed.
- `HoverBase` refresh is no longer hidden by a derived `Update`.
- Required view dependencies are checked before the first `ApplyInfo` call.
- Candidate and successfully displayed active state are separated, so an initial failed `TryShow` is retried.
- The pointer adapter contains no runtime reference to `FarmWorkSite`, `HoverBase`, or `UIManager`.
- The prior report concerned `ItemDataContext` injection and is outside this audit; those findings were not revalidated.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — `FarmGauge` serialized migration is incomplete and misconfiguration can be reported as a successful show

- Severity: High
- Category: Unity serialized-reference safety / runtime correctness
- Location:
  - `Assets/Scripts/UI/FarmGaugeHover.cs:6-29`
  - `Assets/Scripts/UI/HoverBase.cs:16-30`
  - `Assets/Prefab/UI/FarmGauge.prefab:200-204`
  - `Assets/Scenes/FarmerTest.unity:1283-1286`
- Evidence:
  - `FarmGaugeHover` now requires `_progressFill` and `_worldCamera`.
  - The prefab contains only `_hoverType` and `_refreshInterval`; neither new required field is serialized.
  - Its `m_EditorClassIdentifier` still names `Assembly-CSharp::FarmHoverHover`.
  - The `m_Script` GUID still matches `FarmGaugeHover.cs.meta`, so the GUID reference was preserved, but the stale type identifier has not been reserialized and cannot be treated as verified.
  - In the final observed scene snapshot, `UIManager._hovers` contains `{fileID: 0}` and the prefab instance has no overrides for the required fields.
  - When `_isConfigured` is false, `ApplyInfo` returns silently. `HoverBase.TryShow` nevertheless returns true, after which `UIManager` and `PointerHoverRouter` record the source as active.
- Description:
  - The class rename and new serialized dependencies require an asset migration, not only preservation of the `.meta` GUID.
  - The present failure path is not fail-closed: an unusable view can be considered visible and active even though nothing was rendered.
  - Scene placement was explicitly deferred, but migrating the reusable prefab’s fill reference and ensuring the renamed component resolves are separate serialized-asset requirements.
- Recommended fix:
  - After a successful Unity compile/domain reload, open and save `FarmGauge.prefab` so the component is confirmed as `FarmGaugeHover`.
  - Assign `_progressFill` in the prefab.
  - Assign the scene camera on the placed instance, then register the actual `FarmGaugeHover` component in `UIManager._hovers`.
  - Give `HoverBase`/`UIManager` a configuration-readiness contract so an unconfigured concrete view causes `TryShow` to return false instead of succeeding with a no-op.
  - Confirm no Missing Script state in the Inspector; do not manually infer safety from the preserved GUID alone.
- Impact if unfixed:
  - Farm hover cannot render.
  - Once registry wiring is added, the router may stop retrying because the facade reports the invisible view as successfully active.
  - The system can report `HasVisibleHover == true` while presenting no usable UI.

### Medium

#### M-01 — Router ignores the `Owner` validity contract for interface-held Unity objects

- Severity: Medium
- Category: Unity lifecycle / destroyed-object safety
- Location:
  - `Assets/Scripts/UI/PointerHoverRouter.cs:27-35, 87-94, 120-126`
  - `Assets/Scripts/Interface/IHoverInfoSource.cs:5`
  - `Assets/Scripts/UI/HoverBase.cs:77-84`
- Evidence:
  - `_candidateSource` and `_activeSource` are stored as `IHoverInfoSource`.
  - The router tests them with ordinary `== null` and `ReferenceEquals`.
  - `IHoverInfoSource.Owner` exists specifically to expose Unity destroyed-object validity, and `HoverBase` correctly uses `source.Owner`.
  - `CodeConvention.md` §9.2 requires a backing Unity object or explicit validity adapter for interfaces holding Unity objects.
- Description:
  - If a source component is destroyed or replaced while its collider remains the current hit, the interface reference can remain non-null in ordinary C# terms.
  - `HoverBase` can detect the destroyed owner and hide, while the router can retain the dead candidate/active references and take the `ReferenceEquals` early return.
- Recommended fix:
  - Centralize a `HasUsableSource` check equivalent to `source != null && source.Owner`.
  - Re-resolve the hit collider when its cached source becomes unusable, even if the collider itself did not change.
  - Clear router bookkeeping independently of whether a `TryHide` call can still be delivered.
- Impact if unfixed:
  - Hover state can become stuck until the pointer changes collider.
  - A replacement source on the same collider may never be discovered.
  - Router state can disagree with the actual visible state owned by `HoverBase`.

#### M-02 — Router active state is not synchronized when the facade or view hides independently

- Severity: Medium
- Category: State-machine correctness / regression risk
- Location:
  - `Assets/Scripts/UI/PointerHoverRouter.cs:93-102`
  - `Assets/Scripts/UI/HoverBase.cs:61-74`
  - `Assets/Scripts/UI/UIManager.cs:90-104`
- Evidence:
  - After one successful `TryShow`, the router returns whenever candidate and active source are reference-equal.
  - `HoverBase.Update` can call `HideCurrent` when `TryGetHoverInfo` later fails.
  - `UIManager.HideAll` can also hide the active hover without notifying the router.
  - Neither path clears `PointerHoverRouter._activeSource`.
- Description:
  - The candidate/active split correctly retries an initial failed `TryShow`, but it assumes that a successful show remains active until the router hides it.
  - That assumption is already contradicted by the existing `HoverBase` and `UIManager` APIs.
  - If the source later becomes valid again while the pointer remains on the collider, the router does not retry because of the reference-equality early return.
- Recommended fix:
  - Add a narrow facade query or state-change signal that lets the router determine whether its source/type is still the active visible hover.
  - Alternatively, move candidate ownership and restoration into the facade so only one component owns the complete state machine.
  - Preserve the deferred popup-policy decision; do not add a popup guard without a corresponding restoration path.
- Impact if unfixed:
  - `HideAll`, transient source failure, or future popup blocking can leave hover permanently absent until pointer exit/re-entry.
  - Future UI policy changes can reintroduce the stuck-hover regression already identified during planning.

### Low

#### L-01 — Empty hover mask logs an error but keeps the polling component active

- Severity: Low
- Category: Serialized configuration / maintainability
- Location: `Assets/Scripts/UI/PointerHoverRouter.cs:55-56, 66-82`
- Evidence:
  - Missing camera or invalid UI service disables the component.
  - An empty `_hoverableMask` only logs, then continues polling `Physics2D.OverlapPoint` indefinitely.
  - `CodeConvention.md` §9.1 requires missing required configuration to log once and disable safely.
- Description:
  - An empty mask makes successful routing impossible, so it is a required configuration failure rather than a recoverable gameplay state.
- Recommended fix:
  - Disable the router after logging an empty mask, or explicitly document and implement a runtime reconfiguration path.
- Impact if unfixed:
  - Misconfigured scenes perform useless polling and remain nonfunctional despite a clear initialization error.

#### L-02 — Architecture and validation documentation contains stale or overstated claims

- Severity: Low
- Category: Documentation drift / validation accuracy
- Location:
  - `PublicMD/ARCHITECTURE.md:5, 401`
  - `PublicMD/ProjectStructure.md:445-447`
  - `PublicMD/PROGRESS.md:55-61`
  - `Assembly-CSharp.csproj:134`
- Evidence:
  - `ARCHITECTURE.md` and the lower scene section of `ProjectStructure.md` still name `SampleScene.unity`, which is absent; the repository contains `FarmerTest.unity` and `GuardTest.unity`.
  - `PROGRESS.md` says both documents corrected `SampleScene` to `FarmerTest`, but the stale entries remain.
  - `PROGRESS.md` treats `.meta` generation as minimal syntax validation. Asset discovery does not establish successful compilation.
  - The current `.csproj` includes `PointerHoverRouter.cs`, so the next build can and should validate it directly.
- Description:
  - The routing architecture text is otherwise aligned with the code, but repository-state and validation claims are not fully accurate.
- Recommended fix:
  - Replace remaining `SampleScene.unity` references with `FarmerTest.unity`.
  - After a real build containing the router, update `PROGRESS.md` with the actual 0-warning/error result.
  - Record `.meta` generation only as asset discovery, not syntax or compile validation.
- Impact if unfixed:
  - Future reviewers and implementers may rely on the wrong integration scene or overestimate the verification performed.

## Findings By File

- `Assets/Scripts/Interface/IHoverInfoSource.cs`
  - No responsibility-boundary issue found.
  - `HoverType` and `Owner` form a narrow routing and Unity-validity contract.
- `Assets/Scripts/System/Farming/FarmWorkSite.cs`
  - No issue found in the changed hover members.
  - It exposes read-only domain state without referencing a concrete UI type.
- `Assets/Scripts/UI/PointerHoverRouter.cs`
  - Responsibility placement is appropriate.
  - M-01, M-02, and L-01 apply.
- `Assets/Scripts/UI/FarmGaugeHover.cs`
  - The class/file mismatch, throwing stub, and hidden base refresh were corrected.
  - H-01 applies to its configuration contract and serialized asset.
- `Assets/Scripts/UI/HoverBase.cs`
  - Correctly uses `Owner` for destroyed-object validity.
  - Its autonomous hide behavior exposes the M-02 synchronization gap.
- `Assets/Scripts/UI/UIManager.cs`
  - Remains a generic facade/registry without domain formatting or concrete view fields.
  - `HideAll` contributes to M-02 because the router receives no invalidation signal.
- `Assets/Prefab/UI/FarmGauge.prefab`
  - H-01 applies.
- `ProjectSettings/TagManager.asset`
  - No semantic issue found. The 32 layer slots are preserved and index 7 is the only newly named slot.
- `PublicMD/ARCHITECTURE.md`, `PublicMD/ProjectStructure.md`, `PublicMD/PROGRESS.md`
  - L-02 applies.
- `Assets/Scenes/FarmerTest.unity`
  - Scene wiring was explicitly deferred and changed concurrently during review.
  - The final observed state has a null hover registry entry and no router/farm layer wiring, so Play Mode readiness is not verified.

## Cross-Cutting Findings

- Candidate, active source, facade-active view, and concrete view visibility currently have multiple owners without an invalidation signal. M-02 should be resolved before popup blocking is added.
- Preserving a script GUID is necessary but not sufficient when a class is renamed and gains required serialized fields. The affected prefab still requires Editor migration and reference wiring.
- Interface-held Unity objects must consistently use their explicit owner/backing object contract. `HoverBase` does this; the router currently does not.
- The documented single-result limitation of `Physics2D.OverlapPoint` is accurate and intentionally accepted for this slice.

## Positive Notes

- No concrete domain or concrete UI runtime dependency was introduced in `PointerHoverRouter`.
- `Mouse.current` matches the project’s Input System-only setting.
- Polling uses unscaled time and a serialized, validated interval.
- `TryGetComponent` is only performed when the hit collider changes under normal valid-source operation.
- `OnDisable` attempts to clear active hover state and resets the candidate cache.
- `FarmGaugeHover.ApplyInfo` is idempotent and does not allocate or search the scene.
- `HoverInfo` clamps normalized progress before it reaches the view.
- `FarmWorkSite` keeps progress ownership in the facility rather than moving UI state into a manager.
- No new selector, action, worker runner, or manager responsibility drift was found.
- No dead code, repeated LINQ, scene-wide `Find*`, string routing, or new magic tuning value was found in the changed C# code.

## Recommended Next Actions

1. Migrate and wire `FarmGauge.prefab`, then make unconfigured views fail `TryShow`.
2. Use `IHoverInfoSource.Owner` consistently in `PointerHoverRouter`.
3. Define how router active state is invalidated when `HoverBase` or `UIManager` hides independently.
4. Disable the router on an empty hover mask.
5. Finish the explicitly deferred scene wiring and save a stable `FarmerTest.unity`.
6. Run `dotnet build Assembly-CSharp.csproj --no-restore` now that the router is included.
7. Run `git diff --check` after Unity finishes saving the scene.
8. Perform Play Mode cases for enter, exit, target switch, failed show retry, source invalidation, router disable/re-enable, and `HideAll` while the pointer remains stationary.
9. Correct the stale scene and validation statements in the documentation.

## Final Verdict

**Changes requested.**

The architecture and responsibility placement are approved, but H-01 blocks runtime acceptance. M-01 and M-02 should also be resolved before popup policy or additional hover sources expand the state machine. Scene wiring and Play Mode behavior remain **Not Verified**.