---
name: orchestrate-unity-work
description: Orchestrate a bounded Unity repository change through candidate production, selective worker delegation, deterministic gating, independent review when required, and evidence-driven limited remediation. Use for general Unity work that needs Codex to choose the smallest executor and require current GateResult evidence before declaring completion; do not replace the specialized NPC/worker feature implementation workflow.
---

# Orchestrate Unity Work

Act as the root Codex orchestrator. Preserve the user's intent and authority from the current conversation. The harness validates a candidate; it never interprets the request, chooses the implementation, directs workers, or declares the overall task complete.

## Invariants

- Only the root Codex is the orchestrator and final decision-maker.
- A worker's `done` or `success` report submits a candidate. It is not acceptance evidence.
- Direct execution or one worker is the default. Use multiple workers only for at least two genuinely independent workstreams with non-overlapping writable ownership.
- Accept a candidate only from an actual, independent deterministic `GateResult` for the scoped work.
- Do not let the candidate author weaken, replace, or bypass the gate used to accept that candidate.
- Set a remediation budget before verification, never raise it implicitly, and stop when it is exhausted or the same failure cause repeats.
- A reviewer opinion cannot substitute for a deterministic gate.
- A reviewer is a separate read-only agent that did not implement the candidate and was not one of its workers.
- Preserve unrelated pre-existing changes and do not silently expand allowed paths or permissions.

Read [references/gate-acceptance.md](references/gate-acceptance.md) before choosing or evaluating a gate.

## 1. Scope the Run

1. Inspect the current worktree and record the baseline commit plus pre-existing dirty paths.
2. Follow `PublicMD/ProjectStructure.md` to the owning Systems leaf and read only its minimum change scope. Apply the repository's conditional document rules.
3. State the requested outcome, observable acceptance conditions, allowed paths, exclusions, and the deterministic gate profile that can prove those conditions.
4. Set `reviewPolicy` before implementation using [references/reviewer-orchestration.md](references/reviewer-orchestration.md): required by the user or another applicable Skill, required when structural risk is present, or not required for a low-risk candidate fully covered by deterministic checks. Record the reason.
5. Set a non-negative candidate remediation budget before the first gate run. Normally use one or two attempts, choose less for risky mutations, and state the value. Zero disables remediation. Do not increase it without the user's authorization.
6. Ask the user only when a missing choice would materially alter the result or authority. Otherwise choose the narrowest reasonable scope.
7. If no existing deterministic gate can prove the requested outcome, identify that as a gate gap. Do not invent a passing result or call the work complete.

This skill is an acceptance envelope for general Unity work. When `implement-npc-feature` applies to NPC or worker C# feature work, follow that specialized skill's planning, approval, implementation, review, and progress requirements. Do not use this skill to bypass them.

## 2. Choose the Smallest Executor

Choose in this order:

1. Use an existing deterministic tool when it can produce the entire candidate.
2. Let the root Codex implement directly when the change is narrow and one context is sufficient.
3. Delegate to one worker when the task is bounded, its writable paths can be stated precisely, and a separate context provides a concrete benefit.
4. Delegate to multiple workers only when at least two required workstreams are independent, have disjoint writable ownership, and gain meaningful latency or specialization benefits from parallel execution.

Read [references/worker-coordination.md](references/worker-coordination.md) before delegating one or more workers. Give every worker a `WorkerAssignment` and require a `WorkerReport`. The worker may implement and run preliminary checks, but may not set acceptance status or alter the accepting gate.

When the candidate contains one of the established production roles, explicitly instruct the worker to use the matching project Skill:

- `$author-unity-code` for a bounded C# implementation slice;
- `$create-project-sprites` for a bounded raster art slice;
- `$assemble-unity-objects` for a supported Tool-only Unity object slice.

Do not spawn all roles by default. Select only roles required by the scoped outcome. Code and sprite candidates may run in parallel when their writable paths are disjoint and their interface is already fixed; object assembly that consumes either result runs afterward. A role Skill supplies stable worker rules, while the `WorkerAssignment` supplies this run's exact paths, inputs, permissions, and acceptance slice.

Use the available collaboration subagent primitives to spawn and coordinate workers. Do not use tmux panes, terminal scraping, or a worker-managed harness as the control plane. If writable paths overlap, Unity serialization couples the targets, or safe ownership cannot be stated before spawning, use one worker or sequential root-owned slices.

## 3. Establish the Candidate

After direct work or all required worker reports:

