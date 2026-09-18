# Practical Unity scene/prefab work

Read `Tools/NpcHarness/SceneWork.md` for the shared scope and evidence contract.
This route uses bounded YAML edits or Unity APIs, not the TestOnly Harness Job allowlist. Default to direct
work in the original project. Use SceneWork.md's isolated shell workflow only for
large/high-impact work; briefly state the reason first. MCP is optional and was not
connected at the last environment check; discover actual capability when needed.

## Before editing

- Require the objective, exact writable asset paths (and companion meta ownership),
  relevant reference scene/prefab, intended changes, and evidence directory. This
  may be a concise root assignment; WorkerAssignment v1 is not this route's format.
- Route context through ProjectStructure and the relevant Systems leaf. Inspect
  the existing hierarchy/components/serialized fields before choosing edits.
- For connected tools, discover Unity instances and verify project root. Pin the intended instance when
  multiple are available. An exposed tool or `ready_for_tools=true` without a real
  project/instance is not a connection. Never mutate an unidentified Editor.
- For connected tools, inspect Edit/Play Mode, compilation/import status, loaded scenes and prefab stage.
  Do not stop the user's Play Mode, save/discard their unsaved work, or switch away
  from a dirty scene without permission. Git dirtiness and in-memory dirtiness differ.

## Editing

For small property/reference changes, edit assigned existing `.unity`/`.prefab` YAML
under SceneWork.md's bounded YAML safeguards. Inspect actual fields and reference
targets, preserve GUIDs/fileIDs and use minimal patches. No Editor connection is
required for this disk-edit route. Resolve possible overlapping unsaved Editor work
before writing; disk edits must not silently overwrite in-memory work.

For API work, edit only the assigned original assets through a working Editor
tool or authorized Editor helper entrypoint. Do not prepare a workspace by default.
Check and preserve Git-dirty files and in-memory changes before editing; do not
create a commit or revert existing changes automatically. If the original Editor
is closed, a scoped `-executeMethod` may operate directly on that project. If open,
require a working in-Editor entrypoint, not a second batch Editor. Missing connectivity
is a blocker to explain, not a reason to silently clone the project.

Only when isolation was selected, prepare the scoped workspace, put the authorized static
Editor method under `Assets/Editor/SceneWork` in that copy, run it with SceneWorkspace,
and inspect the method result plus saved assets. Keep manifest/receipt hashes outside
worker-controlled evidence. Root alone applies the candidate after common checks.
The method uses normal Unity APIs; do not add a new permanent Tool/profile for its
feature. It must write the current success/failure result passed via
`-sceneWorkResultPath`; process exit alone is insufficient.

When connected tools are selected, use `manage_scene`, `manage_gameobject`, `manage_components`,
`manage_prefabs`, `manage_asset` and material/texture tools as applicable. Discover
their current schemas rather than assuming parameter names. Query only the relevant
hierarchy branch and component fields; use paging and stable instance IDs/paths.

Production components, SpriteRenderer, UI and reference assignments are not limited
to Camera/HarnessTest/LineRenderer. Code dependencies must exist and compile first.
Use actual inspected property names and references, not guessed IDs. Asset import
settings follow the supplied importSpec/reference family.

Scene-instance edits do not authorize applying changes to their source prefab.
Prefab edits require its exact asset path in scope. Save only intended assets; avoid
Save All / global SaveAssets where unrelated dirty assets could be persisted.
One Unity mutation owner at a time. No automatic scene/prefab deletion or broad
rebuild: only remove objects/assets when the task authorizes that removal.

For shell work or an edit tools cannot express, a small temporary Editor script using Unity APIs
is permitted **when the root includes its path, entrypoint and target assets in the
assignment**. It is implementation code, not a new mandatory feature validator.
Use an existing execution route (`-executeMethod` on a closed/isolated project, or
a connected tool/menu entrypoint). Do not launch a second Editor on the open project.
Do not use arbitrary script execution to bypass scope, approvals or dirty-state checks.
If no API route is available, bounded YAML remains available for small resolvable
edits. Otherwise report the blocker; do not expand YAML edits into uncertain prefab
overrides or broad reconstruction just to finish.

## Mandatory minimum checks

1. Read intended saved values/references back. For YAML, inspect the changed blocks
   and resolve changed references on disk; for API edits, read back through Unity.
   Check Missing Script/broken references through Unity when available and report
   unavailable Unity checks as `NOT_VERIFIED`. Null optional fields are not defects.
2. Actual successful compilation remains mandatory for changed C#. When Unity
   refresh/import is available, wait for compilation/domain reload and inspect errors;
   `isCompiling=false` alone is not success. Record unavailable import checks separately.
3. Compare actual file changes to the recorded baseline and exact writable paths;
   preserve prior user edits and GUIDs. Run `git diff --check` on textual changes.
4. Compare the changed hierarchy/wiring with one relevant existing pattern. Record
   intentional deviations and what was not verified. This judgment is not a
   deterministic proof of architecture or visual similarity.

Use relevant existing tests/profiles when they cover the changed area. Do not create
a new gameplay gate per request. Play Mode is required when the agreed task/project
workflow requires runtime verification, not merely because an object was edited.
For YAML-only disk-edit scope, unavailable Unity import/load/runtime checks alone
do not block completion. They still block if the agreed task or selected workflow
requires them. No missing check may be represented as Pass.

Return `Candidate | Blocked | Failed`, changed paths, tool/result evidence, the four
checks above, deviations and unverified items. The root checks the actual diff and
decides completion under the selected common/extended validation scope.
