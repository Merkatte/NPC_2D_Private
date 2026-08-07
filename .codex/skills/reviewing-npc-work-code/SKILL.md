---
name: reviewing-npc-work-code
description: Audits the NPC_Work_2D Unity worker/NPC codebase for code convention compliance, responsibility boundaries, dependency direction, magic numbers, dead code, Unity serialized-reference issues, and architecture drift. Use when asked to review code quality, run a lead-programmer style code audit, update PublicMD/Code_Evaluation_Result.md, or verify that worker actions/selectors/context/data/provider scripts follow PublicMD/CodeConvention.md and PublicMD/ProjectStructure.md.
---

# Reviewing NPC Work Code

## Workflow

1. Read `PublicMD/CodeConvention.md`.
2. Read `PublicMD/ProjectStructure.md`.
3. Read `references/review-workflow.md`.
4. Inspect all relevant `.cs` files under `Assets/Scripts` and `Assets/BehaviorGraph/CustomActionNode`.
5. Check scene/prefab serialized references when findings involve Unity wiring, selector setup, action sets, providers, or managers.
6. Update `PublicMD/Code_Evaluation_Result.md` with the review result.

## Review Stance

Act as a senior lead programmer. Prioritize architectural risk, runtime bugs, dependency pollution, responsibility drift, convention violations, hidden magic numbers, stale code, and missing validation.

Do not modify production code unless the user explicitly asks for fixes. The default output is a review report only.

## Output Rules

- Write the full review to `PublicMD/Code_Evaluation_Result.md`.
- Preserve useful prior context only if it is still accurate; otherwise replace stale content.
- Include file paths and concrete evidence.
- Distinguish confirmed issues from cleanup candidates and intentional design notes.
- Summarize what improved since the previous review when prior content is available.
