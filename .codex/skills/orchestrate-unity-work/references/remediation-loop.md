# Bounded Remediation

Use this reference after a deterministic gate did not return `Pass`, or after a required independent review returned `ChangesRequested`.

## Retry Record

Keep a compact run record containing:

- the initial candidate remediation budget and attempts used;
- each GateResult path, profile/version, and status;
- for each `Fail`, the failed check IDs and their evidence;
- each required ReviewResult and any blocking finding IDs;
- the correction made, changed paths, and resulting failure signature;
- the final disposition: passed, exhausted, repeated cause, infrastructure blocked, or user authority required.

One remediation attempt means one corrective mutation of the candidate, whether prompted by a failed gate or blocking review finding and whether performed by the root or one worker. Preliminary investigation, an evidence-only repair, and a gate or review rerun without candidate mutation do not consume a candidate remediation attempt. Never reset or silently increase the shared budget because a new candidate was produced.

## Candidate Fail

For `status: Fail`:

1. Confirm the result belongs to the current candidate and selected profile.
2. Form a failure signature from the profile/version, sorted failed check IDs, and the materially relevant actual values or error category. Messages and paths may be normalized only to remove volatile timestamps or temporary locations.
3. Compare the signature with earlier post-correction failures. If the same cause has returned, stop even when budget remains. Preserve the evidence rather than trying variants blindly.
4. If no attempts remain, stop and report exhaustion.
5. Derive the smallest correction justified by failed checks. Passing checks and unrelated cleanup do not authorize extra edits.
6. Before editing, confirm the correction stays within the original writable paths, requirements, and permissions. If not, stop and request the needed user decision or authority.
7. Apply one correction and increment attempts used exactly once.
8. Mark the previous result expired, inspect the new candidate, and run the complete affected gate profile again.

Do not weaken expected values, remove failing checks, change fixtures, or edit the accepting validator as remediation. A harness change is a separate scoped task with independent self-tests.

## Review ChangesRequested

For a contract-valid required review with `verdict: ChangesRequested`:

1. Confirm the `candidateRunId` matches the current passing GateResult and the reviewer was not a candidate worker.
2. Use only `Critical` or `Major` findings marked blocking. Form a repeat signature from finding severity, affected file, concrete defect category, and relevant acceptance condition.
3. Stop when the same blocking cause returns after correction or no attempts remain.
4. Derive the smallest correction supported by finding evidence; do not copy a reviewer recommendation beyond the confirmed scope or authority.
5. Apply one correction and increment attempts used once.
6. Expire both the GateResult and ReviewResult immediately.
7. Inspect the new candidate, rerun the complete deterministic gate, and obtain a new independent review only after `Pass`.

The reviewer remains read-only and cannot perform this correction. Do not change review criteria or omit the triggering acceptance condition to obtain approval.

## Infrastructure Error

For `status: InfrastructureError`:

- do not infer a defect in the candidate;
- do not modify candidate code or assets from this evidence;
- do not consume the candidate remediation budget;
- identify the environment or verifier blocker and preserve its logs;
- correct it only when the action is already authorized and does not expand scope;
- rerun the same gate after the blocker is actually resolved.

If resolving the blocker needs new permission, an external action, or a scope change, stop and ask the user. If the cause is unknown or the same infrastructure blocker recurs after one corrective action, stop rather than loop.

A missing or malformed GateResult is likewise a verification/evidence blocker, not proof of a candidate defect. Preserve available logs and repair the result-producing path without editing the candidate.

## Freshness

Every candidate mutation invalidates all GateResults and ReviewResults whose inputs could be affected, including a previous `Pass` or `Approve`. Gate evidence becomes current again only after the complete relevant profile runs against the new candidate. When review is required, review evidence becomes current only after that new pass is independently reviewed. Changing the gate, its expected values, fixtures, review policy, or review criteria never refreshes acceptance for the candidate that motivated that change.
