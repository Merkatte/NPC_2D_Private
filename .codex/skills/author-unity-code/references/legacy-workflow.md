# Legacy WorkerAssignment v1

Read only when root explicitly selects legacy WorkerAssignment v1 for author-unity-code. Ordinary code work uses SKILL.md and the common message contracts without these serialized fields or gate commands. This reference preserves existing policy/schema contracts; it does not broaden permissions or authorize verification development.

## Assignment and Ownership

Root serializes the assignment under .harness-runs/<runId>/assignments/ using the existing [worker-assignment.schema.json](../../../../Tools/NpcHarness/Schemas/worker-assignment.schema.json) and [author-unity-code policy v1](../../../../Tools/NpcHarness/SkillPolicies/author-unity-code.json).

Use the schema as the field definition, including acceptanceConditions, baselineCommit, baselineDirtyFiles (explicitly empty when none overlap), allowed/forbidden paths and operations, artifact paths, preliminary checks and report requirements. The code-specific execution contract is:

- execution.kind is direct-code;
- execution.sourcePaths lists every C# candidate;
- execution.owningDocumentPaths lists assigned Systems documents;
- execution.compileRequired is true.

Root records the pre-delegation assignment SHA-256 and supplies it separately. Do not edit the assignment or compute a replacement digest. Follow the selected policy's actual allowlists: ordinary helper-path permissions do not widen legacy v1 paths. Report an incompatible assignment to root rather than silently changing routes.

All implementation safeguards, mandatory code evidence and independent-review requirements in SKILL.md still apply. No candidate author may alter accepting gates, fixtures, review requests or snapshots.

## Root-Owned Acceptance

After preliminary checks, submit the candidate and evidence. Do not run the trust-boundary Scope Gate yourself. Root reconciles actual changes and runs from the repository root, using the platform's existing wrapper:

```text
./run-harness.cmd verify-scope --policy Tools/NpcHarness/SkillPolicies/author-unity-code.json --assignment <assignment-path> --assignment-sha256 <pre-delegation-hash> --run-id <runId>
```

The existing run-harness.sh wrapper supports the corresponding command on its supported platform. Root then runs the assignment's selected task-specific acceptance gate and required independent review under [extended-workflows.md](../../orchestrate-unity-work/references/extended-workflows.md). A failed selected requirement cannot be dropped or replaced with ordinary evidence.

Corrections use the same task-wide budget under [remediation-loop.md](../../orchestrate-unity-work/references/remediation-loop.md). After changes, root refreshes the complete affected gate profiles; a selected pinned protocol additionally retains its request/snapshot and required-gate rules.

## Legacy Report Additions

Use the common [WorkerReport](../../orchestrate-unity-work/references/worker-coordination.md) identity, status, changedFiles, checksRun, evidence, unresolved and deviations. For this legacy route only, also include:

- assignmentSha256: root-supplied pre-delegation digest;
- scopeGateResultPath: pending root verification until root supplies the actual result; never fabricate a path or pass;
- acceptanceGate: the assigned manifestPath, profile and profileVersion.

Use the assignment's existing worker-result/log artifact paths rather than creating another evidence summary. Report None for a candidate, or an accurate reason such as AssignmentIncomplete, ScopeConflict, UnapprovedDecision or VerificationFailure. Candidate is a submission for root acceptance, never approval.
