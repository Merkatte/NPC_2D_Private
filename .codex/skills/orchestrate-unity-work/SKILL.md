---
name: orchestrate-unity-work
description: Orchestrate bounded Unity work through separate code, graphics and assembly workers, parallel independent production, scoped verification and independent code review. Use for coordinating Unity repository changes; preserve the specialized NPC feature planning and approval workflow.
---

# Orchestrate Unity Work

Act as the root Codex orchestrator. Preserve the user's intent and authority from the current conversation. The harness validates a candidate; it never interprets the request, chooses the implementation, directs workers, or declares the overall task complete.

## Invariants

- Only the root Codex is the orchestrator and final decision-maker.
- A worker's `done` or `success` report submits a candidate. It is not acceptance evidence.
- Delegate each required implementation role to its own worker, even for small changes or existing authoring-tool execution. The root coordinates, checks evidence and maintains workflow documents; it does not author code, art or Unity wiring unless the user explicitly requests an exception.
- Start workers and reviewers with `fork_turns="none"`; supply bounded role context. Never reuse an agent across roles. Parallelize independent work with non-overlapping writable paths.
- In a selected gate workflow, require an actual independent deterministic `GateResult`; ordinary work requires the common evidence contract below.
- Do not let the candidate author weaken, replace, or bypass the gate used to accept that candidate.
- Set a remediation budget before verification, never raise it implicitly, and stop when it is exhausted or the same failure cause repeats.
- A reviewer opinion cannot substitute for a selected mandatory deterministic gate or missing common checks.
- A reviewer is a separate read-only agent that did not implement the candidate and was not one of its workers.
- Preserve unrelated pre-existing changes and do not silently expand allowed paths or permissions.

## Ordinary Production Work — Default Route

For ordinary scoped code/art/scene work, use `Tools/NpcHarness/SceneWork.md` as the
common completion contract. This route permits small, scoped scene/prefab YAML edits,
connected Unity Editor tools and explicitly authorized Editor API helpers.
YAML file editing does not require an Editor connection. Prioritize code compilation,
conventions, responsibility and dependency boundaries; do not weaken these checks.
Use each role's ordinary-work branch; do not force these candidates into legacy
WorkerAssignment v1 or the TestOnly adapter.

Default to direct work in the original project. Reserve SceneWorkspace copies for
genuinely large/high-impact changes under SceneWork.md, stating the reason before
copying. A missing open-Editor execution connection is not permission to substitute
a copy for ordinary work. Preserve existing dirty/unsaved changes; do not auto-commit,
stash or revert. The same mandatory common checks apply to direct and isolated work.

The user-approved lightweight route does not require a new task-specific acceptance
profile or proof of every gameplay outcome. Collect current scope/diff, compile/import,
edited-reference and concise convention/pattern evidence. Required checks remain
mandatory; reuse existing relevant checks and report unverified behavior accurately.
Follow specialized NPC planning/approval. Apply SceneWork.md's automatic verification
exclusions: do not run/retry Play Mode or game-screen checks, or author dedicated
checks for them, unless the user separately and explicitly requests that automation.
Hand off human play/screen QA briefly; its absence alone does not block implementation
completion. Existing plan/skill QA items do not authorize automatic execution.

The strict GateResult pipeline below applies when the root selects an existing
profile, legacy Job/WorkerAssignment v1, the pinned review-evidence workflow, or an
explicit extended-verification request. For ordinary work without that selection,
the common checks/record replace the mandatory profile/v1 envelope, not scope safety
or truthfulness. Set this choice before work; never drop a failed selected check.
Missing bespoke functional coverage alone is not a reason to build more harness.

Read [references/gate-acceptance.md](references/gate-acceptance.md) before choosing or evaluating a gate.

## 1. Scope the Run

1. Inspect the current worktree and record the baseline commit plus pre-existing dirty paths.
2. Follow `PublicMD/ProjectStructure.md` to the owning Systems leaf and read only its minimum change scope. Apply the repository's conditional document rules.
3. State the requested outcome, observable acceptance conditions, allowed paths, exclusions, and verification route: ordinary common checks or a selected existing gate profile.
4. Set `reviewPolicy` before implementation using [references/reviewer-orchestration.md](references/reviewer-orchestration.md): required for every C# change (including helpers), by the user/another Skill, or by structural risk. Only non-code low-risk candidates may omit independent review. Record the reason.
   For a selected pinned workflow, use `Tools/NpcHarness/ReviewEvidence.md` and pin
   the review request before implementation. Ordinary work uses common-check evidence
   and candidate hashes with the ordinary reviewing-unity-candidate record.
   Choose a few applicable review categories, not every convention sentence. Preserve
   specialized NPC planning/approval; this envelope does not replace that workflow.
