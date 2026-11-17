using UnityEngine;

public interface ICombatTarget
{
    bool IsAlive { get; }
    bool CanBeTargeted { get; }
    Vector3 Position { get; }
    float ApplyDamage(float amount);
}
