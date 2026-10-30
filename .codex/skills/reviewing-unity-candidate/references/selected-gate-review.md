# Selected Gate and Pinned Review

Read only when root explicitly selects strict GateResult acceptance or a pinned review request/snapshot. Ordinary review uses SKILL.md's common-check evidence and ordinary record.

Retain SKILL.md's independence, read-only scope and primary-evidence rules. Use [document-review.md](document-review.md) for applicable review categories. This route does not authorize new verification tools, automatic Play Mode or game-screen checks.

## Validate the Selected Evidence

Require the original request, approved scope/exclusions/acceptance, relevant rules, actual current diff including relevant untracked files, and every required passing GateResult with raw logs.

Check the selected run/profile/version, required check IDs, timestamps, exit/result agreement, artifacts, changed-file scope and freshness against [gate-acceptance.md](../../orchestrate-unity-work/references/gate-acceptance.md). Supplied hashes bind only their covered inputs; they do not prove sufficient coverage or authenticate the reviewer.

A required non-passing, missing, malformed, mismatched or stale gate means InsufficientEvidence. Preserve its actual status and identify the gap; do not invent a run ID, rewrite a result, switch to ordinary review or use reviewer opinion to rescue a failure. A gate Pass proves only its declared checks; direct review can still find an evidenced defect without claiming that gate failed.

## Choose Exactly One Result Contract

- **Unpinned selected-gate review:** use the existing v1 JSON in [review-result-contract.md](review-result-contract.md). Match candidateRunId and selected gate evidence; cover each acceptance condition under that contract.
- **Pinned request and snapshot:** use the compact v2 record and exact coverage/consistency rules in [ReviewEvidence.md](../../../../Tools/NpcHarness/ReviewEvidence.md). Inspect the root-supplied request, snapshot, retained hashes, rule documents and required passing gates. Do not create, weaken or repin them.

Pinned ruleCoverage must account for every requested rule. NotCovered forbids Approve; NotApplicable needs explicit permission in that request. Unrelated pre-existing debt belongs in remaining risks, while a pre-existing issue explicitly required by acceptance still blocks. Never use ordinary optional-automation reporting to bypass a mandatory pinned requirement.

Return one record to root; root persists it at the selected path and runs accept-review when the pinned protocol requires it. The reviewer does not write evidence files or run that root-owned acceptance step. Do not emit both v1/v2 or add a second narrative verdict.

## Freshness After Corrections

Root routes fixes and shares the task-wide correction budget under [extended-workflows.md](../../orchestrate-unity-work/references/extended-workflows.md). The reviewer remains read-only and receives updated primary evidence.

An input-affecting correction needs complete affected gate profiles, not only the failed check. A pinned candidate change needs the fresh run/request/snapshot and all required gates defined by ReviewEvidence.md. Do not review a changed candidate as approved using an old gate or snapshot. Missing evidence remains a blocker even when correction budget is exhausted.
