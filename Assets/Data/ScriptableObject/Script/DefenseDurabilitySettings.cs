using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Defense/Durability Settings")]
public sealed class DefenseDurabilitySettings : ScriptableObject
{
    [Serializable] private struct CostEntry
    {
        [SerializeField] private int _itemId;
        [SerializeField, Min(1)] private int _amount;
        public int ItemId => _itemId;
        public int Amount => _amount;
    }
    [SerializeField, Min(1f)] private float _wallHealth = 500f;
    [SerializeField, Min(1f)] private float _buildingHealth = 200f;
    [SerializeField, Min(1f)] private float _townHallHealth = 1000f;
    [SerializeField, Min(0.1f)] private float _repairPerSecond = 20f;
    [SerializeField, Min(0.1f)] private float _wallRebuildSeconds = 15f;
    [SerializeField, Min(0.1f)] private float _clearRubbleSeconds = 8f;
    [SerializeField, Min(0.1f)] private float _underAttackSeconds = 3f;
    [SerializeField] private CostEntry[] _repairCost = Array.Empty<CostEntry>();
    [SerializeField] private CostEntry[] _rebuildCost = Array.Empty<CostEntry>();
    [SerializeField] private CostEntry[] _clearCost = Array.Empty<CostEntry>();
    public float WallHealth => Mathf.Max(1f, _wallHealth);
    public float BuildingHealth => Mathf.Max(1f, _buildingHealth);
    public float TownHallHealth => Mathf.Max(1f, _townHallHealth);
    public float RepairPerSecond => Mathf.Max(0.1f, _repairPerSecond);
    public float WallRebuildSeconds => Mathf.Max(0.1f, _wallRebuildSeconds);
    public float ClearRubbleSeconds => Mathf.Max(0.1f, _clearRubbleSeconds);
    public float UnderAttackSeconds => Mathf.Max(0.1f, _underAttackSeconds);
    public bool TryGetCost(MaintenanceKind kind, out Dictionary<int, int> cost)
    {
        cost = new Dictionary<int, int>();
        CostEntry[] source = kind == MaintenanceKind.RebuildWall ? _rebuildCost : kind == MaintenanceKind.ClearRubble ? _clearCost : _repairCost;
        foreach (CostEntry entry in source)
        {
            if (entry.ItemId <= 0 || entry.Amount <= 0 || cost.ContainsKey(entry.ItemId)) { cost.Clear(); return false; }
            cost.Add(entry.ItemId, entry.Amount);
        }
        return true;
    }
    private void OnValidate()
    {
        _wallHealth = WallHealth; _buildingHealth = BuildingHealth; _townHallHealth = TownHallHealth;
        _repairPerSecond = RepairPerSecond; _wallRebuildSeconds = WallRebuildSeconds;
        _clearRubbleSeconds = ClearRubbleSeconds; _underAttackSeconds = UnderAttackSeconds;
    }
}
