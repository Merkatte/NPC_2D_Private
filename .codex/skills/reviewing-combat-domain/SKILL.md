---
name: reviewing-combat-domain
description: Assess targeting, attacks, guard/enemy combat and defense progression in NPC_Work_2D. Use for combat quality assessments or assigned combat review questions, not common NPC queue ownership or general navigation design.
---

# Review Combat Domain

Use the [shared assessment criteria](../reviewing-npc-work-code/references/review-workflow.md) and the assigned scope. This skill supplies an area lens, not a separate implementation or verification workflow.

For a direct user request, use the [evaluation coordinator](../reviewing-npc-work-code/SKILL.md) for scope, recording and reassessment, then apply this lens. With an existing bounded evaluation assignment, answer it and return evidence, prior-finding outcomes and coverage gaps to the coordinator; do not restart coordination, spawn more evaluators or edit the shared report.

## Route

Start at [Combat Systems](../../../PublicMD/Systems/Combat/README.md), follow only the relevant subfolder READMEs to owning leaves, then inspect their minimum code/asset scope. The current leaves own detailed rules and file mappings; the questions below are evaluation prompts, not a replacement specification.

## Area Questions

- Trace perception candidates, target selection and actor-owned target state: are selection policy, query helpers and mutation responsibilities distinct?
- Check target identity and Unity-object liveness through death, disable, pool reuse and cancellation; does movement/attack replan on invalidation?
- Compare acquisition, range, distance and attack execution assumptions; trace damage acceptance and health ownership without inventing balance requirements.
- Check defense facility destruction/repair, wave progression and stop/restart boundaries for continuing stale actions, duplicate transitions or missed cleanup.
- Assess guard/enemy rule differences before recommending shared code; similar action structure alone does not justify merging distinct behavior.

## Responsibility Boundary

Combat owns targeting/attack and role combat/defense rules. NPC owns common queue/action and movement execution; World path queries; Settlement construction/housing contracts. Defense HUD remains domain-owned while common UI behavior can receive UI review.

Related document entrypoints, only when the assigned question needs them: [NPC](../../../PublicMD/Systems/NPC/README.md), [World](../../../PublicMD/Systems/World/README.md), [Settlement](../../../PublicMD/Systems/Settlement/README.md), [UI](../../../PublicMD/Systems/UI/README.md).

Evaluate and record evidence only. A concrete external issue is passed to its primary evaluator without expanding into an unrelated audit. Requested fixes are handled after evaluation by orchestrate-unity-work, which selects implementation roles.
