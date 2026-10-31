---
name: orchestrate-unity-work
description: Orchestrate bounded Unity work through separate code, graphics and assembly workers, parallel independent production, scoped verification and independent code review. Use for coordinating Unity repository changes; preserve the specialized NPC feature planning and approval workflow.
---

# Orchestrate Unity Work

Act as the root orchestrator: scope the request, assign roles, inspect actual changes and evidence, and decide completion. A worker's success report submits a candidate; it does not establish acceptance.

## Default and Boundaries

Use the ordinary route below and the common completion contract in [SceneWork.md](../../../Tools/NpcHarness/SceneWork.md). Keep these project constraints:

- Code, graphics and Unity assembly belong to separate role workers, even for small changes or existing authoring-tool execution. Root owns coordination, read-only checks and policy/status documents. Only an explicit user exception changes role boundaries.
- Use existing compile/check commands and direct code/serialized-reference inspection. New or extended verification-only runners, validators, tests and harness adapters require a separate explicit user request.
- Automatic Play Mode and game-screen checks, including retries and dedicated verification code, require a separate explicit user request. Hand off human play/screen QA briefly as unverified.
- Preserve unrelated dirty/unsaved work. Default to the original project; choose isolation only for genuinely large/high-impact work under SceneWork.md. A missing Editor connection does not justify a copy or make bounded YAML editing unavailable.
- Keep one compact task record and link existing raw logs. Do not require duplicate assignments, evidence summaries, whole-diff copies or hash manifests for ordinary work.
- Required compilation, existing checks, actual defects and required independent review remain completion conditions. Missing bespoke automation alone is unverified coverage, not a reason to develop a validator or block completion.

Read [extended-workflows.md](references/extended-workflows.md) only when the user requests extended/pinned evidence or the authorized task explicitly selects a legacy Job/WorkerAssignment v1 or strict GateResult workflow. Running an existing checker does not by itself select that workflow. Keep a selected mandatory check after failure; never switch routes to evade it.

## 1. Establish Scope

1. Record the starting commit, pre-existing dirty paths, requested outcome, acceptance conditions, exact writable paths and exclusions in the task record.
2. Follow PublicMD/ProjectStructure.md and folder READMEs to the owning Systems leaf; read its minimum change scope and applicable conditional documents.
3. Identify relevant existing checks and evidence/log locations. Use the ordinary route unless an extended workflow was explicitly selected.
4. Set review policy and its reason using [reviewer-orchestration.md](references/reviewer-orchestration.md). Every C# change, including helpers, needs independent review. Other work follows request/skill requirements and structural risk.
5. Set one task-wide corrective-attempt budget before verification, normally one or two attempts. Zero disables corrective attempts. Share it across all workers, check failures and review findings; never reset it on reassignment or increase it without user authorization.
6. Resolve missing choices only when they materially affect the requested outcome or authority.

For NPC/worker feature work that requires planning, use [implement-npc-feature](../implement-request-feature/SKILL.md) to establish any missing approved scope before implementation; invoking this orchestrator directly does not bypass that workflow's planning/approval or execution-mode gates. On handoff, consume the approved scope, completion conditions, prerequisites and approval evidence. If approval or a material product decision is missing, return only that gap to the user/planning step; do not restart planning for an unchanged approved scope. This orchestrator owns role/skill selection, worker assignments, tools, scheduling, verification, independent review and completion. Execution choices within scope do not need another planning approval; material scope, behavior or acceptance changes need a specific user decision before affected work.

## 2. Assign the Needed Roles

Read [worker-coordination.md](references/worker-coordination.md) before delegation. It owns role boundaries, WorkerAssignment/WorkerReport contents, handoffs and shared-worktree scheduling.

Select only required roles and give each a bounded ordinary assignment using its matching skill. Use fresh contexts and exclusive writable paths. Run independent production in parallel; sequence dependent assembly and operations on the same Unity Editor. Do not instantiate every role by default or fall back to root implementation when delegation fails.

## 3. Inspect the Candidate and Existing Checks

After required worker reports, reconcile actual status/diff, relevant untracked files and serialized state against the baseline, assignments and reports. Resolve ownership conflicts or required partial failures under worker-coordination.md before final verification.

For the combined candidate, establish the relevant SceneWork.md common evidence:

- scope/diff: authorized changes, preserved prior work and no unexplained paths;
- code: successful compilation plus concrete convention, ownership and dependency-direction evidence;
- edited assets: saved values and changed references read back, GUID/fileID targets checked, and reference family or hierarchy/wiring inspected;
- Unity import/load: actual outcome when available, separately from disk inspection. Record unavailable checks as unverified unless the agreed task requires them.

Reuse successful check evidence when the actual inputs, relevant dependencies, command/configuration and relevant environment are unchanged and the check covers the combined candidate. Root must inspect raw results and corroborate freshness; a worker's claim alone is insufficient. Who ran the check is not a reason to rerun it.

After changes, rerun only affected ordinary checks. For example, a task-record wording edit does not invalidate a compile result; a changed source or compilation dependency does. If relevance or freshness cannot be established, rerun the affected check rather than assuming it remains valid. A selected extended protocol keeps its own stricter rerun requirements.

For a failed check, blocked verification or blocking review finding, read [remediation-loop.md](references/remediation-loop.md). Route justified corrections to the owning role under the shared budget. Stop repeated causes instead of trying variants indefinitely.

## 4. Obtain Required Independent Review

After current required checks succeed, follow [reviewer-orchestration.md](references/reviewer-orchestration.md). Use one eligible read-only reviewer for the applicable categories and wait for its actual result.

Supply primary evidence: original request, approved scope/acceptance, relevant rules, actual diff and current successful check results. Ordinary review uses the ordinary record in reviewing-unity-candidate; no gate/hash envelope is needed.

Approval applies only to the reviewed current candidate. Blocking findings require bounded same-role correction, affected check refresh and fresh review. Missing or stale evidence is not approval.

## 5. Complete or Report the Blocker

Complete only when actual changes match the authorized scope, required roles have finished, current required checks succeed, required review approves the current candidate and specialized workflow phases are complete.

Keep the ordinary final report to:

- what changed and any material responsibility/dependency changes;
- checks actually performed or reused, their results and useful log locations;
- review verdict, or the recorded reason review was not required;
- unverified behavior, remaining risks or blockers.

Keep scope/baseline and any corrective-attempt history in the same task record. Report exhaustion or repeated failure clearly when stopping. Do not add unused gate/profile/hash fields to ordinary reports or commission another review merely to fill a handoff.

For implement-npc-feature handoffs, update PublicMD/Status/PROGRESS.md with the actual final or blocked state against the approved completion conditions, including changed files, material decisions, check/log and role/reviewer evidence, unverified items and next actions. Use the existing task record/evidence rather than creating a second review or acceptance report. Selection, approval and delegation alone do not complete a feature.
