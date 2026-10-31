# NPC Review Routing

Execution-stage reviewer selection, primary-evidence handoff, verdict intake and corrections belong to [reviewer-orchestration.md](../../orchestrate-unity-work/references/reviewer-orchestration.md). Review judgment and result formats belong to [reviewing-unity-candidate](../../reviewing-unity-candidate/SKILL.md).

This reference preserves existing links; implement-npc-feature does not launch a separate review or duplicate the orchestrator's procedure. The orchestrator must await the required result before feature completion.

Do not use the retained legacy background CLI launcher or overwrite the repository-wide PublicMD/Status/Code_Evaluation_Result.md for a bounded feature review. Reuse the actual review evidence in the task/progress record.
