---
name: reviewing-economy-domain
description: Assess farming, inventory, cargo, shared resources, storage and trade with their domain UI in NPC_Work_2D. Use for economy quality assessments or assigned transaction review questions, not unrelated consumer-domain spending rules.
---

# Review Economy Domain

Use the [shared assessment criteria](../reviewing-npc-work-code/references/review-workflow.md) and the assigned scope. This skill supplies an area lens, not a separate implementation or verification workflow.

For a direct user request, use the [evaluation coordinator](../reviewing-npc-work-code/SKILL.md) for scope, recording and reassessment, then apply this lens. With an existing bounded evaluation assignment, answer it and return evidence, prior-finding outcomes and coverage gaps to the coordinator; do not restart coordination, spawn more evaluators or edit the shared report.

## Route

Start at [Economy Systems](../../../PublicMD/Systems/Economy/README.md), follow only the relevant subfolder READMEs to owning leaves, then inspect their minimum code/asset scope. The current leaves own detailed rules and file mappings; the questions below are evaluation prompts, not a replacement specification.

## Area Questions

- Follow production through harvest, cargo, deposit and sale: do item identity, capacity and partial acceptance agree across selection and execution?
- Inspect transaction success, rejection, cancellation and repeated requests for loss, duplicate grants or partial resource mutation.
- Compare availability/preflight decisions to execution acceptance so a rejected request cannot endlessly reselect the same impossible operation.
- Check shared resource definitions/state, catalog/CSV boundaries and tuning ownership; distinguish shared storage from domain-specific prices or costs.
- Check warehouse/trade UI sessions and stable item identity against rebinding and resource updates; domain decisions should remain behind their contracts.

## Responsibility Boundary

Economy owns shared resources and its farming/logistics/trade rules, including warehouse/merchant UI ownership per leaf. Construction/recruitment spending rules remain Settlement. NPC supplies common decision/action execution, World common provider contracts, and UI common view lifecycle; inspect their connections only as needed.

Related document entrypoints, only when the assigned question needs them: [Settlement](../../../PublicMD/Systems/Settlement/README.md), [NPC](../../../PublicMD/Systems/NPC/README.md), [World](../../../PublicMD/Systems/World/README.md), [UI](../../../PublicMD/Systems/UI/README.md).

Evaluate and record evidence only. A concrete external issue is passed to its primary evaluator without expanding into an unrelated audit. Requested fixes are handled after evaluation by orchestrate-unity-work, which selects implementation roles.
