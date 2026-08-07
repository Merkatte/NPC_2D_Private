---
name: implement-npc-feature
description: Plan and implement Unity NPC or worker C# features in NPC_Work_2D, then delegate a read-only architecture review to an independent Codex agent and update PROGRESS.md. Use for feature implementation, script creation, worker behavior, IAction, selector, action plan, destination provider, movement, stats, manager, or Behavior Graph integration requests. Require Opus Plan Mode, explicit user approval, and Sonnet implementation.
---

# Implement NPC Feature

Follow the phases in order. Do not combine planning and implementation.

## 1. Plan with Opus

1. Capture the requested behavior, boundaries, and observable acceptance criteria.
2. Ensure the current Claude model is Opus and Plan Mode is active. If either condition is false, stop and ask the user to switch; do not imitate the missing mode.
3. Read these project documents in order:
   - `PublicMD/ARCHITECTURE.md`
   - `PublicMD/ProjectStructure.md`
   - `PublicMD/CodeConvention.md`
4. Inspect only the code, scenes, prefabs, tests, and additional specifications relevant to the request.
5. Produce a concrete plan containing scope, responsibility placement, files to create or change, dependency direction, validation, risks, and explicit exclusions.
6. Ask the user to approve the plan. End the turn without editing implementation files.

## 2. Implement with Sonnet

1. Start only after explicit user approval.
2. Ensure the current Claude model is Sonnet. If it is not, stop and ask the user to switch before editing code.
3. Re-read any project document changed since planning.
4. Implement only the approved scope. Preserve the documented architecture and local style.
5. Keep decision policy in selectors, execution lifecycle in actions and plans, shared runtime dependencies in context, destinations in providers, movement in movers, and active-plan lifecycle in `WorkerAI`.
6. Validate in proportion to the change: compile, run relevant tests, and inspect Unity scene or prefab serialized wiring when applicable.
7. Treat implementation as complete only after validation results are known.

## 3. Delegate Review to an Independent Codex Agent

Immediately after implementation validation, launch Codex as a separate background review agent. Do not review on Codex's behalf and do not wait for, poll, or summarize its result.

Read the focused reviewer role, priorities, output format, and tool restrictions in [references/codex-review-agent.md](references/codex-review-agent.md), then run:

```powershell
powershell -ExecutionPolicy Bypass -File "<skill-directory>/scripts/start_codex_review_agent.ps1" `
  -RepoRoot "<project-root>" `
  -ImplementationSummary "<implemented scope and validation summary>" `
  -ChangedFiles "<comma-separated changed files>"
```

The launched Codex process receives its own context, runs with a read-only sandbox, and returns the complete report as its final response. The launcher writes that response to `PublicMD/Code_Evaluation_Result.md`. Continue immediately after a successful launch. If launch fails, record the failure and continue to the progress update.

## 4. Update Progress Immediately

Update `PublicMD/PROGRESS.md` immediately after launching the reviewer. Follow the document's existing format and terminology. Record:

- completed task or slice;
- files changed;
- implementation decisions;
- verification performed and its actual result;
- next actions and blockers;
- that the Codex review agent was launched, but not an unreceived review outcome.

Do not modify `PublicMD/Code_Evaluation_Result.md`; the Codex review agent owns that file.

## 5. Report

Report the implemented scope, validation result, Codex agent launch status, and progress update. Do not claim the asynchronous review passed.
