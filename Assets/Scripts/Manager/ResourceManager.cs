using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ResourceManager : MonoBehaviour, IInventory
{
    public const int GoldItemId = 8;
    private static readonly IReadOnlyDictionary<int, int> Empty = new Dictionary<int, int>();
    [SerializeField] private ItemDataContext _itemDataContext;
    private readonly ResourceInventory _inventory = new ResourceInventory();
    private readonly Dictionary<UnityEngine.Object, int> _capacityProviders = new Dictionary<UnityEngine.Object, int>();
    private int _notificationDepth;
    private bool _pendingNotification;
    private bool _isNotifying;

    public event Action ResourcesChanged;
    public long Capacity => _inventory.Capacity;
    public long UsedCapacity => _inventory.UsedCapacity;
    public bool HasStorageSpace => UsedCapacity < Capacity;
    public int GetQuantity(int itemId) => _inventory.GetQuantity(itemId);
    public ItemDataContext ItemData => _itemDataContext;

    public bool TryDeposit(int itemId, int quantity, out int acceptedQuantity)
    {
        acceptedQuantity = 0;
        if (!_itemDataContext || !_itemDataContext.TryGetItemInfo(itemId, out ItemInfo info)
            || !_inventory.TryDeposit(itemId, quantity, info.UsesStorage, out acceptedQuantity))
            return false;
        NotifyChanged();
        return true;
    }

    bool IInventory.TryAdd(int itemId, int quantity, out int acceptedQuantity)
        => TryDeposit(itemId, quantity, out acceptedQuantity);

    public bool TrySpend(IReadOnlyDictionary<int, int> cost) => TryExchange(cost, Empty);
    public bool TryRefund(IReadOnlyDictionary<int, int> amounts) => TryExchange(Empty, amounts);

    // Credits are exact, including refunds over capacity. Ordinary intake uses TryDeposit.
    public bool TryExchange(IReadOnlyDictionary<int, int> debits, IReadOnlyDictionary<int, int> credits)
    {
        var storageRules = new Dictionary<int, bool>();
        if (!TryValidate(debits, storageRules) || !TryValidate(credits, storageRules))
            return false;
        bool changed = false;
        foreach (var entry in debits)
            if (!credits.TryGetValue(entry.Key, out int credit) || credit != entry.Value)
                changed = true;
        foreach (var entry in credits)
            if (!debits.ContainsKey(entry.Key))
                changed = true;
        if (!_inventory.TryExchange(debits, credits, storageRules))
            return false;
        if (changed)
            NotifyChanged();
        return true;
    }

    private bool TryValidate(IReadOnlyDictionary<int, int> amounts, Dictionary<int, bool> storageRules)
    {
        if (amounts == null || !_itemDataContext)
            return false;
        foreach (var entry in amounts)
        {
            if (entry.Value <= 0 || !_itemDataContext.TryGetItemInfo(entry.Key, out ItemInfo info))
                return false;
            storageRules[entry.Key] = info.UsesStorage;
        }
        return true;
    }

    public bool RegisterCapacity(UnityEngine.Object owner, int capacity)
    {
        if (!owner || capacity < 0)
            return false;
        _capacityProviders.TryGetValue(owner, out int previous);
        _capacityProviders[owner] = capacity;
        if (previous != capacity)
        {
            _inventory.SetCapacity(Capacity + (long)capacity - previous);
            NotifyChanged();
        }
        return true;
    }

    public void UnregisterCapacity(UnityEngine.Object owner)
    {
        if (ReferenceEquals(owner, null) || !_capacityProviders.TryGetValue(owner, out int capacity))
            return;
        _capacityProviders.Remove(owner);
        _inventory.SetCapacity(Capacity - capacity);
        if (capacity != 0)
            NotifyChanged();
    }

    public IDisposable DeferNotifications()
    {
        ++_notificationDepth;
        return new NotificationScope(this);
    }

    private void NotifyChanged()
    {
        _pendingNotification = true;
        FlushNotifications();
    }

    private void FlushNotifications()
    {
        if (_notificationDepth > 0 || _isNotifying)
            return;
        _isNotifying = true;
        try
        {
            while (_pendingNotification && _notificationDepth == 0)
            {
                _pendingNotification = false;
                Action handlers = ResourcesChanged;
                if (handlers == null)
                    continue;
                foreach (Action handler in handlers.GetInvocationList())
                {
                    try { handler(); }
                    catch (Exception exception) { Debug.LogException(exception, this); }
                }
            }
        }
        finally { _isNotifying = false; }
    }

    private sealed class NotificationScope : IDisposable
    {
        private ResourceManager _owner;
        public NotificationScope(ResourceManager owner) { _owner = owner; }
        public void Dispose()
        {
            ResourceManager owner = _owner;
            _owner = null;
            if (!owner)
                return;
            --owner._notificationDepth;
            owner.FlushNotifications();
        }
    }
}
