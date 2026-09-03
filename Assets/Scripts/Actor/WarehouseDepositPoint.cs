using UnityEngine;

/// <summary>
/// Scene facility that accepts a worker's carried cargo into a warehouse-side IInventory.
/// Owns only the deposit transaction; it does not know about NPC or Farmer concrete types,
/// and the warehouse's own quantity storage stays in WarehouseInventory (single responsibility).
/// </summary>
public class WarehouseDepositPoint : BaseInteractionProvider
{
    [SerializeField] private MonoBehaviour _inventorySource;

    private IInventory _inventory;
    private bool _hasLoggedSourceDestroyed;
    private bool _hasLoggedMissingCargo;

    protected override bool SupportsCore(ActionType type)
        => type == ActionType.Deposit;

    protected override bool TryInitializeCore(out string failureReason)
    {
        _inventory = _inventorySource as IInventory;

        if (_inventory == null)
        {
            failureReason = "_inventorySource does not implement IInventory";
            return false;
        }

        failureReason = null;
        return true;
    }

    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;

        // _inventorySource is the real backing UnityEngine.Object; re-check it with Unity's
        // null semantics (CodeConvention.md 8.3) since the cached _inventory interface reference
        // cannot by itself detect a destroyed MonoBehaviour.
        if (!_inventorySource)
        {
            LogSourceDestroyedOnce();
            return false;
        }

        if (!request.HasCargo)
        {
            LogMissingCargoOnce();
            return false;
        }

        return request.Cargo.TryTransferAllTo(_inventory, out _);
    }

    private void LogSourceDestroyedOnce()
    {
        if (_hasLoggedSourceDestroyed)
            return;

        Debug.LogError($"WarehouseDepositPoint '{name}': _inventorySource was destroyed; deposits are disabled.", this);
        _hasLoggedSourceDestroyed = true;
    }

    private void LogMissingCargoOnce()
    {
        if (_hasLoggedMissingCargo)
            return;

        Debug.LogError($"WarehouseDepositPoint '{name}': Deposit request arrived without carried cargo.", this);
        _hasLoggedMissingCargo = true;
    }
}
