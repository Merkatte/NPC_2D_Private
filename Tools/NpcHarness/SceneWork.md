# Practical scene-work route

Use this route for scoped work in `Assets/Scenes/FarmerTest.unity`,
`Assets/Scenes/GuardTest.unity`, other explicitly assigned scenes and related prefabs.
Do not create a new Harness Job tool or gameplay validator for every request.

## Scope before work

Record one compact `.harness-runs/<runId>/scene-work.json` (or equivalent Markdown):

- objective, exact writable files, authorized deletions/overwrites, reference pattern;
- baseline commit, current dirty paths and before hashes/copies of affected dirty files;
- actual execution route and target Unity project/instance;
- mandatory common checks and any explicitly selected extended checks;
- maximum corrective attempts (normally 2), evidence/log paths.

The root owns this record. Workers cannot expand it. Preserve existing dirty changes;
do not treat permission to edit a file as permission to replace all its contents.
Scope changes need user direction when they materially change the request.

## Choose direct work or isolation

**Default: edit the original project directly.** A scoped script change, object
addition, reference adjustment or small scene/prefab edit does not require a copy.
Record the Git baseline and preserve existing uncommitted and unsaved work. Git
can recover committed files; it does not protect unsaved Editor memory or overwritten
uncommitted changes. Do not auto-commit, stash, reset or revert on the user's behalf.
Ask only when overlapping existing work cannot be preserved safely.

Use isolation for genuinely large/high-impact work: bulk edits across many scenes
or shared prefabs, broad serialized-data migrations, or difficult-to-reverse mass
restructuring. Judge the affected scope and recovery difficulty, not a fixed file
count or the mere presence of serialized edits. Briefly state why isolation is
needed before creating a copy. When the choice materially affects scope and is
unclear, ask the user. The common checks remain the same in both modes.

## Available execution routes

1. **Bounded YAML file edits (ordinary small changes):** edit existing `.unity` or
   `.prefab` properties/references directly under the safeguards below. No MCP or
   Editor execution entrypoint is needed for disk edits; Unity validation is separate.
2. **Direct Unity Editor API work:** edit the original project through an
   available connected Editor tool or an authorized, actually invocable Editor-only
   helper. Verify target project and dirty state; save only assigned changes.
   A closed original project may be edited using Unity `-executeMethod` directly,
   without cloning. Never launch a second Editor on an already open project.
   An open Editor requires a working in-Editor execution entrypoint. If none exists,
   report that connection/entrypoint blocker; do not silently switch to a copy or
   claim live editing is available. Do not implicitly install MCP or close the Editor.
3. **Isolated shell workflow (large/high-impact work only):** use `SceneWorkspace.ps1`
   to prepare a copy, run a Unity Editor API method and apply exact authorized files.
   This uses no MCP and returns a short summary; full Unity logs stay on disk.
   The original Editor may stay open during preparation/execution. Applying files
   requires the original Editor to be closed so unsaved in-memory edits cannot be
   overwritten. Never close it or discard changes without the user's permission.
4. **Connected Unity tools (optional direct-work transport):** use existing Unity MCP tools for scene, prefab,
   component, reference and importer operations. A registered tool without a connected
   Editor is not an executable route. Verify instance/project identity before writes.
5. **Legacy Harness Job:** remains available for its existing TestOnly fixtures only.
   Its v1 policies/assignment/`verify-scope` are unchanged and do not validate the
   other routes. Do not fabricate v1 evidence for a different execution route.

### Bounded YAML safeguards

- Inspect the target block, actual serialized field names and one existing wiring
  pattern. Patch only assigned properties/references; do not rebuild the whole file.
- Preserve existing object fileIDs and asset GUIDs. Resolve changed local references
  to actual objects and external GUID/fileID pairs to inspected assets/subassets;
  do not invent IDs. Preserve prefab source/instance/override relationships.
- Preserve dirty disk content. If the target is open with possible unsaved edits,
  establish with the Editor or user that disk editing will not overwrite that work;
  unknown overlapping in-memory state is not permission to proceed. Do not force
  reload, Save All, discard or close the Editor.
- Prefer Unity APIs for object creation/removal, complex prefab overrides or broad
  restructuring. If serialization cannot be resolved confidently, stop that slice
  or select an available API route; no new permanent Tool/profile is required.
- This permission covers small scene/prefab edits, not direct importer `.meta` or
  arbitrary `.asset` rewrites. Existing selected legacy allowlists remain unchanged.
- Read the edited blocks back, inspect the actual diff and changed reference targets.
  Disk inspection is not Unity import/load or runtime verification. Record unavailable
  Unity checks as `NOT_VERIFIED`, never Pass.

## Isolated shell workflow (Windows PowerShell 5.1, optional)

Use only after choosing isolation above; `SceneWorkspace.ps1` is not a direct-edit
runner and its existing safety checks stay intact. Keep authorized helpers Editor-only
and outside runtime code in either mode. No implicit Save All, unsaved-work discard
or copy-back over changed source files.

Run from the repository root. If script execution is restricted, request approval
for the specific script/process; do not change the machine-wide execution policy.

```powershell
$workspace = "$PWD/.harness-runs/my-scene-change/project"
./Tools/NpcHarness/SceneWorkspace.ps1 -Action Prepare -Workspace $workspace `
  -Targets @('Assets/Scenes/FarmerTest.unity', 'Assets/Scenes/GuardTest.unity')
