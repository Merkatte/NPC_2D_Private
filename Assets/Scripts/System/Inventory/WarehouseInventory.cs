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
}
