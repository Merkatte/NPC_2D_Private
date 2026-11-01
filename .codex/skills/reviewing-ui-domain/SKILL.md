---
name: reviewing-ui-domain
description: Assess common UI input, popup/hover lifecycle, view binding, localization and camera code in NPC_Work_2D. Use for UI quality assessments or assigned UI boundary questions, not domain game-rule ownership or automatic screen verification.
---

# Review UI Domain

Use the [shared assessment criteria](../reviewing-npc-work-code/references/review-workflow.md) and the assigned scope. This skill supplies an area lens, not a separate implementation or verification workflow.

For a direct user request, use the [evaluation coordinator](../reviewing-npc-work-code/SKILL.md) for scope, recording and reassessment, then apply this lens. With an existing bounded evaluation assignment, answer it and return evidence, prior-finding outcomes and coverage gaps to the coordinator; do not restart coordination, spawn more evaluators or edit the shared report.

## Route

Start at [UI Systems](../../../PublicMD/Systems/UI/README.md), follow only the relevant subfolder READMEs to owning leaves, then inspect their minimum code/asset scope. The current leaves own detailed rules and file mappings; the questions below are evaluation prompts, not a replacement specification.

## Area Questions

- Trace open, close, close-cancel, re-open and disable/reuse paths: do visual state, subscriptions and domain session state remain consistent?
- Check source binding, input ownership and event routing for stale sources, duplicate requests and missing unsubscribe/resubscribe paths.
- Check stable interaction identity during list rebinding, drag/drop and asynchronous updates; displayed slot state should not silently change the intended command.
- Verify views consume domain queries/commands without directly mutating resources, residents or facilities; inspect result/failure propagation across the boundary.
- Check localization keys, lookup/fallback and Text/TMP binding, plus camera/UI input blocking, against the relevant leaf. Inspect serialized wiring when needed; do not infer rendered appearance from source alone.

## Responsibility Boundary

Use Systems ownership: warehouse/merchant views are Economy, recruitment/construction/housing views Settlement, and defense HUD Combat. UI can assess their common framework/lifecycle contracts without claiming their domain logic. Common views actually owned by UI remain here.

Related document entrypoints, only when the assigned question needs them: [Economy](../../../PublicMD/Systems/Economy/README.md), [Settlement](../../../PublicMD/Systems/Settlement/README.md), [Combat](../../../PublicMD/Systems/Combat/README.md).

Evaluate and record evidence only. A concrete external issue is passed to its primary evaluator without expanding into an unrelated audit. Requested fixes are handled after evaluation by orchestrate-unity-work, which selects implementation roles.
