---
name: reviewing-npc-work-code
description: Evaluate NPC_Work_2D code quality across the requested project, domain, feature or change; coordinate domain specialists, record findings and reassess prior issues. Use for detailed assessments or overview prioritization, not implementation or candidate acceptance gates.
---

# Reviewing NPC Work Code

Own assessment scope, evaluator coordination and the consolidated record. Evaluate and record only; implementation skill selection and correction coordination belong to [orchestrate-unity-work](../orchestrate-unity-work/SKILL.md).

## Scope and Depth

- Default to detailed assessment when the target is clear: whole project, selected domains, or a feature/file/change with necessary related code.
- Use overview only to recommend where detailed assessment should begin, especially among unreviewed areas. Inspect main structure and relevant prior records; overview is neither a mandatory first pass nor detailed-review completion.
- A new-code fit check examines the actual change, comparable existing implementations, callers and ownership. A suspected feature problem follows its relevant execution path and separates observed symptoms from established causes.
- Ask only when ambiguity materially changes scope. The target bounds assessment, not necessary cross-boundary reading. Do not turn related-code inspection into an unrelated full audit.
- Read [review-workflow.md](references/review-workflow.md) for shared criteria and evidence limits, and [reporting-and-reevaluation.md](references/reporting-and-reevaluation.md) for prior findings and the final record.

## Route and Assign

Start at [ProjectStructure](../../../PublicMD/ProjectStructure.md), follow relevant Systems READMEs recursively and select owning leaves. Open the minimum code/assets they identify, then expand only for concrete evaluation questions. For whole-project detailed assessment, enumerate the agreed production scope and account for coverage; an overview of every domain does not establish detailed completion.

Use only relevant specialist skills:

| Area | Skill | Ownership starting point |
|---|---|---|
| NPC | [reviewing-npc-domain](../reviewing-npc-domain/SKILL.md) | Common actor lifetime, decision/action execution and presentation |
| Settlement | [reviewing-settlement-domain](../reviewing-settlement-domain/SKILL.md) | Construction, housing and recruitment |
| Economy | [reviewing-economy-domain](../reviewing-economy-domain/SKILL.md) | Production, resources, cargo, storage and trade |
| Combat | [reviewing-combat-domain](../reviewing-combat-domain/SKILL.md) | Targeting, attacks, role combat and defense |
| World | [reviewing-world-domain](../reviewing-world-domain/SKILL.md) | Navigation and common destination/provider contracts |
| UI | [reviewing-ui-domain](../reviewing-ui-domain/SKILL.md) | Common input/display lifecycle, views, localization and camera |

One evaluator may use multiple skills. Do not create a catch-all Core evaluator or instantiate every specialist by default. Use [evaluation-coordination.md](references/evaluation-coordination.md) to decide whether to split work, assign boundary questions and integrate evidence. Small reviews can stay with this evaluator; large independent assessments can use domain agents.

## Complete the Assessment

Inspect primary code/serialized evidence and relevant prior findings. Integrate duplicates, conflicting advice and boundary coverage without repeating every worker's full investigation. Additional investigation addresses a specific unresolved question.

Write one consolidated report, normally [Code_Evaluation_Result.md](../../../PublicMD/Status/Code_Evaluation_Result.md), or the user's requested artifact. Follow the reporting reference for finding identity, dates, reassessment and preserved unreviewed history. Domain workers return findings to the coordinator instead of concurrently editing that report. State actual coverage and remaining gaps; evaluation completion is not absence of defects or completion of fixes.

## Evaluation, Candidate Review and Fixes

- For evaluation-only requests, finish after recording and reporting. Do not modify production code, assets, owning Systems documents or rules to make findings disappear.
- When the user also requested fixes, finish assessment and its record first. Then the root switches to orchestrate-unity-work with the existing report, requested finding/scope and authorization. That role selects implementation workers and manages verification; this evaluator never becomes the fixer or implementation coordinator. No extra handoff document or coordinator agent is required.
- Reuse authorization for unchanged requested fixes. A bug-fix request does not authorize a broad redesign or invention of TBD game rules; resolve only the material out-of-scope decision.
- [reviewing-unity-candidate](../reviewing-unity-candidate/SKILL.md) remains the independent acceptance review for an implementation candidate. A quality assessment does not replace its required current checks/verdict, and using that candidate skill does not automatically trigger this assessment.
