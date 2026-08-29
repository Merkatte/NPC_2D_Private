---
name: reviewing-npc-work-code
description: Audits the NPC_Work_2D Unity worker/NPC codebase for code convention compliance, documented Systems ownership, dependency direction, magic numbers, dead code, Unity serialized-reference issues, and architecture drift. Use when asked to review code quality, run a lead-programmer style code audit, update PublicMD/Status/Code_Evaluation_Result.md, or verify worker/NPC code against its owning PublicMD/Systems documents.
---

# Reviewing NPC Work Code

## Workflow

1. Read `PublicMD/ProjectStructure.md` and `references/review-workflow.md`.
2. Classify the requested or changed-file scope with the routing table. Read only the owning and directly affected `PublicMD/Systems` leaf documents; use a feature `README.md` only to select leaves.
3. Read `PublicMD/CodeConvention.md` when C# is in scope. Read `PublicMD/ARCHITECTURE.md` only for cross-system ownership or dependency findings.
4. Inspect only the relevant `.cs` files and direct dependency surfaces. For an explicitly whole-project audit, read all Systems leaves and inspect all production C#.
5. Check scene/prefab serialized references when the selected leaf identifies Unity wiring or a finding depends on it.
6. Update `PublicMD/Status/Code_Evaluation_Result.md` with the review result.

## Review Stance

Act as a senior lead programmer. Prioritize architectural risk, runtime bugs, dependency pollution, responsibility drift, convention violations, hidden magic numbers, stale code, and missing validation.

Do not modify production code unless the user explicitly asks for fixes. The default output is a review report only.

## Output Rules

- Write the full review to `PublicMD/Status/Code_Evaluation_Result.md`.
- Preserve useful prior context only if it is still accurate; otherwise replace stale content.
- Include file paths and concrete evidence.
- Distinguish confirmed issues from cleanup candidates and intentional design notes.
- Summarize what improved since the previous review when prior content is available.