5. Set a non-negative candidate remediation budget before the first gate run. Normally use one or two attempts, choose less for risky mutations, and state the value. Zero disables remediation. Do not increase it without the user's authorization.
6. Ask the user only when a missing choice would materially alter the result or authority. Otherwise choose the narrowest reasonable scope.
7. If a selected mandatory deterministic check has no executable gate, identify that as a gate gap. Do not invent a passing result or call that verification complete. Excluded human play/screen QA is not a missing mandatory gate; apply the ordinary completion contract.

This skill is an acceptance envelope for general Unity work. When `implement-npc-feature` applies to NPC or worker C# feature work, follow that specialized skill's planning, approval, implementation, review, and progress requirements. Do not use this skill to bypass them.

## 2. Assign Separate Roles

Apply [references/worker-coordination.md](references/worker-coordination.md):

1. Identify required code, graphics and assembly roles; create only those workers.
2. Assign exclusive paths and stable interfaces/asset paths before starting parallel writers. Existing deterministic authoring tools are executed by the owning role, not the root.
3. Run independent code and graphics work concurrently. Assembly may inspect and prepare concurrently, but waits for referenced code/art and successful compilation before writes/imports.
4. Schedule Unity import, compilation-triggering operations and saves sequentially. Preserve the user's state. If live auto-import prevents safe parallel writes, use assigned staging paths and explicit handoff.
5. Keep the root on coordination, read-only inspection, verification commands and workflow/status documentation. Route missing code or fixes back to a code worker, never to assembly or graphics.

Read [references/worker-coordination.md](references/worker-coordination.md) before delegating one or more workers. Give every worker a `WorkerAssignment` and require a `WorkerReport`. The worker may implement and run preliminary checks, but may not set acceptance status or alter the accepting gate.

When the candidate contains one of the established production roles, explicitly instruct the worker to use the matching project Skill:

- `$author-unity-code` for a bounded C# implementation slice;
- `$create-project-sprites` for a bounded raster art slice;
- `$assemble-unity-objects` for a scoped Unity object slice (bounded YAML or Editor
  API for ordinary work; registered Tools only for a selected legacy Job).

Do not spawn all roles by default. Select only roles required by the scoped outcome. Code and sprite candidates may run in parallel when their writable paths are disjoint and their interface is already fixed; object assembly that consumes either result runs afterward. A role Skill supplies stable worker rules, while the `WorkerAssignment` supplies this run's exact paths, inputs, permissions, and acceptance slice.

Use the available collaboration subagent primitives with `fork_turns="none"`. Do not use tmux panes, terminal scraping, or a worker-managed harness as the control plane. If paths overlap or Unity serialization couples the targets, schedule same-role owners sequentially. Do not merge roles or fall back to root implementation. If delegation is unavailable, report the missing capability; a direct-implementation exception needs an explicit user request.

## 3. Establish the Candidate

After all required worker reports:

1. Inspect the actual worktree, diff, and relevant Unity serialized state; do not rely on reports alone.
2. Compare actual changed paths with the baseline, assignment ownership map, and every report. Detect overlaps, unreported changes, unauthorized paths, and changes left by blocked or failed workers.
3. If a conflict exists, do not run the accepting gate. Preserve the worktree, invalidate affected reports, and resolve under one sequential owner or stop for user direction when resolution changes scope or risks unrelated work.
4. If a required worker is blocked or failed, the combined candidate is incomplete even when other workers report success. Inspect the partial diff and handle it under the coordination reference; do not silently drop the failed workstream.
5. Check each code worker's actual compile result and evidence for conventions, ownership, dependency direction and path scope. A "followed the docs" statement or compiler success alone is insufficient. Read actual code/diff as needed; run proportionate verification commands. These checks do not replace a selected accepting GateResult or independent code review.
6. Record unresolved items honestly. Worker reports and partial implementations remain candidate inputs, not success.

## 4. Run the Deterministic Gate

This section's GateResult pipeline applies only to a selected gate workflow. For
ordinary work without a profile, collect current SceneWork.md common checks and
continue to independent review in section 6; do not create a gate just for a record.

After the root has reconciled the actual diff into one conflict-free candidate, invoke one root-owned deterministic acceptance gate appropriate to the full scoped conditions. If several validators are necessary, compose them under the selected profile and one GateResult rather than accepting worker-local results. Use `run-harness.sh` or `run-harness.cmd` for a supported batch profile when the Unity project is closed; use the real Editor entrypoint while it is open. Do not imply that the current HarnessBeacon profiles validate unrelated production work.

