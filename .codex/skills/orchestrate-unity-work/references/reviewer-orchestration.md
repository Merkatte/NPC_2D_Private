# Reviewer Orchestration

Use the policy section during scoping. Review after selected gates pass, or after
successful ordinary common checks when no gate profile was selected. Every C# change,
including temporary helpers, requires independent conventions/ownership/dependency
review. Compilation alone is not that review. Non-code work follows the risk policy.

## Review Policy

Set the policy during scoping and record its reason:

- `RequiredByRequestOrSkill`: the user, an applicable Skill, or project policy requires independent review;
- `RequiredByRisk`: the change has structural, integration, serialization, lifecycle, persistence, transaction, or material uncovered acceptance risk;
- `NotRequired`: the change is low risk, deterministically covered, and no higher-priority workflow requires review.

Do not downgrade the policy after implementation merely to avoid review. Upgrade it to `RequiredByRisk` if material structural risk is discovered while producing or inspecting the candidate. A deterministic pass can establish correctness for its checks but does not erase a previously identified structural risk.

## Lightweight evidence mode

When existing relevant gates support the pinned workflow, use `Tools/NpcHarness/ReviewEvidence.md`. Pin request
SHA-256 before work; snapshot inputs before gates; use one compact v2 review and the
`accept-review` evidence gate after it. Historical/unrelated v1 reviews remain valid
only in their original workflow and are not silently upgraded to the new envelope.
Choose conventions and ownership categories for C#; include cross-feature architecture
when applicable. A category not mechanically covered needs the same independent reviewer,
not another agent. Review only the diff and necessary adjacent dependencies/documents.

Always include dependency direction in code review. Ordinary work with no selected
GateResult uses the ordinary record in reviewing-unity-candidate; do not invent a
gate/result or build a bespoke validator merely to use accept-review.

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

Create a separate collaboration subagent with `fork_turns="none"` and direct it to use `$reviewing-unity-candidate`. It must be read-only and not a candidate author/worker. Record its identity. Wait for and inspect the actual result before completion. Wait for a free slot rather than reusing a worker as reviewer. Do not launch a detached CLI review and finish without its result.

The reviewer remains separate throughout the candidate lifecycle. Do not assign it remediation, implementation, gate editing, or worker duties after review. A worker peer opinion is not the independent review.

## Evidence Bundle

Give the reviewer only primary evidence:

1. the user's original request;
2. confirmed scope, exclusions, and every acceptance condition;
3. relevant project requirement and architecture documents;
4. the root-inspected actual diff, including relevant untracked files;
5. the current candidate's passing selected gates or ordinary common-check results, with compile logs and scope evidence;
6. candidate freshness evidence binding the diff to the common checks or selected gate run.

Do not include implementation summaries, WorkerReports, claims of completion, the implementer's reasoning, suspected defects, expected findings, recommended verdict, or proposed remediation. These bias an independent review and are not primary evidence.

## Result Intake

Accept exactly one review record that follows the reviewer Skill contract. Ordinary
review uses its ordinary record: verify reviewer identity, current candidate hashes,
reviewed files, required rule coverage, findings and verdict against common checks.
The GateResult/run-ID fields below apply only to selected gated workflows.
For those workflows, at minimum confirm:

For compact v2, use the field mapping and deterministic intake in
`Tools/NpcHarness/ReviewEvidence.md` instead of the legacy v1 field names below.

- `candidateRunId` equals the current GateResult run ID;
- selected gate profile/version/status match and `preserved` is true (ordinary review instead checks common evidence);
- `reviewerRole` is independent and read-only;
- freshness evidence applies to the unchanged current candidate;
- acceptance coverage includes every confirmed condition;
- finding severity, blocking status, and verdict agree;
- reviewed files and documents are within the supplied evidence boundary.

Treat a missing, malformed, stale, contradictory, or ineligible self-review result as `InsufficientEvidence`.

## Verdict Routing

### Approve

`Approve` satisfies review only while the unchanged candidate has current successful ordinary common checks or selected deterministic gates. Minor non-blocking findings may be reported; approval cannot replace verification or turn a failed check into success.

### ChangesRequested

Use only evidenced blocking findings to scope remediation. Root assigns the smallest authorized correction to the original role worker or a fresh same-role worker. Root and reviewer do not patch implementation files.

After any candidate mutation:

1. expire affected verification and review evidence;
2. inspect the actual new diff and scope;
3. refresh affected ordinary common checks and candidate hashes, or rerun complete selected gate profiles;
4. only after successful current verification, invoke an eligible read-only reviewer with a fresh primary-evidence bundle.

Do not ask the reviewer to edit the candidate or confirm a fix without current successful verification under the chosen route.

### InsufficientEvidence

Completion is forbidden. If the evidence defect can be repaired without changing the candidate and within current authority, supply corrected primary evidence and rerun review against still-current verification. Candidate changes require refreshed common checks or complete selected gate profiles first.

Do not consume candidate remediation budget for evidence-only repair. Stop for the user when obtaining evidence requires new permission, broader scope, or an unavailable external action. Stop rather than loop if the same evidence gap recurs after one correction.
