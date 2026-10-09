using UnityEngine;

public sealed class DefenseMaintenanceSite : BaseInteractionProvider
{
    [SerializeField] private DefenseMaintenanceRegistry _registry;
    [SerializeField] private ResourceManager _resources;
    [SerializeField] private DefenseDurabilitySettings _settings;
    [SerializeField] private DefenseWallSegment _wall;
    [SerializeField] private DefenseBuildingDurability _building;
    [SerializeField] private Transform _workAnchor;
    private MaintenanceLease _lease;
    private MaintenanceKind _kind;
    private float _progress;
    private bool _isPaid;
    private bool _isChanging;
    public long Order { get; private set; }
    public MaintenanceKind Kind => _kind;
    public int Priority => _wall ? (_kind == MaintenanceKind.RebuildWall ? 1 : _wall.IsUnderAttack ? 0 : 2)
        : _kind == MaintenanceKind.Repair ? 2 : 3;
    public bool CanReserve => isActiveAndEnabled && !_isChanging && _kind != MaintenanceKind.None && _lease == null && CanPay();
    private void OnEnable() { if (_registry) _registry.Register(this); RefreshTask(); }
    private void OnDisable() { _lease?.Dispose(); if (_registry) _registry.Unregister(this); }
    public void Configure(DefenseMaintenanceRegistry registry, ResourceManager resources, DefenseDurabilitySettings settings,
        DefenseBuildingDurability building, Transform workAnchor)
    { _registry = registry; _resources = resources; _settings = settings; _building = building; _workAnchor = workAnchor; }
    public void RefreshTask()
    {
        MaintenanceKind next = _wall ? (_wall.CurrentHealth <= 0f ? MaintenanceKind.RebuildWall : _wall.IsDamaged ? MaintenanceKind.Repair : MaintenanceKind.None)
            : _building ? (_building.IsRubble ? MaintenanceKind.ClearRubble : _building.IsDamaged ? MaintenanceKind.Repair : MaintenanceKind.None) : MaintenanceKind.None;
        if (next == _kind) return;
        _lease?.Dispose(); _kind = next; _progress = 0f; _isPaid = false;
        Order = _registry ? _registry.NextOrder() : long.MaxValue;
    }
    private bool CanPay()
    {
        if (_isPaid) return true;
        if (!_resources || !_settings || !_settings.TryGetCost(_kind, out var cost)) return false;
        foreach (var entry in cost) if (_resources.GetQuantity(entry.Key) < entry.Value) return false;
        return true;
    }
    public bool TryReserve(out MaintenanceLease lease)
    {
        lease = null;
        if (!CanReserve || !_workAnchor) return false;
        lease = new MaintenanceLease(this, _kind, _workAnchor.position); _lease = lease;
        return true;
    }
    internal bool IsLeaseValid(MaintenanceLease lease)
        => isActiveAndEnabled && ReferenceEquals(_lease, lease) && lease.Kind == _kind && _kind != MaintenanceKind.None;
    internal void Release(MaintenanceLease lease) { if (ReferenceEquals(_lease, lease)) _lease = null; }
    protected override bool SupportsCore(ActionType type) => type == ActionType.Maintain;
    protected override bool CanInteractCore(ActionType type) => isActiveAndEnabled && _kind != MaintenanceKind.None;
    protected override bool TryInitializeCore(out string reason)
    {
        reason = "Maintenance requires registry, resources, settings, work anchor and one durability owner.";
        if (!_registry || !_resources || !_settings || !_workAnchor || (!_wall && !_building)) return false;
        reason = null; return true;
    }
    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;
        if (_isChanging || Time.timeScale <= 0f || request.Strength <= 0f || !(request.Reservation is MaintenanceLease lease) || !IsLeaseValid(lease)) return false;
        _isChanging = true;
        try
        {
            using (_resources.DeferNotifications())
            {
                if (!_isPaid)
                {
                    if (!_settings.TryGetCost(_kind, out var cost) || !_resources.TrySpend(cost)) return false;
                    _isPaid = true;
                }
                MaintenanceKind previousKind = _kind;
                if (_kind == MaintenanceKind.Repair)
                {
                    if (_wall) _wall.ChangeHealth(_settings.RepairPerSecond * request.Strength);
                    else _building.ChangeHealth(_settings.RepairPerSecond * request.Strength);
                }
                else
                {
                    _progress += request.Strength;
                    if (_kind == MaintenanceKind.RebuildWall && _progress >= _settings.WallRebuildSeconds) _wall.TryFinishRebuild();
                    else if (_kind == MaintenanceKind.ClearRubble && _progress >= _settings.ClearRubbleSeconds) _building.ClearRubble();
                }
                RefreshTask();
                if (_kind != previousKind) { lease.Complete(); _lease = null; }
                return true;
            }
        }
        finally { _isChanging = false; }
    }
}
