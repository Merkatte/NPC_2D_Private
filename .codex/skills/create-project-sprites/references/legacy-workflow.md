# Legacy WorkerAssignment v1

Read only when root explicitly selects WorkerAssignment v1 for create-project-sprites. Ordinary work uses SKILL.md and the common message contracts. This route preserves the existing policies/schemas and does not authorize new verification tooling.

## Serialized Assignment

Root serializes the assignment under .harness-runs/<runId>/assignments/ using the existing [worker-assignment.schema.json](../../../../Tools/NpcHarness/Schemas/worker-assignment.schema.json) and [create-project-sprites policy v1](../../../../Tools/NpcHarness/SkillPolicies/create-project-sprites.json).

Use the schema's exact fields for identity, baseline/dirty files, scope, allowed/forbidden operations, context, artifacts, acceptance and reporting. The code for this protocol is unchanged. The graphics execution contract uses execution.kind raster-art and records:

- assetFamily and intendedUse;
- outputPath and approved referenceAssets;
- importReferenceMetaPath and importSpecPath;
- width, height, requireAlpha, frameColumns and frameRows.

Root retains the assignment SHA-256 before delegation and supplies it separately. Do not edit the assignment, repin its digest or broaden the policy's paths. Ordinary staging/source permissions do not widen legacy v1 allowlists.

Persist importSpec at its assigned path using the exact existing [sprite-import-spec.schema.json](../../../../Tools/NpcHarness/Schemas/sprite-import-spec.schema.json). Keep additional handoff details and explicit override provenance in the worker report, not unknown schema properties. If a required operation is unsupported by this protocol, report the gap; do not change schemas or routes to force acceptance.

SKILL.md's target-setting precedence, measured-versus-visual distinction and role boundaries also apply here. Preserve any content hashes required by the selected assignment/protocol.

## Root-Owned Acceptance

After preliminary file checks and visual inspection, return the candidate, proposed import settings and evidence. The worker does not execute the trust-boundary Scope Gate. Root reconciles actual changes and runs the existing platform wrapper from the repository root:

```text
./run-harness.cmd verify-scope --policy Tools/NpcHarness/SkillPolicies/create-project-sprites.json --assignment <assignment-path> --assignment-sha256 <pre-delegation-hash> --run-id <runId>
```

Use the corresponding run-harness.sh command on its supported platform. Root then runs selected task-specific acceptance and arranges independent visual review under [extended-workflows.md](../../orchestrate-unity-work/references/extended-workflows.md). Never alter the gates to fit the candidate or drop a failed requirement.

Corrections use the same task-wide budget and complete affected-profile reruns from [remediation-loop.md](../../orchestrate-unity-work/references/remediation-loop.md). Selected pinned evidence retains its stricter freshness requirements.

## Legacy Report Additions

Use the common [WorkerReport](../../orchestrate-unity-work/references/worker-coordination.md), plus the sprite-specific referenceAssets, generationPrompt and importSpec. For this legacy route only, also provide:

- assignmentSha256: the root-supplied pre-delegation digest;
- scopeGateResultPath: pending root verification until root provides the actual result; never fabricate a pass;
- acceptanceGate: the assigned manifestPath, profile and profileVersion.

Use the assignment's worker-result/log artifact paths. Use None for a candidate or the concrete reason such as AssignmentIncomplete, ArtDirectionAmbiguous, GenerationFailure or VerificationFailure. Candidate is a submission, not final acceptance.
