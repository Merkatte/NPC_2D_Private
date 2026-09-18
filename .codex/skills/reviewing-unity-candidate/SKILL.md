---
name: reviewing-unity-candidate
description: Independently review one bounded Unity candidate against its confirmed scope, acceptance criteria, actual diff, and passing deterministic gate evidence. Use after implementation and gating; do not use for repository-wide or NPC/worker architecture audits.
---

# Reviewing Unity Candidate

Act as an independent, read-only reviewer of one candidate change. Judge the candidate from primary evidence, not from the implementer's confidence or conclusion.

## Required Input

Require all of the following:

- the user's original request;
- the confirmed scope, exclusions, and acceptance conditions;
- the relevant project documents;
- the actual candidate diff, including relevant untracked files;
- the passing `GateResult` for this exact candidate and its essential raw logs.

Do not request or accept the implementer's conclusion as review evidence. If a required input is absent, unreadable, internally inconsistent, or stale relative to the diff, return `InsufficientEvidence` without guessing.

Read [references/review-result-contract.md](references/review-result-contract.md) before reviewing or emitting a result.

When supplied a pinned review request and snapshot, use the compact v2 record in
`Tools/NpcHarness/ReviewEvidence.md` and read
[references/document-review.md](references/document-review.md). This is the same
independent review, not an additional review pass. Never create, weaken, or repin its
request/snapshot. Return only one compact record; the root persists it.

## Boundaries

- Remain read-only. Do not edit code, assets, scenes, project settings, documentation, the gate, or the candidate.
- Use read-only inspection commands. If a focused diagnostic needs output, write it only to a temporary location outside the repository and remove no user data.
- Do not remediate findings, expand scope, contact the implementer for a conclusion, or direct another agent to modify the candidate.
- Preserve the deterministic result as independent evidence. Never rewrite, weaken, override, or substitute for its status.
- A passing gate proves only its declared checks. You may still request changes for an evidenced scope, integration, maintainability, or Unity risk outside those checks, but must not call the deterministic gate failed.
- A non-passing, malformed, mismatched, or stale gate yields `InsufficientEvidence`; reviewer opinion cannot rescue it.

This skill reviews one bounded candidate after its gate. Use `reviewing-npc-work-code` instead for a broad NPC/worker architecture or convention audit, especially when the requested deliverable is `PublicMD/Status/Code_Evaluation_Result.md`. This skill never updates that report.

## Review Method

1. Establish the candidate boundary from the confirmed scope and actual diff. Flag unauthorized paths, omitted relevant untracked files, and unrelated mutations.
2. Confirm the gate run ID, profile/version, changed files, timestamps, checks, exit/log agreement, and artifacts apply to the current candidate. Prefer a candidate digest when supplied; otherwise corroborate that the worktree has not changed since the gate and disclose the weaker freshness evidence. Record the gate's `Pass` unchanged.
3. Trace every acceptance condition to either deterministic gate evidence or direct review evidence. Missing coverage is not an implicit pass.
4. Inspect only relevant documents, changed files, and direct dependency or serialized wiring surfaces needed to test a concrete concern.
5. Review Unity-specific risks proportionately: lifecycle and state restoration, serialization/GUID stability, scene or prefab wiring, Editor/runtime boundaries, compile/platform compatibility, failure handling, and false-positive tests.
6. Report only evidence-backed findings. Prefer a precise function, serialized object, diff hunk, log entry, or gate check over generalized advice.
7. Emit exactly one `ReviewResult` using the referenced contract. Do not append an informal second verdict.

## Decision Rules

- `Approve`: evidence is sufficient and there are no `Critical` or `Major` findings.
- `ChangesRequested`: at least one evidence-backed `Critical` or `Major` finding exists.
- `InsufficientEvidence`: a required input or trustworthy passing gate for the exact candidate is missing, invalid, inconsistent, or stale.

`Minor` findings are non-blocking. Do not inflate style preferences into blocking findings. Every blocking finding must include its severity, concrete evidence, affected file, and actionable recommendation.

If an acceptance condition is visibly violated, mark it `Violated`, record a `Major` or `Critical` finding, and use `ChangesRequested`. If it is satisfied, mark it `Satisfied`. If it merely lacks trustworthy evidence, mark it `NotCovered` and use `InsufficientEvidence` without inventing a defect.
