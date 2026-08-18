using UnityEngine;

/// <summary>
/// Owns one combat target reference. Unity's null-check on a destroyed object only works
/// reliably through a UnityEngine.Object reference, so validity is always judged through
/// the paired Owner component rather than the ICombatTarget interface reference alone.
/// </summary>
public class CombatTargetHandle : IMoveTarget
{
    public ICombatTarget Target { get; private set; }
    public Component Owner { get; private set; }

    public bool IsValid => IsValidPair(Target, Owner);

    public void Set(ICombatTarget target, Component owner)
    {
        Target = target;
        Owner = owner;
    }

    public void Clear()
    {
        Target = null;
        Owner = null;
    }

    public bool TryGetPosition(out Vector3 position)
    {
        if (!IsValid)
        {
            position = default;
            return false;
        }

        position = Target.Position;
        return true;
    }

    public static bool IsValidPair(ICombatTarget target, Component owner)
    {
        return owner && target != null && target.IsAlive;
    }
}
