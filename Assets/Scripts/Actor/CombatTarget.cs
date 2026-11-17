using System;
using UnityEngine;

/// <summary>
/// Combat-facing adapter for any damageable scene object. Targeting state belongs here while
/// the actual health value remains in the injected runtime health owner.
/// </summary>
public sealed class CombatTarget : MonoBehaviour, ICombatTarget
{
    [SerializeField] private Transform _targetPoint;
    [SerializeField] private bool _targetableOnInitialize = true;
    [SerializeField] private bool _invulnerableOnInitialize;

    private IHealthState _healthState;
    private bool _isTargetable;
    private bool _isInvulnerable;
    private bool _hasRaisedDied;

    public uint LifetimeVersion { get; private set; }

    public event Action<float> Damaged;
    public event Action Died;

    public bool IsAlive => _healthState != null && _healthState.CurrentHealth > 0f;
    public bool CanBeTargeted => isActiveAndEnabled && _isTargetable && IsAlive;
    public bool IsInvulnerable => _isInvulnerable;
    public Vector3 Position => _targetPoint ? _targetPoint.position : transform.position;

    public void Initialize(IHealthState healthState)
    {
        LifetimeVersion++;
        if (healthState == null)
        {
            Debug.LogError($"CombatTarget '{name}' cannot initialize without an IHealthState.", this);
            _healthState = null;
            _isTargetable = false;
            return;
        }

        _healthState = healthState;
        _isTargetable = _targetableOnInitialize;
        _isInvulnerable = _invulnerableOnInitialize;
        _hasRaisedDied = false;
    }

    private void OnDisable()
    {
        LifetimeVersion++;
    }

    public void SetTargetable(bool isTargetable)
    {
        _isTargetable = isTargetable;
    }

    public void SetInvulnerable(bool isInvulnerable)
    {
        _isInvulnerable = isInvulnerable;
    }

    public float ApplyDamage(float amount)
    {
        if (Time.timeScale <= 0f || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) || !CanBeTargeted || _isInvulnerable)
        {
            return 0f;
        }

        float previousHealth = _healthState.CurrentHealth;
        float currentHealth = _healthState.ChangeHealth(-amount);
        float appliedDamage = Mathf.Max(0f, previousHealth - currentHealth);

        if (appliedDamage <= 0f)
        {
            return 0f;
        }

        Damaged?.Invoke(appliedDamage);

        if (currentHealth <= 0f && !_hasRaisedDied)
        {
            _hasRaisedDied = true;
            Died?.Invoke();
        }
        return appliedDamage;
    }
}
