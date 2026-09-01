using UnityEngine;

/// <summary>
/// TestOnly health owner for validating Enemy combat. CombatTarget supplies the common targeting
/// and damage gate while this component keeps only the dummy-specific health and death response.
/// </summary>
public class CombatTestDummy : MonoBehaviour, IHealthState
{
    [SerializeField] private float _maxHealth = 10f;

    private CombatTarget _combatTarget;
    private float _health;

    public float CurrentHealth => _health;

    private void Awake()
    {
        _health = _maxHealth;

        if (!TryGetComponent(out _combatTarget))
        {
            _combatTarget = gameObject.AddComponent<CombatTarget>();
        }

        _combatTarget.Initialize(this);
        _combatTarget.Died += HandleDied;
    }

    private void OnDestroy()
    {
        if (_combatTarget)
            _combatTarget.Died -= HandleDied;
    }

    public float ChangeHealth(float amount)
    {
        _health = Mathf.Clamp(_health + amount, 0f, _maxHealth);
        return _health;
    }

    private void HandleDied()
    {
        Destroy(gameObject);
    }
}