1. Inspect the actual worktree, diff, and relevant Unity serialized state; do not rely on reports alone.
2. Compare actual changed paths with the baseline, assignment ownership map, and every report. Detect overlaps, unreported changes, unauthorized paths, and changes left by blocked or failed workers.
3. If a conflict exists, do not run the accepting gate. Preserve the worktree, invalidate affected reports, and resolve under one sequential owner or stop for user direction when resolution changes scope or risks unrelated work.
4. If a required worker is blocked or failed, the combined candidate is incomplete even when other workers report success. Inspect the partial diff and handle it under the coordination reference; do not silently drop the failed workstream.
5. Run proportionate preliminary checks such as compilation or focused tests. These help produce the combined candidate but do not replace the accepting GateResult.
6. Record unresolved items honestly. Worker reports and partial implementations remain candidate inputs, not success.

## 4. Run the Deterministic Gate

After the root has reconciled the actual diff into one conflict-free candidate, invoke one root-owned deterministic acceptance gate appropriate to the full scoped conditions. If several validators are necessary, compose them under the selected profile and one GateResult rather than accepting worker-local results. Use `run-harness.sh` or `run-harness.cmd` for a supported batch profile when the Unity project is closed; use the real Editor entrypoint while it is open. Do not imply that the current HarnessBeacon profiles validate unrelated production work.

Evaluate the emitted result using [references/gate-acceptance.md](references/gate-acceptance.md). `Fail` means the candidate is rejected. `InfrastructureError` means verification is blocked, not that the candidate passed or failed. Any candidate mutation after a result invalidates that result and requires a fresh gate run.

## 5. Handle a Gate Failure

Read [references/remediation-loop.md](references/remediation-loop.md) when a gate returns `Fail` or `InfrastructureError`.

Use only the failed check IDs and their expected, actual, message, and artifact evidence to derive the smallest correction inside the existing scope. Each corrective candidate mutation consumes one remediation attempt and immediately expires every GateResult affected by that mutation. Re-run the complete affected profile; do not accept a selectively rerun check.

Stop without further candidate edits when the budget is exhausted, the same failure cause appears after a correction, or the correction would require new paths, permissions, requirements, or acceptance criteria. Ask the user before any scope or authority expansion.

Do not remediate candidate code or assets in response to `InfrastructureError`. Treat it as a separate verification blocker and follow the infrastructure rules in the remediation reference.

## 6. Run Independent Review When Required

After the current candidate has a valid deterministic `Pass`, follow [references/reviewer-orchestration.md](references/reviewer-orchestration.md) when `reviewPolicy` requires review. Structural risk includes material responsibility or dependency changes, public contracts, Unity lifecycle or state restoration, serialized scene/prefab wiring, persistence or transaction behavior, and changes whose important acceptance conditions are not fully exercised by the gate.

Spawn a separate read-only collaboration agent and explicitly instruct it to use `$reviewing-unity-candidate`. A candidate worker cannot review its own work, and a reviewer cannot later become a remediation worker for that candidate.

Provide only the original request, confirmed scope/exclusions/acceptance conditions, relevant project documents, actual diff including relevant untracked files, and the passing GateResult plus raw logs for this exact candidate. Do not provide worker reports, the implementer's conclusion, suspected findings, proposed verdict, or preferred fix.

Handle the returned `ReviewResult` as follows:

- `Approve`: preserves the deterministic `Pass` and satisfies the review requirement; it never replaces or upgrades the gate.
- `ChangesRequested`: route evidenced blocking findings into the bounded remediation workflow. Any candidate edit expires both the GateResult and ReviewResult; run the complete deterministic gate again, then a fresh review of the new candidate.
- `InsufficientEvidence`: do not complete. Repair only the evidence gap within current authority and rerun review; if the candidate changes, rerun the gate before review.

A missing, malformed, stale, self-reviewing, or contract-inconsistent ReviewResult is `InsufficientEvidence`, not approval.

## 7. Complete or Stop

Declare completion only when all of the following are true:

- the actual candidate matches the authorized scope;
- the expected gate profile and version ran against the current candidate;
- its result satisfies every acceptance rule in the reference;
- when `reviewPolicy` requires review, the current candidate has a contract-valid `Approve` from an eligible reviewer;
- no required evidence is missing;
- no specialized workflow has an outstanding required phase.

Report the changed files, gate profile/version, result status, check summary, artifact/result paths, review policy and latest verdict, initial retry budget, attempts used, and the disposition of each failed run or review. If the gate or required review is missing, failed, or blocked, report that state and the evidence without saying the task is complete.

The separate `$reviewing-unity-candidate` Skill owns review judgment and its result contract; this Skill owns when to call it and how its verdict affects orchestration. A specialized project skill may impose additional reviewer requirements.
