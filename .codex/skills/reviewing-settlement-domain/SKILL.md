---
name: reviewing-settlement-domain
description: Assess construction, housing, recruitment and their domain UI in NPC_Work_2D. Use for settlement quality assessments or assigned settlement review questions, not common NPC spawning or resource storage implementation.
---

# Review Settlement Domain

Use the [shared assessment criteria](../reviewing-npc-work-code/references/review-workflow.md) and the assigned scope. This skill supplies an area lens, not a separate implementation or verification workflow.

For a direct user request, use the [evaluation coordinator](../reviewing-npc-work-code/SKILL.md) for scope, recording and reassessment, then apply this lens. With an existing bounded evaluation assignment, answer it and return evidence, prior-finding outcomes and coverage gaps to the coordinator; do not restart coordination, spawn more evaluators or edit the shared report.

## Route

Start at [Settlement Systems](../../../PublicMD/Systems/Settlement/README.md), follow only the relevant subfolder READMEs to owning leaves, then inspect their minimum code/asset scope. The current leaves own detailed rules and file mappings; the questions below are evaluation prompts, not a replacement specification.

## Area Questions

- Trace construction reservation, work, cancellation and completion: can claims, costs or registrations be duplicated, abandoned or committed out of order?
- Check housing assignment and facility lifetime against entry/exit, upgrades and disappearance; distinguish per-scene occupancy from shared definitions.
- Trace recruitment eligibility, cost, reserved spawn and commit/cancel results across failure paths; identify which owner restores each reservation or resource.
- Check domain UI against actual construction/housing/recruitment contracts, including result propagation and stale sessions, without moving game rules into views.

## Responsibility Boundary

Settlement leads recruitment/construction/housing evaluations. Economy owns shared resource storage/contracts; Settlement owns its spending rules. NPC owns common spawn/pool lifetime, World registration/navigation contracts, and UI common display/input lifecycle. A recruitment UI is not automatically UI-owned code.

Related document entrypoints, only when the assigned question needs them: [Economy](../../../PublicMD/Systems/Economy/README.md), [NPC](../../../PublicMD/Systems/NPC/README.md), [World](../../../PublicMD/Systems/World/README.md), [UI](../../../PublicMD/Systems/UI/README.md).

Evaluate and record evidence only. A concrete external issue is passed to its primary evaluator without expanding into an unrelated audit. Requested fixes are handled after evaluation by orchestrate-unity-work, which selects implementation roles.
