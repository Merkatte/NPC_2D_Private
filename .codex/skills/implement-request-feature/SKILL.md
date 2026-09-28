---
name: implement-npc-feature
description: Plan NPC or worker features, delegate implementation to separate code, graphics and assembly workers, await independent code review, and update PROGRESS.md. Use for NPC feature or wiring work. Preserve native planning mode, explicit plan approval and a separate implementation phase.
---

# Implement NPC Feature

Follow the phases in order. Do not combine planning and implementation.

This is the canonical NPC implementation workflow. The `.agents` entry delegates
here. Apply `../orchestrate-unity-work/references/worker-coordination.md` for role
ownership/context. Root plans, schedules and verifies; role workers implement.

## 1. Plan

1. Capture the requested behavior, boundaries, and observable acceptance criteria.
2. Apply the planning gate for the active agent environment:
   - In Claude Code, require the Opus model with Plan Mode active.
   - In Codex, require Plan collaboration mode. The current Codex model is acceptable unless the user or project instructions require a specific Codex model.
   - If the applicable planning mode or model requirement is not satisfied, stop and ask the user to switch. Do not imitate a missing mode or another agent's model.
3. Read `PublicMD/ProjectStructure.md`, use its routing table, and then read only the relevant `PublicMD/Systems` leaf documents. When a feature is a folder, read its `README.md` only to select the needed leaf; do not automatically read every sibling leaf.
4. Add conditional project documents only when applicable:
   - game rules or player experience: `PublicMD/Game_Plan.md` and `PublicMD/SPEC.md`;
   - roadmap, prerequisite, or completion status: `PublicMD/PLAN.md` and the selected feature's active document linked under `PublicMD/Plans`;
   - cross-system ownership or dependency changes: `PublicMD/ARCHITECTURE.md`;
   - planned C# creation or modification: `PublicMD/CodeConvention.md`.
5. Follow the selected leaf document's change-routing table and inspect only the relevant code, scenes, prefabs, tests, and additional specifications.
6. Produce a concrete plan containing scope, responsibility placement, files to create or change, dependency direction, validation, risks, and explicit exclusions.
7. Ask the user to approve the plan. End the turn without editing implementation files.

## 2. Implement

1. Start only after explicit user approval.
2. Apply the implementation gate for the active agent environment:
   - In Claude Code, require the Sonnet model.
   - In Codex, require Default collaboration mode. Use the current Codex model unless the user or project instructions require a specific Codex model.
   - If the applicable implementation mode or model requirement is not satisfied, stop and ask the user to switch before editing implementation files.
3. Re-read any project document changed since planning.
4. Delegate approved C# work to `$author-unity-code`, images to `$create-project-sprites`, and Unity wiring to `$assemble-unity-objects` with fresh role contexts and disjoint paths. Parallelize independent preparation; assembly writes follow ready code/art and compilation. Root does not implement role files. Preserve the documented architecture and local style.
5. Keep utility policy in decision code, role priority and queue composition in selectors, selected behavior lifecycle in actions, execution dependencies in `ActionContext`, facility transactions in providers, movement/presentation in `NPCComponent`, and active queue lifecycle in `WorkerNPC`.
6. Validate in proportion to the change: compile, run existing relevant non-Play-Mode tests, and inspect Unity scene or prefab serialized wiring when applicable. Follow `Tools/NpcHarness/SceneWork.md` verification/evidence limits and automatic exclusions: do not write verification-only runners, eval helpers, validators or tests without an explicit user request for verification development. Reuse unaffected checks and one compact task record. Hand off unavailable gameplay/runtime/visual coverage as unverified; its absence alone does not block completion. Actual defects, failed existing checks, compilation and independent review remain mandatory.
7. Require code-worker compile, convention, ownership, dependency and scope evidence. Inspect actual diff and collect common checks or selected gates. This produces a candidate; completion also requires the independent review below.

## 3. Delegate Review to an Independent Codex Agent

After successful current validation, spawn an independent read-only collaboration
agent with `fork_turns="none"`, using `$reviewing-unity-candidate`. Every C# change
requires this review. It must not be an implementation worker; wait for an available
slot if necessary. Do not launch the legacy background CLI reviewer for this workflow.

Read [references/codex-review-agent.md](references/codex-review-agent.md). Supply
original request, approved scope/exclusions, actual diff, relevant rules, current
compile/scope/reference evidence and selected gates if any. Do not pass the whole
conversation, author's reasoning or self-assessed compliance as review evidence.

Wait for the result. Confirm conventions, ownership and dependency coverage using
code/document locations; compile success or reviewer launch alone is insufficient.
Route blocking fixes to the original role or a fresh same-role worker. Reviewer
stays read-only; root does not patch implementation files. Rerun affected checks and
review on a changed candidate within the shared remediation budget. Missing/failed
review remains incomplete. For a selected pinned workflow, also require accept-review.

Store the actual review under the run evidence path. Do not overwrite the separate
repository-wide `PublicMD/Status/Code_Evaluation_Result.md` audit for a bounded review.

## 4. Update Progress

Update `PublicMD/Status/PROGRESS.md` with the actual final or blocked state. Follow its existing format. Record:

- completed task or slice;
- files changed;
- implementation decisions;
- verification performed and its actual result;
- next actions and blockers;
- role/agent ownership, independent reviewer identity, actual verdict and unresolved findings.

Pending review may be recorded as pending, never as accepted. Preserve the separate repository audit report.

## 5. Report

Report the implemented scope, actual checks, independent review outcome, unresolved
items and progress update. Complete only when required evidence and review are present.
