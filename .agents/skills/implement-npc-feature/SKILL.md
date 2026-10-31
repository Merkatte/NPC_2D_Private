---
name: implement-npc-feature
description: Route NPC_Work_2D task selection, scope planning and explicit approval to the canonical planning skill, then hand execution to orchestrate-unity-work.
---

# Implement NPC Feature

Read [the canonical planning workflow](../../../.codex/skills/implement-request-feature/SKILL.md). It distinguishes a user-selected task from a request for a next-task recommendation, defines scope/completion conditions, preserves native planning and explicit approval, and hands approved execution to orchestrate-unity-work.

Do not maintain a second planning, approval, worker or reviewer policy here. Orchestration owns execution and completion; this entry does not delegate implementation or run reviews itself.

The retained scripts/start_codex_review_agent.ps1 is historical and is not an execution path for this workflow.
