using UnityEngine;

/// <summary>
/// Per-NPC combat target state. Owns one reusable CombatTargetHandle instance so a selected
/// target survives perception losing sight of it (GQ-007) until it dies, is destroyed, or the
/// NPC itself is reset. Shared by any actor that engages in combat (Guard, Enemy, ...).
/// </summary>
public class CombatRuntimeState
{
    private readonly CombatTargetHandle _targetHandle = new CombatTargetHandle();

    public bool HasValidTarget => _targetHandle.IsValid;
    public CombatTargetHandle TargetHandle => _targetHandle;

    public void SetTarget(ICombatTarget target, Component owner)
    {
        _targetHandle.Set(target, owner);
    }

    public void ClearTarget()
    {
        _targetHandle.Clear();
    }
}
