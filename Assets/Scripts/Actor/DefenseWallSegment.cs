using System;
using UnityEngine;

public sealed class DefenseWallSegment : MonoBehaviour, IHealthState
{
    [SerializeField] private DefenseBattlefield _battlefield;
    [SerializeField] private DefenseDurabilitySettings _settings;
    [SerializeField] private CombatTarget _target;
    [SerializeField] private DefenseMaintenanceSite _maintenance;
    [SerializeField] private NavigationObstacle2D[] _obstacles = Array.Empty<NavigationObstacle2D>();
    [SerializeField] private BoxCollider2D _passageBounds;
    [SerializeField] private Transform _topAnchor;
    [SerializeField] private Transform _groundAnchor;
    [SerializeField] private Transform _attackAnchor;
    [SerializeField] private Transform _entryAnchor;
    [SerializeField] private SpriteRenderer _visual;
    [SerializeField] private Sprite _intactSprite;
    [SerializeField] private Sprite _damagedSprite;
    [SerializeField] private Sprite _destroyedSprite;
    private float _health;
    private float _lastDamageTime = float.NegativeInfinity;
    private bool _pendingClosure;
    private bool _initialized;
    public float CurrentHealth => _health;
    public float MaximumHealth => _settings ? _settings.WallHealth : 0f;
    public bool IsPassable => _health <= 0f || _pendingClosure;
    public bool IsDamaged => _health > 0f && _health < MaximumHealth;
    public bool IsUnderAttack => Time.time - _lastDamageTime <= (_settings ? _settings.UnderAttackSeconds : 0f);
    public bool IsAwaitingClearPassage => _pendingClosure;
    public CombatTarget Target => _target;
    public Vector3 TopPosition => _topAnchor.position;
    public Vector3 GroundPosition => _groundAnchor.position;
    public Vector3 AttackPosition => _attackAnchor.position;
    public Vector3 EntryPosition => _entryAnchor.position;
    private void Awake()
    {
        if (!_settings || !_battlefield || !_target || !_topAnchor || !_groundAnchor || !_attackAnchor || !_entryAnchor || !_passageBounds)
        { Debug.LogError($"Defense wall '{name}' has incomplete anchors/services.", this); return; }
        _health = MaximumHealth; _initialized = true; _target.Initialize(this); Refresh();
        if (_maintenance) _maintenance.RefreshTask();
    }
    public float ChangeHealth(float amount)
    {
        if (!_initialized || float.IsNaN(amount) || float.IsInfinity(amount) || Time.timeScale <= 0f) return _health;
        if (_health <= 0f && amount > 0f) return _health;
        _health = Mathf.Clamp(_health + amount, 0f, MaximumHealth);
        if (amount < 0f) _lastDamageTime = Time.time;
        Refresh();
        if (_maintenance) _maintenance.RefreshTask();
        return _health;
    }
    public bool TryFinishRebuild()
    {
        if (!_initialized || _health > 0f || Time.timeScale <= 0f) return false;
        _health = MaximumHealth;
        _pendingClosure = _battlefield.IsOccupied(_passageBounds);
        _target.Initialize(this);
        Refresh();
        return true;
    }
    private void Update()
    {
        if (!_pendingClosure || Time.deltaTime <= 0f || _battlefield.IsOccupied(_passageBounds)) return;
        _pendingClosure = false; Refresh();
    }
    private void Refresh()
    {
        foreach (NavigationObstacle2D obstacle in _obstacles) if (obstacle) obstacle.SetBlocked(!IsPassable);
        if (_visual) _visual.sprite = IsPassable ? _destroyedSprite : IsDamaged ? _damagedSprite : _intactSprite;
        if (_target) _target.SetTargetable(!IsPassable);
        if (_battlefield) _battlefield.NotifyChanged();
    }
}
