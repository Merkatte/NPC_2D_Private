using System;
using UnityEngine;

/// <summary>
/// A single NPC's carried cargo. Plain C# (not a MonoBehaviour) so it can be owned inline by
/// NPCComponent without prefab wiring, mirroring CombatRuntimeState's ownership pattern.
/// Holds at most one item type at a time up to <see cref="_capacity"/> — this project has a
/// single Farm destination and crop selection requires zero progress on the current crop, so
/// two different crops can never land in one worker's cargo at the same time.
/// </summary>
public sealed class WorkerInventory : ICarriedInventory
{
    private readonly int _capacity;
    private readonly Action<bool> _setCarryVisible;
    private int _itemId = -1;
    private int _quantity;

    public WorkerInventory(int capacity, Action<bool> setCarryVisible)
    {
        _capacity = Mathf.Max(1, capacity);
        _setCarryVisible = setCarryVisible;
    }

    public bool IsEmpty => _quantity <= 0;
    public bool IsFull => _quantity >= _capacity;

    public bool TryAdd(int itemId, int quantity, out int acceptedQuantity)
    {
        acceptedQuantity = 0;

        if (itemId < 0 || quantity <= 0)
            return false;

        if (_quantity > 0 && itemId != _itemId)
            return false;

        int room = _capacity - _quantity;
        acceptedQuantity = Mathf.Min(quantity, room);
        if (acceptedQuantity <= 0)
            return false;

        _itemId = itemId;
        _quantity += acceptedQuantity;
        _setCarryVisible?.Invoke(true);
        return true;
    }

    public int GetQuantity(int itemId) => itemId == _itemId ? _quantity : 0;

    public bool TryTransferAllTo(IInventory destination, out int movedQuantity)
    {
        movedQuantity = 0;

        if (IsEmpty || destination == null)
            return false;

        if (!destination.TryAdd(_itemId, _quantity, out int accepted) || accepted <= 0)
            return false;

        _quantity -= accepted;
        movedQuantity = accepted;
        if (_quantity <= 0)
            _itemId = -1;

        _setCarryVisible?.Invoke(!IsEmpty);
        return true;
    }

    /// <summary>
    /// Not part of <see cref="ICarriedInventory"/> — no external provider (Farm, Warehouse) has
    /// authority to empty a worker's cargo. Only the owning NPCComponent calls this, on pool reuse.
    /// </summary>
    public void Clear()
    {
        _itemId = -1;
        _quantity = 0;
        _setCarryVisible?.Invoke(false);
    }
}
