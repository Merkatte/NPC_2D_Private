---
name: author-unity-code
description: Implement a bounded Unity C# candidate according to NPC_Work_2D document routing, code conventions, ownership boundaries, and an orchestrator-supplied WorkerAssignment. Use for delegated code creation, refactoring, or bug fixes; do not use for sprite production, direct scene or prefab editing, accepting-gate authorship, or independent review.
---

# Author Unity Code

Act as a candidate-producing code worker, not as the root orchestrator. Implement only the assigned code slice and return evidence for root inspection. Never mark the overall run accepted.

## Required Assignment

Require a bounded assignment containing:

- a stable `assignmentId` and one concrete `objective`;
- observable `acceptanceConditions`;
- exact `writablePaths` and `forbiddenPaths`;
- separate `artifactPaths` for authorized build logs or run evidence outside the candidate;
- `baselineCommit` plus `baselineDirtyFiles`, using an explicit empty object when none overlap;
- `requiredContext` project documents and candidate inputs;
- preliminary checks and report requirements.

The root must serialize this assignment as WorkerAssignment v1 under
`.harness-runs/<runId>/assignments/`, select
`Tools/NpcHarness/SkillPolicies/author-unity-code.json` v1, and set
`execution.kind` to `direct-code`. `execution.sourcePaths` lists every C# candidate,
`execution.owningDocumentPaths` lists any assigned Systems documents, and
`execution.compileRequired` must be true. Read
`Tools/NpcHarness/Schemas/worker-assignment.schema.json` for the exact contract.
The root records the assignment SHA-256 before delegation and supplies it separately.
Do not edit the assignment or recompute a replacement digest.

If the assignment omits a safe writable boundary or exact source paths, requires an unapproved product decision, or conflicts with existing user changes, return `status: Blocked` with `reasonCode: AssignmentIncomplete`. Do not broaden the assignment yourself.

## Route Project Context

1. Read `PublicMD/ProjectStructure.md` and use its task table to find the owning Systems document.
2. If that feature is folder-shaped, read its `README.md` and only the relevant leaf documents.
3. Read the leaf's minimum change scope before opening additional code or Unity assets.
4. Read `PublicMD/CodeConvention.md` before creating or modifying C#.
5. Read `PublicMD/Game_Plan.md` and `PublicMD/SPEC.md` only when the assignment changes gameplay rules or player experience. Keep unresolved rules as `TBD`.
6. Read `PublicMD/PLAN.md` when the assignment begins a planned feature or depends on an active detailed plan.
7. Read `PublicMD/ARCHITECTURE.md` only when responsibility, a shared interface, dependency direction, or runtime ownership crosses feature boundaries.

Do not read or modify `CLAUDE.md` or `.claude`. Treat actual code and serialized assets as current fact when documentation is stale, and correct the owning Systems document within the assignment when authorized.

## Author the Candidate

- Modify only assigned C# files, their new companion `.meta` files, and explicitly assigned owning documentation.
- Preserve unrelated and pre-existing changes. Do not reset, reformat, or clean files outside the assignment.
- Keep `WorkerNPC`, selector, action, provider, `NPCComponent`, manager, runtime state, and shared-definition ownership consistent with current project documents.
- Place new code according to `PublicMD/ProjectStructure.md`; do not introduce future architecture that the project does not have.
- Preserve serialized enum values, field migration, and existing `.meta` GUIDs.
- For a new C# asset, prefer Unity import to create its `.meta`. When the assignment requires creating the companion `.meta` without an importer, generate a unique GUID, verify that it does not occur elsewhere in the repository, and never replace the GUID of an existing asset.
- Do not edit scenes, prefabs, materials, textures, animation assets, ProjectSettings, accepting gate code, or gate fixtures unless the assignment explicitly makes one of those files the code worker's sole owned path.
- Do not use direct Unity YAML edits as a substitute for missing object-wiring tools.
- When a required non-code change is outside the assignment, report it as unresolved instead of performing it.

## Preliminary Verification

Run the narrowest meaningful combination authorized by the assignment:

- compile the affected assembly or perform the repository's equivalent compile check;
- run focused tests for changed behavior;
- search for forbidden references or stale serialized names when relevant;
- inspect actual changed paths and run `git diff --check`.

Normal compiler output under `Library`, `Temp`, or an assignment-authorized temporary directory is verification output, not a candidate change. Persisted logs and reports must stay within `artifactPaths`. Do not add generated build output to Git or treat it as permission to alter unrelated source files.

Compilation does not prove scene wiring or runtime behavior. Record `NOT_VERIFIED` for checks that require Unity Editor, Play Mode, external authority, or another worker.

After preliminary verification, return the candidate and evidence to the root. Do not run the trust-boundary Scope Gate yourself. The root must run:

`./run-harness.sh verify-scope --policy Tools/NpcHarness/SkillPolicies/author-unity-code.json --assignment <assignment-path> --assignment-sha256 <pre-delegation-hash> --run-id <runId>`

The root then runs the assignment's separate task-specific acceptance gate. Never modify either gate to fit the candidate.

## Return a WorkerReport

Return one structured report with:

```json
{
  "assignmentId": "assignment-id",
  "roleSkill": "author-unity-code",
  "status": "Candidate | Blocked | Failed",
  "reasonCode": "None | AssignmentIncomplete | ScopeConflict | UnapprovedDecision | VerificationFailure",
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

`Candidate` means the code slice is ready for root inspection and deterministic gating. It never means `Accepted`. Do not review your own candidate or modify the accepting gate.
