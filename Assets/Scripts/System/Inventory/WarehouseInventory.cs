using System.Collections.Generic;
using UnityEngine;

public class WarehouseInventory : MonoBehaviour, IInventory
{
    private readonly Dictionary<int, int> _quantities = new Dictionary<int, int>();

    public bool TryAdd(int itemId, int quantity, out int acceptedQuantity)
    {
        acceptedQuantity = 0;

        if (itemId < 0 || quantity <= 0)
            return false;

        _quantities.TryGetValue(itemId, out int current);

        long sum = (long)current + quantity;
        if (sum > int.MaxValue)
            return false;

        _quantities[itemId] = (int)sum;
        acceptedQuantity = quantity;
        return true;
    }

    public int GetQuantity(int itemId)
    {
        return _quantities.TryGetValue(itemId, out int quantity) ? quantity : 0;
    }

    /// <summary>
    /// Not part of IInventory — that contract only covers accepting stock (partial accept
    /// allowed), and nothing else in the project has authority to remove warehouse stock.
    /// Only MerchantTradeSite calls this, for a completed sale. All-or-nothing: a request for
    /// more than is held is rejected outright rather than removing whatever is available.
    /// </summary>
    public bool TryRemove(int itemId, int quantity, out int removedQuantity)
    {
        removedQuantity = 0;

        if (itemId < 0 || quantity <= 0)
            return false;

        if (!_quantities.TryGetValue(itemId, out int current) || current < quantity)
            return false;

        _quantities[itemId] = current - quantity;
        removedQuantity = quantity;
        return true;
    }
}
