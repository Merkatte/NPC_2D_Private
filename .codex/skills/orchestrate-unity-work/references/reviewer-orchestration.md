# Reviewer Orchestration

Use the policy section during scoping. Use the invocation and verdict sections only after the exact current candidate has a valid deterministic `GateResult` with `Pass` status.

## Review Policy

Set the policy during scoping and record its reason:

- `RequiredByRequestOrSkill`: the user, an applicable Skill, or project policy requires independent review;
- `RequiredByRisk`: the change has structural, integration, serialization, lifecycle, persistence, transaction, or material uncovered acceptance risk;
- `NotRequired`: the change is low risk, deterministically covered, and no higher-priority workflow requires review.

Do not downgrade the policy after implementation merely to avoid review. Upgrade it to `RequiredByRisk` if material structural risk is discovered while producing or inspecting the candidate. A deterministic pass can establish correctness for its checks but does not erase a previously identified structural risk.

## Lightweight evidence mode

For new code-candidate reviews use `Tools/NpcHarness/ReviewEvidence.md`. Pin request
SHA-256 before work; snapshot inputs before gates; use one compact v2 review and the
`accept-review` evidence gate after it. Historical/unrelated v1 reviews remain valid
only in their original workflow and are not silently upgraded to the new envelope.
Choose conventions and ownership categories for C#; include cross-feature architecture
when applicable. A category not mechanically covered needs the same independent reviewer,
not another agent. Review only the diff and necessary adjacent dependencies/documents.

Unrequested responsibility moves, new layers or shared-contract changes require user
direction before implementation. Updating current-state documentation does not authorize
changing normative rules. Snapshot documents preserve which version was reviewed; when
this task legitimately changes a normative rule, retain the approved pre-change rule and
approval in scoping evidence rather than passing against a silently weakened replacement.

Record actual author IDs in the request and compare reviewer identity to collaboration
records. Trust only root-retained request/snapshot hashes. The evidence gate cannot
authenticate actors or enforce OS isolation. A hash mismatch is not repaired by replacing
the pinned hash; re-establish an authorized candidate and rerun affected steps.

## Reviewer Identity and Invocation

Create a separate collaboration subagent and direct it to use `$reviewing-unity-candidate`. The reviewer must be read-only and must not be the root author or any agent listed in a `WorkerAssignment` for this candidate. Record its agent identity with the review result.

The reviewer remains separate throughout the candidate lifecycle. Do not assign it remediation, implementation, gate editing, or worker duties after review. A worker peer opinion is not the independent review.

## Evidence Bundle

Give the reviewer only primary evidence:

1. the user's original request;
2. confirmed scope, exclusions, and every acceptance condition;
3. relevant project requirement and architecture documents;
4. the root-inspected actual diff, including relevant untracked files;
5. the current candidate's passing GateResult and essential raw logs/artifacts;
6. candidate freshness evidence used to bind the diff to that gate run.

Do not include implementation summaries, WorkerReports, claims of completion, the implementer's reasoning, suspected defects, expected findings, recommended verdict, or proposed remediation. These bias an independent review and are not primary evidence.

## Result Intake

Accept exactly one `ReviewResult` that follows the reviewer Skill contract. At minimum confirm:

For compact v2, use the field mapping and deterministic intake in
`Tools/NpcHarness/ReviewEvidence.md` instead of the legacy v1 field names below.

- `candidateRunId` equals the current GateResult run ID;
- the recorded gate profile/version/status match and `preserved` is true;
- `reviewerRole` is independent and read-only;
- freshness evidence applies to the unchanged current candidate;
- acceptance coverage includes every confirmed condition;
- finding severity, blocking status, and verdict agree;
- reviewed files and documents are within the supplied evidence boundary.

Treat a missing, malformed, stale, contradictory, or ineligible self-review result as `InsufficientEvidence`.

## Verdict Routing

### Approve

`Approve` satisfies the review step only while the same deterministic `Pass` remains current. Minor non-blocking findings may be reported, but approval cannot replace the gate or turn a non-passing gate into success.

### ChangesRequested

Use only evidenced `Critical` or `Major` blocking findings to scope remediation. The root, never the reviewer, decides the smallest authorized correction under the existing retry budget.

After any candidate mutation:

1. expire the prior GateResult and ReviewResult;
2. inspect the actual new diff and scope;
3. rerun the complete deterministic gate;
4. only after a new `Pass`, invoke an eligible read-only reviewer again with a fresh primary-evidence bundle.

Do not ask the reviewer to edit the candidate or confirm a fix without a new passing gate.

### InsufficientEvidence

Completion is forbidden. If the evidence defect can be repaired without changing the candidate and within current authority, supply the corrected primary evidence and rerun review against the still-current gate. If the candidate changes, return to deterministic gating first.

Do not consume candidate remediation budget for evidence-only repair. Stop for the user when obtaining evidence requires new permission, broader scope, or an unavailable external action. Stop rather than loop if the same evidence gap recurs after one correction.
