using UnityEngine;

/// <summary>
/// TestOnly stand-in target for validating Enemy's own combat AI (IMP-035). Farmer/Guard are
/// deliberately NOT made attackable in this slice - resident death/incapacitation policy
/// (PublicMD/Game_Plan.md GD-008) is not yet decided, so NPCComponent does not implement
/// ICombatTarget. This dummy has the same minimal shape as Enemy.cs's original test-target role:
/// health, IsAlive, ApplyDamage -> Destroy. Once GD-008 is settled and residents become a real
/// combat target, this file can be removed.
/// </summary>
public class CombatTestDummy : MonoBehaviour, ICombatTarget
{
    [SerializeField] private float _maxHealth = 10f;

    private float _health;

    public bool IsAlive => _health > 0f;
    public Vector3 Position => transform.position;

    private void Awake()
    {
        _health = _maxHealth;
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - amount);
        if (_health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