# Retain the printed ManifestSha256 outside worker-owned evidence.
# Author the assigned static Editor method in the COPY's Assets/Editor/SceneWork.
./Tools/NpcHarness/SceneWorkspace.ps1 -Action Run -Workspace $workspace `
  -ManifestSha256 '<retained manifest hash>' -ExecuteMethod 'MySceneEdit.Run' `
  -UnityPath 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe'
# Inspect method-result.json, saved assets, scope and the common checks.
# Retain ReceiptSha256. Only after the original Editor is saved/closed by its owner:
./Tools/NpcHarness/SceneWorkspace.ps1 -Action Apply -Workspace $workspace `
  -ManifestSha256 '<retained manifest hash>' -ReceiptSha256 '<retained receipt hash>'
```

`Targets` are exact `.unity`, `.prefab`, `.mat`, or `.asset` files in existing Assets
folders, plus their automatically owned companion `.meta`. Only assign assets the
request actually needs; the example does not require editing both scenes. No source
code, helper, package or project-setting changes are promoted by this runner. Root
code/art authoring remains a separate slice. New folders, PNG import settings and
asset deletion are not supported by this shell promotion path yet; report the gap
or use another explicitly authorized existing Editor route, never silently broaden.

The static Editor method receives `-sceneWorkResultPath <absolute path>`. On success
it must save only intended assets, read them back, then write fresh JSON containing
boolean `success: true`, a short `message`, and its actual `checks`/`unverified` details.
On failure write `success: false` and exit nonzero. The runner requires both Unity
exit 0 and a fresh success result; it does not interpret the truth of helper assertions.
Never write success in `finally`. Use normal Unity APIs such as EditorSceneManager,
PrefabUtility and SerializedObject; no feature-specific harness registration needed.

Prepare copies current on-disk Assets/Packages/ProjectSettings (excluding _Recovery),
not unsaved Editor memory or Library. Initial import can take time without consuming
model tokens for every log line. Keep Unity's version equal to ProjectVersion.txt.
Package restore may require network/permission. Stop on infrastructure failure rather
than modifying gameplay files to make startup succeed.

Run/Apply pin evidence and compare current source/candidate hashes. Out-of-scope
workspace changes, source/dependency drift, changed GUIDs and missing paired meta
block promotion. Root retains both hashes, reviews the helper and sequences all writes.
Use a fresh run after an attempted execution; no automatic retry or stale-result reuse.
Default timeout is 300 seconds and configurable explicitly up to 3600 seconds.
Backups and apply-start/applied records remain beside the workspace. Multi-file
promotion is not atomic: a late I/O failure can leave a partial apply; inspect backups
and preserve newer user work before recovery. This is not a hostile-code sandbox.

## Common completion checks

The root verifies actual evidence, not only a worker's success claim:

- scope/diff: actual changed paths versus baseline; unrelated dirty work preserved;
- compile/import: successful current compilation when code changes. Check Unity import
  completion when available; otherwise record it as `NOT_VERIFIED`;
- edited assets: intended saved values/references read back and GUIDs preserved. For
  YAML edits, inspect changed blocks and resolve changed references on disk; report
  Unity load/Missing Script checks separately, `NOT_VERIFIED` if not executed;
- conventions/pattern: relevant code conventions, reference art family or existing
  hierarchy/wiring comparison, limited to the changed slice;
- concise record: what changed, which reference/rule was used, deviations, checks
  actually run and unverified behavior. Store long tool output/logs on disk.

Code compilation, code conventions and responsibility/dependency checks are unchanged.
For a YAML-only edit, scope/diff, serialized readback, changed-reference inspection and
the concise record are the completion minimum; unavailable Unity import/load/Play Mode
checks do not block that disk-edit scope alone. If the agreed task or selected workflow
requires those Unity checks, they remain mandatory and missing evidence blocks completion.
Shell/diff checks and
existing validators provide mechanical evidence; style/ownership assessment remains
review judgment. The isolated shell route checks file scope/conflicts at promotion;
the root still performs the semantic/readback checks and bounds implementation code.
It is **not** a sandbox or Git hook, and cannot contain malicious Editor scripts.

A missing required check means Blocked/incomplete. A Pass only proves the checks
actually run, never all gameplay behavior. No custom functional profile is required
by default; select existing tests or an extended check when the request calls for it.
Keep specialized NPC planning/approval and runtime verification requirements intact.
Root confirms ordinary changes; commission independent review only when the user,
specialized skill or material structural/serialization risk requires it. Existing
review-evidence commands remain available when that workflow is selected.

## Optional connection setup

Unity MCP tools are present in this session, but initial inspection on 2026-09-19
found zero connected instances and no Unity bridge package in this project. The user
raised token-cost concerns, so no MCP package was installed. The direct-work default
is a routing policy, not proof that an open-Editor connection exists. Small YAML disk
edits can proceed under the safeguards above without that connection. API operations
need an available in-Editor entrypoint or direct batch execution on a closed original
project; report missing API capability instead of cloning ordinary work automatically.
If MCP is selected later, query only needed hierarchy branches
and properties; avoid whole-scene dumps and repeated polling in model context.
Until project-info and a smoke test succeed, do not describe MCP as connected.
