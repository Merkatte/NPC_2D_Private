public interface IInventory
{
    bool TryAdd(int itemId, int quantity, out int acceptedQuantity);
    int GetQuantity(int itemId);
}