For a pinned review request, run `review-snapshot` before these gates and retain its
hash outside worker-controlled evidence. Use roots covering the candidate, dependencies,
validators and relevant documents. Run gates sequentially without candidate writes.
The evidence envelope may list several existing gates; it does not create missing
functional coverage. A changed candidate needs a fresh snapshot/run and complete
required gates; preserve the previous request's requirements and retry ledger.

Evaluate the emitted result using [references/gate-acceptance.md](references/gate-acceptance.md). `Fail` means the candidate is rejected. `InfrastructureError` means verification is blocked, not that the candidate passed or failed. Any candidate mutation after a result invalidates that result and requires a fresh gate run.

## 5. Handle a Gate Failure

Read [references/remediation-loop.md](references/remediation-loop.md) when a gate returns `Fail` or `InfrastructureError`.

Use only the failed check IDs and their expected, actual, message, and artifact evidence to derive the smallest correction inside the existing scope. Assign it to the original role worker or a fresh same-role worker; the root and reviewer do not patch implementation files. Each corrective candidate mutation consumes one remediation attempt and immediately expires every GateResult affected by that mutation. Re-run the complete affected profile; do not accept a selectively rerun check.

Stop without further candidate edits when the budget is exhausted, the same failure cause appears after a correction, or the correction would require new paths, permissions, requirements, or acceptance criteria. Ask the user before any scope or authority expansion.

Do not remediate candidate code or assets in response to `InfrastructureError`. Treat it as a separate verification blocker and follow the infrastructure rules in the remediation reference.

## 6. Run Independent Review When Required

After selected gates pass (or ordinary common checks succeed when no profile was selected), follow [references/reviewer-orchestration.md](references/reviewer-orchestration.md). Every C# change requires independent review of conventions, ownership and dependency direction. Structural risk also includes public contracts, Unity lifecycle/state restoration, serialized wiring, persistence and transactions. Missing automatic play/screen QA is excluded under SceneWork.md.

Spawn a separate read-only collaboration agent with `fork_turns="none"` and explicitly instruct it to use `$reviewing-unity-candidate`. Wait for and inspect its result before completion. A candidate worker cannot review its own work, and a reviewer cannot later become a remediation worker for that candidate. If all slots are occupied, wait for a worker to finish; never repurpose that worker as reviewer.

Provide only the original request, confirmed scope/exclusions/acceptance conditions, relevant project documents, actual diff including relevant untracked files, and current gate/common-check evidence. Do not provide worker reports, the implementer's conclusion, suspected findings, proposed verdict, or preferred fix.

Handle the returned `ReviewResult` as follows:

- `Approve`: satisfies review only for the unchanged candidate with current successful common checks or selected gates; it cannot replace verification.
- `ChangesRequested`: route evidenced blocking findings to a same-role worker within the remediation budget. Candidate edits expire affected verification and review evidence. Refresh affected ordinary common checks and hashes, or rerun complete selected gate profiles, then obtain a fresh review.
- `InsufficientEvidence`: do not complete. Repair the evidence gap within current authority and rerun review; candidate changes require refreshed verification first under the chosen route.

A missing, malformed, stale, self-reviewing, or contract-inconsistent ReviewResult is `InsufficientEvidence`, not approval.

## 7. Complete or Stop

Declare completion only when all of the following are true:

- the actual candidate matches the authorized scope;
- selected required gates ran against the current candidate and satisfy the acceptance reference, or the ordinary route has current successful common-check evidence;
- each implementation path has a role owner and each C# change has compile/convention/ownership/dependency evidence;
- when `reviewPolicy` requires review, the current candidate has a contract-valid `Approve` from an eligible reviewer;
- no required evidence is missing;
- no specialized workflow has an outstanding required phase.

For the lightweight review workflow, also require current `accept-review` Pass/exit 0.
Do not accept an old result file when the current command failed. This checks evidence
identity/completeness, not the truth of AI reasoning. Confirm the actual reviewer's
identity against collaboration records; JSON identity fields are not authentication.

Keep the handoff short: changed responsibilities, changed dependencies, verified scope,
and remaining risks. Reuse the single review's evidence; do not commission extra reviews
or a second essay just to fill the handoff. Report existing unrelated debt separately.

Report the changed files, gate profile/version, result status, check summary, artifact/result paths, review policy and latest verdict, initial retry budget, attempts used, and the disposition of each failed run or review. If the gate or required review is missing, failed, or blocked, report that state and the evidence without saying the task is complete.

The separate `$reviewing-unity-candidate` Skill owns review judgment and its result contract; this Skill owns when to call it and how its verdict affects orchestration. A specialized project skill may impose additional reviewer requirements.
