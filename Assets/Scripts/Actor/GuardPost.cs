using System;
using UnityEngine;

public sealed class GuardPost : BaseInteractionProvider, IHealthState
{
    private const int PositionResolution = 1000000;

    [SerializeField] private BoxCollider2D _patrolArea;
    [SerializeField] private SeededRandomSource _positionRandomSource;
    [SerializeField] private CombatTarget _combatTarget;
    [SerializeField] private GameObject _visual;
    [SerializeField, Min(1f)] private float _maximumHealth = 100f;

    private float _currentHealth;
    private bool _healthInitialized;

    public event Action Died;
    public float CurrentHealth => _currentHealth;

    private void Awake()
    {
        InitializeHealth();
        if (!TryInitialize(out string reason))
            Debug.LogError($"GuardPost '{name}': {reason}", this);
    }

    private void InitializeHealth()
    {
        if (_healthInitialized)
            return;

        _healthInitialized = true;
        _currentHealth = Mathf.Max(1f, _maximumHealth);
        if (_combatTarget)
            _combatTarget.Initialize(this);
    }

    public float ChangeHealth(float amount)
    {
        InitializeHealth();
        if (_currentHealth <= 0f || float.IsNaN(amount))
            return _currentHealth;

        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maximumHealth);
        if (_currentHealth <= 0f)
        {
            if (_combatTarget)
                _combatTarget.SetTargetable(false);
            if (_visual)
                _visual.SetActive(false);
            Died?.Invoke();
        }
        return _currentHealth;
    }

    protected override bool SupportsCore(ActionType type) => type == ActionType.Guard;

    protected override bool TryInitializeCore(out string failureReason)
    {
        InitializeHealth();
        failureReason = null;
        if (!_patrolArea || !_positionRandomSource || !_combatTarget || !_visual || _visual == gameObject)
            failureReason = "assign patrol area, independent random source, CombatTarget and child visual";
        else if (_patrolArea.size.x <= 0f || _patrolArea.size.y <= 0f)
            failureReason = "patrol area must have a positive size";
        return failureReason == null;
    }

    protected override bool CanInteractCore(ActionType type)
        => isActiveAndEnabled && _currentHealth > 0f && _patrolArea &&
           _patrolArea.gameObject.activeInHierarchy && _positionRandomSource;

    protected override bool TryGetActionPositionCore(ActionType type, Vector3 fallbackPosition, out Vector3 position)
    {
        Vector2 fraction = new Vector2(
            _positionRandomSource.NextInclusive(0, PositionResolution) / (float)PositionResolution,
            _positionRandomSource.NextInclusive(0, PositionResolution) / (float)PositionResolution);
        Vector2 local = _patrolArea.offset + Vector2.Scale(fraction - Vector2.one * 0.5f, _patrolArea.size);
        position = _patrolArea.transform.TransformPoint(local);
        return true;
    }

    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;
        return false;
    }

    private void OnValidate() => _maximumHealth = Mathf.Max(1f, _maximumHealth);
}
