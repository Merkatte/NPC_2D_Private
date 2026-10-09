using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DefenseBuildingDurability : MonoBehaviour, IHealthState
{
    [SerializeField] private DefenseBattlefield _battlefield;
    [SerializeField] private DefenseMaintenanceRegistry _registry;
    [SerializeField] private ResourceManager _resources;
    [SerializeField] private DefenseDurabilitySettings _settings;
    [SerializeField] private DefenseGameSession _session;
    [SerializeField] private BuildingPlot _plot;
    [SerializeField] private CompletedBuildingFacility _facility;
    [SerializeField] private CombatTarget _target;
    [SerializeField] private DefenseMaintenanceSite _maintenance;
    [SerializeField] private Transform _workAnchor;
    [SerializeField] private bool _isTownHall;
    [SerializeField] private Behaviour[] _functionalBehaviours = Array.Empty<Behaviour>();
    [SerializeField] private Collider2D[] _functionalColliders = Array.Empty<Collider2D>();
    [SerializeField] private SpriteRenderer[] _visuals = Array.Empty<SpriteRenderer>();
    [SerializeField] private GameObject _rubbleVisual;
    [SerializeField] private DestinationDB _destinations;
    private float _health;
    private bool _initialized;
    private bool _destroyed;
    private bool _isCleared;
    private readonly List<DestinationInfo> _registrations = new List<DestinationInfo>();
    public CombatTarget Target => _target;
    public float CurrentHealth => _health;
    public float MaximumHealth => !_settings ? 0f : _isTownHall ? _settings.TownHallHealth : _settings.BuildingHealth;
    public bool IsFunctional => _initialized && !_destroyed && isActiveAndEnabled;
    public bool IsRubble => _destroyed && !_isCleared && !_isTownHall;
    public bool IsDamaged => IsFunctional && _health < MaximumHealth;
    public void Configure(DefenseBattlefield battlefield, DefenseMaintenanceRegistry registry, ResourceManager resources,
        DefenseDurabilitySettings settings, DefenseGameSession session, BuildingPlot plot, CompletedBuildingFacility facility, DestinationDB destinations)
    {
        _battlefield = battlefield; _registry = registry; _resources = resources; _settings = settings;
        _session = session; _plot = plot; _facility = facility; _destinations = destinations;
        if (_maintenance) _maintenance.Configure(registry, resources, settings, this, _workAnchor);
    }
    private void Awake()
    {
        if (!_settings || !_battlefield || !_target || !_workAnchor)
        { Debug.LogError($"Defense building '{name}' requires settings, battlefield, target and work anchor.", this); return; }
        _health = MaximumHealth; _initialized = true; _target.Initialize(this);
        if (_maintenance) _maintenance.RefreshTask();
        if (_rubbleVisual) _rubbleVisual.SetActive(false);
    }
    private void OnEnable() { if (_battlefield) _battlefield.RegisterBuilding(this); }
    private void OnDisable() { if (_battlefield) _battlefield.UnregisterBuilding(this); }
    public float ChangeHealth(float amount)
    {
        if (!_initialized || _destroyed || Time.timeScale <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return _health;
        _health = Mathf.Clamp(_health + amount, 0f, MaximumHealth);
        if (_health <= 0f) DestroyFacility();
        if (_maintenance) _maintenance.RefreshTask();
        return _health;
    }
    private void DestroyFacility()
    {
        if (_destroyed) return;
        _destroyed = true; _target.SetTargetable(false);
        if (_plot) _plot.OnFacilityDestroyed(_facility);
        if (_facility) _facility.RemoveRegistrations();
        // Scene-native facilities have serialized destination entries rather than factory registrations.
        _registrations.Clear();
        if (_destinations)
        {
            foreach (BuildingType type in _destinations.RegisteredKeys)
                foreach (DestinationInfo info in _destinations.GetCandidates(type))
                    if (info.DestinationObject && info.DestinationObject.transform.IsChildOf(transform)) _registrations.Add(info);
            foreach (DestinationInfo info in _registrations) _destinations.Unregister(info);
        }
        foreach (Behaviour behaviour in _functionalBehaviours)
        {
            if (!behaviour || behaviour == this || behaviour == _maintenance || behaviour == _target) continue;
            if (behaviour is WarehouseDepositPoint warehouse) warehouse.UnregisterCapacity();
            behaviour.enabled = false;
        }
        foreach (Collider2D collider in _functionalColliders) if (collider) collider.enabled = false;
        foreach (SpriteRenderer visual in _visuals) if (visual) visual.enabled = false;
        if (_rubbleVisual) _rubbleVisual.SetActive(true);
        if (_battlefield) _battlefield.NotifyChanged();
        if (_isTownHall && _session) _session.EndGame();
    }
    public bool ClearRubble()
    {
        if (!IsRubble || Time.timeScale <= 0f) return false;
        _isCleared = true;
        if (_plot && !_plot.ClearDestroyedFacility(_facility)) { _isCleared = false; return false; }
        if (_battlefield) _battlefield.UnregisterBuilding(this);
        Destroy(gameObject);
        return true;
    }
}
