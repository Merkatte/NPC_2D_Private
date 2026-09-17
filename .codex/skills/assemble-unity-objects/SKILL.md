---
name: assemble-unity-objects
description: Assemble bounded Unity scene objects through the registered NPC Harness JSON Job tools and an orchestrator-supplied WorkerAssignment. Use for supported GameObject, Transform, Camera, LineRenderer, material, and TestOnly scene work; do not use for direct Unity YAML edits, unsupported production assets, code authoring, sprite generation, accepting-gate changes, or final acceptance.
---

# Assemble Unity Objects

Act as a tool-only Unity object assembly worker. Translate an already-scoped object specification into a valid Harness Job, execute it only through the registered adapter, and return the adapter receipt. Never edit `.unity`, `.prefab`, `.asset`, or `.meta` YAML directly.

## Required Assignment

Require a bounded assignment containing:

- `assignmentId`, objective, hierarchy, components, transforms, and serialized values;
- exact scene and other writable asset paths;
- separate run `artifactPaths` for the Job, adapter result, and logs;
- allowed tools and component types;
- existing code and art inputs that may be referenced;
- explicit overwrite authority, defaulting to denied;
- observable acceptance conditions, preliminary checks, and report requirements.

If required assignment fields or exact serialized values are absent, return `status: Blocked` with `reasonCode: AssignmentIncomplete`. If a required component, asset type, property, or path is not supported by the current adapter, use `reasonCode: ToolCapabilityGap`. Never bypass a missing Tool with direct YAML, ad hoc Editor scripting, or an unassigned C# helper.

## Confirm Current Capabilities

Read [references/current-tool-contract.md](references/current-tool-contract.md) before producing a Job. The current adapter is intentionally narrow and supports only `Assets/TestOnly` scene work with its registered allowlists. Assignment text cannot expand those allowlists.

Verify that required scripts, sprites, materials, and other dependencies already exist. Do not author code or graphics in this role.

## Build and Run the Job

1. Create one schema-versioned Job under an assigned `artifactPaths` location; do not treat the scene's writable ownership as permission to create arbitrary repository files.
2. Use only registered atomic Tools and stable, unique step IDs.
3. Create parent objects before children and components before configuration.
4. Put `SaveScene` after all changes to that scene.
5. Validate the Job against `Tools/NpcHarness/Schemas/harness-job.schema.json` and adapter policy before execution.
6. Run `./run-harness.sh run-adapter --job <path>` when the Unity project is closed. Use `--allow-overwrite` only when the assignment records explicit user approval for every value managed by the Tool, including documented implicit values.
7. If the project is locked by an open Editor and no automated interactive adapter path is available, return `status: Blocked` with `reasonCode: UnityProjectLocked`; do not take over the user's Editor or close it implicitly.
8. Preserve the result JSON and logs as Tool receipts. Job success or `NoChange` creates a candidate but does not prove acceptance.

## Boundaries

- Do not modify C#, sprites, source art, accepting gates, validator fixtures, or unrelated scenes.
- Do not add a generic reflection/property-setting Tool to finish one assignment.
- Do not remove or overwrite unknown existing components or values.
- Treat a scene and its `.meta` as one ownership unit. Never run concurrent Unity mutations in the same project.
- Report any unrequested import, `.meta`, scene, or project-setting change as a deviation.
- Because `SaveScene` also calls `AssetDatabase.SaveAssets()`, execute against a closed-Editor batch project or an isolated copy with a recorded baseline. Do not execute when unrelated in-memory dirty assets could be persisted.

## Return a WorkerReport

Return one structured report with:

```json
{
  "assignmentId": "assignment-id",
  "roleSkill": "assemble-unity-objects",
  "status": "Candidate | Blocked | Failed",
  "reasonCode": "None | AssignmentIncomplete | ToolCapabilityGap | UnityProjectLocked | AdapterFailure",
  "jobPath": "",
  "adapterResultPath": "",
  "changedFiles": [],
  "checksRun": [],
  "evidence": [],
  "unresolved": [],
  "deviations": []
}
```

`Candidate` means the Tool-produced object slice is ready for root inspection and an independent deterministic gate. It never means `Accepted`.
