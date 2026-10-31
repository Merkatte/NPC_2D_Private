---
name: implement-npc-feature
description: Select and scope NPC_Work_2D feature work from the user's request and agreed plans, define completion conditions, obtain plan approval, and hand execution to orchestrate-unity-work. Use for choosing the next feature task or preparing a selected one; not for implementation coordination or code audits.
---

# Implement NPC Feature

Decide what feature work to do, why it is needed and where it ends. Use the user's choices and agreed plans as the basis. This is the canonical planning/approval entry; the .agents entry routes here.

[orchestrate-unity-work](../orchestrate-unity-work/SKILL.md) owns how approved work is executed: choosing role skills and workers, allocating paths, scheduling, checking evidence, routing corrections, reviewing and deciding implementation completion. Do not duplicate those procedures here.

For substantive feature planning, preserve the existing native planning gate: Codex requires Plan collaboration mode, using the current model unless explicitly constrained; Claude Code requires Opus with Plan Mode. If the applicable mode/model is missing, request the necessary switch before planning rather than imitating it. An already approved handoff follows the execution gate in section 4 instead of restarting planning.

## 1. Identify What the User Has Already Chosen

Read the current request together with relevant prior agreements. State the interpretation briefly before planning; do not require the user to select a formal mode for these two branches.

| Request/context | Action |
|---|---|
| A feature, behavior or plan item is selected | Clarify that selected task's scope, prerequisites and completion conditions. Do not replace it with a preferred backlog item. |
| No task is selected; the user asks what to do next | Recommend a bounded next task from agreed priorities, unfinished work and prerequisites, explaining why. |
| Only a broad area is named | Narrow choices within that area; ask only when alternatives materially change scope, behavior or authority. |
| "Proceed" follows a concrete agreed task/plan | Use that context. Do not restart task selection or ask for approval already given for the unchanged plan. |

For example, "warehouse capacity first" selects a task; "what next?" asks for a recommendation. "Improve the warehouse" may need a choice of outcome. Identify selected/recommended work in the first substantive sentence so the user can correct a mistaken interpretation.

If the user asks only for advice, explanation or comparison, deliver that without editing implementation or triggering execution. Agreement with a direction alone is not approval of an unspecified implementation scope.

## 2. Check the Plan and Relevant Current State

- For next-task recommendations, read PublicMD/PLAN.md, the relevant active detailed plan it links, and relevant recent PROGRESS.md entries. Check only the code/assets needed to establish a candidate task's completion or prerequisites; do not audit the whole project.
- For a selected task, use applicable plan entries as context without reopening settled priorities. Read ProjectStructure.md and follow folder READMEs recursively to the owning Systems leaf and its minimum change scope.
- Add Game_Plan.md/SPEC.md for gameplay rules or player experience, CodeConvention.md for planned C#, and ARCHITECTURE.md for cross-feature responsibility, shared contracts or dependency changes.
- Prefer actual code/serialized state when status documentation is stale. Distinguish confirmed facts from unresolved items; keep undecided game rules as TBD rather than inventing them.
- Do not read CLAUDE.md or .claude.

If the user-selected task lacks a prerequisite, explain the dependency and propose the smallest prerequisite slice or alternative ordering. Do not silently switch tasks, expand scope or rewrite agreed priorities/game rules. Material changes to those agreements need the user's decision.

## 3. Define a Reviewable Scope and Obtain Approval

Produce one concise scope proposal in the conversation or the existing relevant plan. A new handoff document is not mandatory. Include:

- selected task, original user outcome and its relation to an existing plan when applicable;
- included behavior and explicit exclusions;
- prerequisites, approved game-rule decisions and unresolved questions;
- intended responsibility/dependency boundaries and likely affected code/document/asset paths, based on owning Systems documents;
- observable completion conditions, relevant existing checks and human/unverified QA;
- material risks and the decisions needed before execution.

Describe outcome constraints and affected surfaces, not a worker roster or command-by-command implementation schedule. The orchestrator turns the approved scope into exact assignments and selects the needed skills, agents, tools and verification route. Do not repeat a static NPC class-responsibility catalog here; current Systems leaves own that information.

Use existing verification capabilities under [SceneWork.md](../../../Tools/NpcHarness/SceneWork.md). Planning a feature does not authorize new verification-only code or automatic Play Mode/game-screen checks; those need separate explicit requests. Distinguish human QA from mandatory executable checks.

For a new or materially changed proposal, request explicit approval and end the planning turn without implementation. Preserve approval already given for a concrete unchanged plan. Unresolved choices that determine behavior, scope or acceptance must be settled before execution; routine execution choices belong to the orchestrator.

## 4. Hand the Approved Scope to the Orchestrator

Implementation starts only after explicit plan approval and the existing execution-mode gate: Default collaboration mode in Codex; Sonnet in Claude Code. If the required mode/model is missing, request the switch before implementation. Do not ask the user to approve the same unchanged plan again after switching modes.

Pass the original request, approved scope above, relevant document/plan links, approval evidence and any separately authorized verification to $orchestrate-unity-work. A concise message or reference to the existing plan is sufficient. Recheck only facts/documents that changed since approval.

This is a change of workflow responsibility, not a requirement to spawn a separate "implement" or "orchestrator" agent. The same root can adopt the orchestrator skill and continue authorized execution. This planning skill does not create implementation/review workers, execute their tasks or commission its own second review.

The orchestrator determines the actual role/skill selection, assignment boundaries, parallel/sequential order, tool use, checks and required independent review. It must preserve the approved goal, scope, game rules and completion conditions.

If execution reveals a material scope/behavior/acceptance change, return that specific decision to the user with its impact. Update only the affected part of the plan and obtain approval for that change; do not silently expand work or restart the entire planning process. Ordinary in-scope implementation corrections remain with the orchestrator under its shared budget.

## 5. Carry Completion and Progress Requirements Through the Handoff

The handoff is planning completion, not feature completion. For execution requested by the user, the orchestrator continues through implementation, required checks/review and final reporting; do not stop at delivering a plan once execution is approved and available.

Require the orchestrator to record the actual completed or blocked state in PublicMD/Status/PROGRESS.md using its existing format: scope/files, material decisions, performed/reused checks and log locations, role/reviewer evidence, unverified items and next actions/blockers. Use this as the compact task record where practical or reference the existing record without copying full evidence.

Do not mark a plan item implemented merely because it was selected, scoped, approved or delegated. Report the outcome against its approved completion conditions using the orchestrator's evidence; do not repeat its checks or launch another review to produce a second completion report.

The retained legacy CLI review launchers are not part of this workflow. [The review reference](references/codex-review-agent.md) routes historical links to the current orchestrator-owned procedure.
