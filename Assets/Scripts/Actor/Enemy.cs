using UnityEngine;

/// <summary>
/// ICombatTarget adapter over EnemyStat. Owns no health of its own - EnemyStat.GetCurrentHealth
/// is the single source of truth so Guard's damage and EnemyActionSelector's own logic never
/// see two different health values for the same actor. Init(EnemyStat) must be called (by the
/// spawner, with the same instance handed to WorkerNPC.Init) before this is usable; an
/// un-initialized Enemy reports IsAlive == false.
/// </summary>
public class Enemy : MonoBehaviour, ICombatTarget
{
    private EnemyStat _stat;

    public bool IsAlive => _stat != null && _stat.GetCurrentHealth > 0f;
    public Vector3 Position => transform.position;

    public void Init(EnemyStat stat)
    {
        if (stat == null)
        {
            Debug.LogError("Enemy.Init called with a null EnemyStat.", this);
            return;
        }

        _stat = stat;
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return;
        }

        if (_stat.ChangeHealth(-amount) <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
