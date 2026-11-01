---
name: reviewing-world-domain
description: Assess navigation, spatial queries and common destination/provider contracts in NPC_Work_2D. Use for World quality assessments or assigned cross-domain query review questions, not role decisions or facility transaction policies.
---

# Review World Domain

Use the [shared assessment criteria](../reviewing-npc-work-code/references/review-workflow.md) and the assigned scope. This skill supplies an area lens, not a separate implementation or verification workflow.

For a direct user request, use the [evaluation coordinator](../reviewing-npc-work-code/SKILL.md) for scope, recording and reassessment, then apply this lens. With an existing bounded evaluation assignment, answer it and return evidence, prior-finding outcomes and coverage gaps to the coordinator; do not restart coordination, spawn more evaluators or edit the shared report.

## Route

Start at [World Systems](../../../PublicMD/Systems/World/README.md), follow only the relevant subfolder READMEs to owning leaves, then inspect their minimum code/asset scope. The current leaves own detailed rules and file mappings; the questions below are evaluation prompts, not a replacement specification.

## Area Questions

- Check grid coordinates, traversal costs, path failure/results and revision/obstacle changes for consistent contracts with movement consumers.
- Check registration/removal and cached handles against scene/facility lifetime so stale providers are not treated as usable solely because they remain referenced.
- Compare option, request, capability/preflight and execution-result contracts across consumers; verify common validation without absorbing domain transaction rules.
- Check whether query helpers mutate selection state or choose role-specific work, and whether domain consumers duplicate an already sufficient common query.
- Distinguish backed Unity-object liveness from interface non-nullness when judging provider/target handles.

## Responsibility Boundary

World owns spatial queries and common destination/provider contracts. NPC owns semantic choice and movement execution. Economy/Settlement/Combat own their concrete transactions and role rules. There is no catch-all Core responsibility for unrelated shared-looking code.

Related document entrypoints, only when the assigned question needs them: [NPC](../../../PublicMD/Systems/NPC/README.md), [Economy](../../../PublicMD/Systems/Economy/README.md), [Settlement](../../../PublicMD/Systems/Settlement/README.md), [Combat](../../../PublicMD/Systems/Combat/README.md).

Evaluate and record evidence only. A concrete external issue is passed to its primary evaluator without expanding into an unrelated audit. Requested fixes are handled after evaluation by orchestrate-unity-work, which selects implementation roles.
