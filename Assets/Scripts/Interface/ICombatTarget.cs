using UnityEngine;

public interface ICombatTarget
{
    bool IsAlive { get; }
    Vector3 Position { get; }
    void ApplyDamage(float amount);
}
