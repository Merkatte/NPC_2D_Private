---
name: assemble-unity-objects
description: Assemble bounded Unity scene objects through the registered NPC Harness JSON Job tools and an orchestrator-supplied WorkerAssignment. Use for supported GameObject, Transform, Camera, LineRenderer, material, and TestOnly scene work; do not use for direct Unity YAML edits, unsupported production assets, code authoring, sprite generation, accepting-gate changes, or final acceptance.
---

# Assemble Unity Objects

Act as a tool-only Unity object assembly worker. Translate an already-scoped object specification into a valid Harness Job, execute it only through the registered adapter, and return the adapter receipt. Never edit `.unity`, `.prefab`, `.asset`, or `.meta` YAML directly.

## Required Assignment

Require a bounded assignment containing:

- `assignmentId`, `runId`, `roleSkill`, `policyVersion`, and `objective`;
- `execution.kind: harness-job` with explicit Job/receipt paths, component types, overwrite authority, and `taskSpecification` hierarchy, components, transforms, and serialized values;
- `requiredContext`, exact `writablePaths`, and explicit `forbiddenPaths`;
- `allowedTools` and `forbiddenOperations`;
- existing code and art inputs in `requiredContext`;
- separate run `artifactPaths` for the worker report and log;
- root-recorded `baselineCommit` and `baselineDirtyFiles`;
- `acceptanceGate`, observable `acceptanceConditions`, `preliminaryChecks`, and `reportRequirements`.

The assignment must be serialized as WorkerAssignment v1 under
`.harness-runs/<runId>/assignments/` and must select
`Tools/NpcHarness/SkillPolicies/assemble-unity-objects.json` v1. Read
`Tools/NpcHarness/Schemas/worker-assignment.schema.json` when authoring that contract. The assignment may narrow the tracked policy but never broaden it.
The root orchestrator owns this file, records its SHA-256 before delegation, and supplies that digest separately. Do not edit the assignment or recompute a replacement digest.

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
9. After the adapter receipt exists, return the candidate and receipts to the root. The root must run `./run-harness.sh verify-scope --policy Tools/NpcHarness/SkillPolicies/assemble-unity-objects.json --assignment <assignment-path> --assignment-sha256 <pre-delegation-hash> --run-id <runId>`. Do not run this trust-boundary gate as the worker and do not rewrite the assignment or policy to fit the diff.
10. The assignment's `acceptanceGate` identifies the independent task-specific manifest and profile. Do not run or interpret that accepting gate as this worker; the root executes it only after the scope GateResult passes and the candidate is reconciled.

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
  "jobPath": "execution.jobPath",
  "adapterResultPath": "execution.adapterResultPath",
  "assignmentSha256": "root-supplied pre-delegation digest",
  "scopeGateResultPath": "pending root verification",
  "acceptanceGate": {
    "manifestPath": "",
    "profile": "",
    "profileVersion": 1
  },
  "changedFiles": [],
  "checksRun": [],
  "evidence": [],
  "unresolved": [],
  "deviations": []
}
```

`Candidate` means the Tool-produced object slice is ready for root inspection and an independent deterministic gate. It never means `Accepted`.
