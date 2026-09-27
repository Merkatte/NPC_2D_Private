using UnityEngine;

public sealed class WarehouseDepositPoint : BaseInteractionProvider, IClickPopupSource
{
    [SerializeField] private ResourceManager _inventorySource;
    private int _providedCapacity;
    public ResourceManager Resources => _inventorySource;
    public void Configure(ResourceManager resources, int providedCapacity)
    {
        _inventorySource = resources;
        _providedCapacity = providedCapacity;
    }
    public bool RegisterCapacity() => _inventorySource && _inventorySource.RegisterCapacity(this, _providedCapacity);
    public void UnregisterCapacity() { if (_inventorySource) _inventorySource.UnregisterCapacity(this); }
    private void Start() { RegisterCapacity(); }
    private void OnDestroy() { UnregisterCapacity(); }
    public bool TryGetClickPopup(out PopupType type)
    {
        type = _inventorySource && isActiveAndEnabled ? PopupType.Warehouse : PopupType.None;
        return type != PopupType.None;
    }
    protected override bool SupportsCore(ActionType type) => type == ActionType.Deposit;
    protected override bool CanInteractCore(ActionType type)
        => isActiveAndEnabled && _inventorySource && _inventorySource.HasStorageSpace;
    protected override bool TryInitializeCore(out string reason)
    {
        reason = _inventorySource ? null : "missing ResourceManager _inventorySource";
        return _inventorySource;
    }
    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;
        if (!_inventorySource || !request.HasCargo) return false;
        using (_inventorySource.DeferNotifications())
            return request.Cargo.TryTransferAllTo(_inventorySource, out _);
    }
}
