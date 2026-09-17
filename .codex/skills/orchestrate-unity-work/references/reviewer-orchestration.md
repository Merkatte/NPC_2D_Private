# Reviewer Orchestration

Use the policy section during scoping. Use the invocation and verdict sections only after the exact current candidate has a valid deterministic `GateResult` with `Pass` status.

## Review Policy

Set the policy during scoping and record its reason:

- `RequiredByRequestOrSkill`: the user, an applicable Skill, or project policy requires independent review;
- `RequiredByRisk`: the change has structural, integration, serialization, lifecycle, persistence, transaction, or material uncovered acceptance risk;
- `NotRequired`: the change is low risk, deterministically covered, and no higher-priority workflow requires review.

Do not downgrade the policy after implementation merely to avoid review. Upgrade it to `RequiredByRisk` if material structural risk is discovered while producing or inspecting the candidate. A deterministic pass can establish correctness for its checks but does not erase a previously identified structural risk.

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
