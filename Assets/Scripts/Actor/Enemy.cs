using UnityEngine;

/// <summary>
/// Minimal ICombatTarget test target. No movement or retaliation AI - only the
/// health/aliveness/damage seam Guard combat validation needs.
/// </summary>
public class Enemy : MonoBehaviour, ICombatTarget
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
