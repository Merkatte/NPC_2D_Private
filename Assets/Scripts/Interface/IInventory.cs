public interface IInventory
{
    /// <summary>
    /// Accepts as much of <paramref name="quantity"/> as the implementation can hold.
    /// Returns true when at least one unit was accepted; <paramref name="acceptedQuantity"/>
    /// may be less than requested (partial accept is allowed by this contract). Callers must
    /// not assume all-or-nothing — check <paramref name="acceptedQuantity"/> against the
    /// requested amount when the difference matters.
    /// </summary>
    bool TryAdd(int itemId, int quantity, out int acceptedQuantity);
    int GetQuantity(int itemId);
}
