using System.Collections.Generic;

// Owns quantities only. Definitions and notifications belong to ResourceManager.
public sealed class ResourceInventory
{
    private readonly Dictionary<int, int> _quantities = new Dictionary<int, int>();
    public long Capacity { get; private set; }
    public long UsedCapacity { get; private set; }
    internal void SetCapacity(long capacity) { Capacity = capacity; }
    public int GetQuantity(int itemId) => _quantities.TryGetValue(itemId, out int value) ? value : 0;

    public bool TryDeposit(int itemId, int quantity, bool usesStorage, out int accepted)
    {
        accepted = 0;
        if (quantity <= 0)
            return false;
        int amount = usesStorage ? (int)System.Math.Min(quantity, System.Math.Max(0L, Capacity - UsedCapacity)) : quantity;
        if (amount == 0 || (long)GetQuantity(itemId) + amount > int.MaxValue)
            return false;
        accepted = amount;
        _quantities[itemId] = GetQuantity(itemId) + accepted;
        if (usesStorage)
            UsedCapacity += accepted;
        return true;
    }

    public bool TryExchange(IReadOnlyDictionary<int, int> debits, IReadOnlyDictionary<int, int> credits,
        IReadOnlyDictionary<int, bool> storageRules)
    {
        foreach (var entry in debits)
            if (entry.Value <= 0 || GetQuantity(entry.Key) < entry.Value)
                return false;
        foreach (var entry in credits)
        {
            int debit = debits.TryGetValue(entry.Key, out int amount) ? amount : 0;
            if (entry.Value <= 0 || (long)GetQuantity(entry.Key) - debit + entry.Value > int.MaxValue)
                return false;
        }
        foreach (var entry in debits)
        {
            _quantities[entry.Key] = GetQuantity(entry.Key) - entry.Value;
            if (storageRules[entry.Key])
                UsedCapacity -= entry.Value;
        }
        foreach (var entry in credits)
        {
            _quantities[entry.Key] = GetQuantity(entry.Key) + entry.Value;
            if (storageRules[entry.Key])
                UsedCapacity += entry.Value;
        }
        return true;
    }
}
