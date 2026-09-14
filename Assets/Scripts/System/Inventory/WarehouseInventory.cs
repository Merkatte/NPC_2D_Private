using System;
using System.Collections.Generic;
using UnityEngine;

public class WarehouseInventory : MonoBehaviour, IInventory
{
    [Serializable]
    private struct InitialStockEntry
    {
        [SerializeField, Min(0)] private int _itemId;
        [SerializeField, Min(1)] private int _quantity;

        public int ItemId => _itemId;
        public int Quantity => _quantity;

        public void Clamp()
        {
            _itemId = Mathf.Max(0, _itemId);
            _quantity = Mathf.Max(1, _quantity);
        }
    }

    [SerializeField] private InitialStockEntry[] _initialStock = Array.Empty<InitialStockEntry>();

    private readonly Dictionary<int, int> _quantities = new Dictionary<int, int>();

    private void OnValidate()
    {
        if (_initialStock == null)
            return;

        for (int i = 0; i < _initialStock.Length; i++)
        {
            InitialStockEntry entry = _initialStock[i];
            entry.Clamp();
            _initialStock[i] = entry;
        }
    }

    private void Awake()
    {
        if (_initialStock == null)
            return;

        foreach (InitialStockEntry entry in _initialStock)
        {
            if (!TryAdd(entry.ItemId, entry.Quantity, out int acceptedQuantity) || acceptedQuantity != entry.Quantity)
                Debug.LogError($"WarehouseInventory '{name}' has invalid initial stock: itemId={entry.ItemId}, quantity={entry.Quantity}.", this);
        }
    }

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
        Debug.Log("Successfully added all items");
        Debug.Log("itemId = " + itemId);
        Debug.Log("item count = " + sum);
        return true;
    }

    public int GetQuantity(int itemId)
    {
        return _quantities.TryGetValue(itemId, out int quantity) ? quantity : 0;
    }

    /// <summary>
    /// Not part of IInventory — that contract only covers accepting stock (partial accept
    /// allowed), and nothing else in the project has authority to remove warehouse stock.
    /// Only MerchantTradeSite calls this, for a completed sale (a whole cart at once, since a
    /// single-item TryRemove would have no other caller). All-or-nothing across the whole batch:
    /// a validation pass confirms every line before an execution pass mutates anything, so a
    /// request that fails partway through never leaves some items removed and others not.
    /// </summary>
    public bool TryRemoveBatch(IReadOnlyDictionary<int, int> quantitiesByItemId)
    {
        if (quantitiesByItemId == null || quantitiesByItemId.Count == 0)
            return false;

        foreach (KeyValuePair<int, int> entry in quantitiesByItemId)
        {
            if (entry.Key < 0 || entry.Value <= 0)
                return false;

            if (!_quantities.TryGetValue(entry.Key, out int current) || current < entry.Value)
                return false;
        }

        foreach (KeyValuePair<int, int> entry in quantitiesByItemId)
        {
            _quantities[entry.Key] -= entry.Value;
        }

        return true;
    }
}
