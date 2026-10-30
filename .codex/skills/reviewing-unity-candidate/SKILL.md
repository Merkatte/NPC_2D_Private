---
name: reviewing-unity-candidate
description: Independently review one bounded Unity candidate against scope, rules, actual diff and current successful common-check or selected-gate evidence. Cover conventions, ownership and dependencies for C# changes. Do not use for repository-wide audits.
---

# Reviewing Unity Candidate

Independently review one bounded candidate from primary evidence. Root owns reviewer selection, correction routing and completion under [reviewer-orchestration.md](../orchestrate-unity-work/references/reviewer-orchestration.md); this skill owns review judgment and its result.

Use the ordinary review below unless root explicitly selected a strict gate or pinned evidence workflow. Only then read [selected-gate-review.md](references/selected-gate-review.md). Running an existing checker does not itself select a strict result format.

## Boundaries

- Remain read-only and independent of every implementation/remediation worker for this candidate. Do not edit code, assets, settings, documents, tests, gates or evidence.
- Do not perform fixes, direct implementation agents, become a correction worker, or seek the implementer's conclusions. Use the original request and primary artifacts, not WorkerReports, author reasoning, suggested findings or an expected verdict.
- Use read-only inspection. If a focused diagnostic needs output, use a temporary location outside the repository and remove no user data. Do not run project-mutating commands as a reviewer.
- Follow [SceneWork.md](../../../Tools/NpcHarness/SceneWork.md): no new or expanded verification-only code without a separate explicit request, and no automatic Play Mode/game-screen checks or retries without a separate explicit request. Report needed actions to root; this read-only role does not acquire implementation authority.
- Preserve raw check outcomes. Review cannot turn a failed check into success, and compilation alone does not establish runtime behavior or architecture compliance.

Use reviewing-npc-work-code for a repository-wide NPC/worker audit. This skill does not expand a candidate review into that audit or update PublicMD/Status/Code_Evaluation_Result.md.

## 1. Establish the Ordinary Review Basis

Require:

- original request and approved scope, exclusions and acceptance conditions;
- relevant project rules and owning documents;
- Git baseline and actual current diff, including relevant untracked files and pre-existing-change context;
- current successful required common checks, including actual compile command/result/log for C# and applicable scope/reference evidence;
- evidence that those check inputs, dependencies and relevant configuration/environment still match the candidate.

Inspect the actual files and raw results. Reuse successful evidence when it covers the unchanged relevant inputs; do not repeat a check merely because a worker ran it. Ordinary review does not require a GateResult, run ID, per-file hash manifest or accept-review.

If reviewer independence or a required input cannot be established, or required checks are missing, failed, unreadable, inconsistent or stale, return InsufficientEvidence with the specific gap. Do not assume a changed source was compiled by an earlier run. Root repairs evidence or routes corrections; the reviewer does not launch an open-ended verification loop.

## 2. Inspect the Changed Scope

Read [document-review.md](references/document-review.md) for project routing and code-review categories.

1. Compare actual changes with approved paths and baseline. Identify unauthorized changes and omitted relevant files; preserve the distinction between prior user work and this candidate.
2. Check each approved acceptance condition using current check evidence or direct source/serialized inspection. State what the evidence establishes; do not claim runtime execution from a code read.
3. Cover conventions, ownership and dependency direction for every C# change, including helpers. Inspect directly affected callers, dependencies and serialized references only as needed for a concrete concern.
4. Assess affected Unity risks such as lifecycle/state restoration, pooling, transactions, serialization/GUID stability, Editor/runtime boundaries and failure paths. Do not scan every subsystem or request an unrelated regression sweep.
5. Report evidence-backed findings with a code location, serialized object, diff hunk, log or relevant rule. One reviewer covers the applicable categories; do not create additional category reviewers.

A current compile result does not erase an evidenced behavior or ownership defect. Conversely, lack of bespoke automated coverage alone is not evidence of a defect. After a correction, review the current diff and affected reasoning against refreshed required checks, reusing still-current unaffected evidence.

## 3. Decide and Return One Ordinary Record

After the required-input check above:

- **Approve:** required review coverage is sufficient and there are no Critical/Major findings.
- **ChangesRequested:** at least one evidenced Critical/Major defect violates the approved scope, acceptance or applicable project rules.
- **InsufficientEvidence:** a material required condition or review category cannot be assessed from available evidence. Identify what is missing rather than inventing a defect.

Critical includes likely data loss, permission/security breaches, broken builds/loading or serialized-state corruption. Major includes evidenced acceptance/scope violations, likely runtime/Editor defects or broken required integration. Minor covers localized clarity/maintainability issues without demonstrated acceptance failure; it does not block approval. Do not promote preferences or unrelated old debt into blockers.

Keep optional unexecuted behavior checks in remainingRisks as NOT_VERIFIED/NotCovered. This includes excluded automatic play/screen QA and absent bespoke automation. Their absence alone does not prevent Approve when required checks and direct review are sufficient. Missing mandatory compilation or a real acceptance violation cannot be moved into that non-blocking category.

Return one compact ordinary record with the existing fields:

- reviewerId, verdict and reviewedFiles (only files actually inspected);
- candidateBasis: Git baseline/current diff and the check evidence/freshness actually verified;
- ruleCoverage: category/status/evidence for required categories and confirmed acceptance conditions, grouped when the same evidence applies;
- findings: severity/file/evidence/recommendation, with a concrete impact and smallest corrective direction for each defect;
- remainingRisks: unverified behavior and relevant uncertainties; summarize responsibility/dependency changes once here or in the corresponding coverage rows.

Use Satisfied for demonstrated coverage and Violated for an evidenced violation with a matching blocking finding. Use NotCovered for missing required coverage and InsufficientEvidence, unless another established blocking defect already warrants ChangesRequested. Optional automation gaps belong in remainingRisks. NotApplicable requires root's prior permission and an explanation; never use it to omit mandatory C# categories.

Return the record to root without writing files or appending a second verdict/essay. Root waits for the result, persists it if needed, and decides completion. Approval applies only to the reviewed candidate with current required checks.
