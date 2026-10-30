# Extended Workflows

Read only for user-requested extended/pinned evidence or an authorized task explicitly selecting legacy Job/WorkerAssignment v1 or strict GateResult acceptance. Ordinary work follows SKILL.md and does not need these envelopes merely because an existing checker is available.

This reference preserves existing contracts; it does not authorize new tests, runners, validators or harness adapters. Verification development and automatic Play Mode/game-screen checks each still need the user's separate explicit request. An extended-evidence request alone does not grant either.

## Select the Existing Protocol Before Work

Record the selected protocol, required existing profile/version/check IDs, scope, evidence paths and shared task-wide correction budget. Read [gate-acceptance.md](gate-acceptance.md) for strict GateResult acceptance. A selected mandatory gate without an executable route is a blocker, not a passing result; missing bespoke automation in ordinary work is a different case.

Keep original project/Editor state safe under [SceneWork.md](../../../../Tools/NpcHarness/SceneWork.md). Use the supported run-harness.cmd/run-harness.sh batch route only when the project is closed, or an actually available in-Editor entrypoint when open. Do not launch a second Editor on an open project or substitute isolation merely because a connection is unavailable.

## Legacy Job and WorkerAssignment v1

Read the selected role skill's legacy branch and existing policy/schema. Serialize the required v1 assignment before delegation and retain its pre-delegation SHA-256 separately under root control. Keep the role's exact allowlists, baseline/dirty-file evidence and tool restrictions; ordinary route permissions do not broaden legacy policies.

After reconciling the actual candidate, root runs the required verify-scope command with the pre-delegation assignment hash and selected policy, plus the selected acceptance profiles. A worker cannot author its accepting gate or replace root-owned scope acceptance. Existing HarnessBeacon/TestOnly profiles do not validate unrelated production work.

## Strict GateResult Acceptance

After required worker reports and conflict resolution, run root-owned acceptance for the combined candidate. Worker-local success is insufficient. Use the existing selected profiles; where several gates are required, retain every required result rather than writing a new composite validator merely for reporting.

Evaluate schema, run/profile/version, every required check, exit/result agreement, raw logs/artifacts, scope and freshness under gate-acceptance.md:

- Pass accepts only the declared checks against the current candidate.
- Fail rejects the candidate.
- InfrastructureError blocks verification without proving candidate failure.

After an input-affecting correction, rerun each complete affected profile, not only the failed check. Follow [remediation-loop.md](remediation-loop.md) for shared budget, same-role corrections and repeated-cause stops. Never drop a failed selected requirement or weaken its validator/fixtures.

## Pinned Review Evidence

Only when requested and supported by existing relevant gates, follow [ReviewEvidence.md](../../../../Tools/NpcHarness/ReviewEvidence.md) exactly:

1. Before implementation, pin the review request and retain its SHA-256 outside worker-controlled evidence. Record actual author IDs, every required gate and applicable rule categories.
2. Cover candidate inputs, dependencies, validators and relevant documents with permitted input roots. Preserve authorized prior rules when normative rules legitimately change.
3. After implementation, run review-snapshot before gates and retain the snapshot hash. Run required gates sequentially on that unchanged candidate.
4. Invoke one independent reviewer using [reviewer-orchestration.md](reviewer-orchestration.md), passing the request/snapshot and primary evidence. Require the compact v2 record from ReviewEvidence.md; this is the same required review, not an additional reviewer.
5. Root persists that record and runs accept-review with retained request/snapshot hashes. Require current Pass/exit 0; an older output never rescues a failed command.

Changed candidates require a fresh run/request/snapshot and all required gates under ReviewEvidence.md. Preserve original requirements and the task-wide retry ledger. Do not replace pinned hashes merely to repair a mismatch.

Hashes bind selected bytes; they do not authenticate agents, prove sufficient coverage or provide filesystem isolation. Check reviewer eligibility against actual collaboration records and preserve root control of trusted hashes.

## Review Intake and Completion

Use the ordinary reviewer identity/primary-evidence/verdict rules in reviewer-orchestration.md. For unpinned selected-gate review, additionally use the existing [review-result-contract.md](../../reviewing-unity-candidate/references/review-result-contract.md): confirm candidateRunId, selected profile/version/status, preserved gate evidence, current reviewed inputs, complete acceptance coverage and severity/verdict consistency.

Pinned review uses compact v2 mappings and accept-review instead of legacy v1 field names. Neither review format replaces required functional gates. Missing, stale, malformed or ineligible review evidence is InsufficientEvidence.

Complete only with current successful required gates, eligible required review approval and all applicable specialized phases. Extend the single task record/report with only the selected protocol's actual profiles/versions, results/log paths, review verdict and any failed-run/correction disposition. Do not duplicate raw evidence or demand these fields in ordinary work.
