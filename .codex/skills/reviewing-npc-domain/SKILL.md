---
name: reviewing-npc-domain
description: Assess common NPC lifetime, decision and action execution, movement execution, pooling and presentation in NPC_Work_2D. Use for NPC quality assessments or assigned NPC review questions, not role-specific economy, settlement or combat rules.
---

# Review NPC Domain

Use the [shared assessment criteria](../reviewing-npc-work-code/references/review-workflow.md) and the assigned scope. This skill supplies an area lens, not a separate implementation or verification workflow.

For a direct user request, use the [evaluation coordinator](../reviewing-npc-work-code/SKILL.md) for scope, recording and reassessment, then apply this lens. With an existing bounded evaluation assignment, answer it and return evidence, prior-finding outcomes and coverage gaps to the coordinator; do not restart coordination, spawn more evaluators or edit the shared report.

## Route

Start at [NPC Systems](../../../PublicMD/Systems/NPC/README.md), follow only the relevant subfolder READMEs to owning leaves, then inspect their minimum code/asset scope. The current leaves own detailed rules and file mappings; the questions below are evaluation prompts, not a replacement specification.

## Area Questions

- Trace queue completion, failure, cancellation and re-selection: is common runtime taking over semantic decisions that belong to selectors or execution rules that belong to actions?
- Check reused actors/actions for stale state and ownership of cleanup, including interrupted actions and spawn/return boundaries.
- Check movement execution against navigation results and dynamic targets; distinguish path computation from path following and role-specific destination choice.
- Check whether adapters and messages consume state without selecting work, and whether shared definitions leak mutable state between actors.

## Responsibility Boundary

Role-specific farming belongs to Economy, building/recruitment to Settlement, and guard/enemy combat to Combat. World owns path queries and common destination/provider contracts; NPC owns common execution/decision mechanisms. Follow actual leaf ownership for individual classes and assign interface questions explicitly.

Related document entrypoints, only when the assigned question needs them: [Economy](../../../PublicMD/Systems/Economy/README.md), [Settlement](../../../PublicMD/Systems/Settlement/README.md), [Combat](../../../PublicMD/Systems/Combat/README.md), [World](../../../PublicMD/Systems/World/README.md).

Evaluate and record evidence only. A concrete external issue is passed to its primary evaluator without expanding into an unrelated audit. Requested fixes are handled after evaluation by orchestrate-unity-work, which selects implementation roles.
