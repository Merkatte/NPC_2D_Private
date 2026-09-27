using System;
using System.Collections.Generic;
using UnityEngine;

// Test scene starting conditions only. All mutations use production domain APIs.
public sealed class BuildingTestBootstrap : MonoBehaviour
{
    [Serializable]
    private struct StockEntry
    {
        [SerializeField] private int _itemId;
        [SerializeField] private int _quantity;
        public int ItemId => _itemId;
        public int Quantity => _quantity;
    }
    [SerializeField] private ResourceManager _resources;
    [SerializeField] private BuildingDataContext _buildings;
    [SerializeField] private WarehouseDepositPoint[] _initialWarehouses = Array.Empty<WarehouseDepositPoint>();
    [SerializeField] private StockEntry[] _initialStock = Array.Empty<StockEntry>();
    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private int _builderCount;
    [SerializeField] private BuildingPlotRegistry _plots;
    private bool _started;
    private void Start()
    {
        if (_started || !_resources || !_buildings || !_buildings.TryGetBuildingDefinition(1, out var warehouse)) return;
        _started = true;
        using (_resources.DeferNotifications())
        {
            foreach (WarehouseDepositPoint point in _initialWarehouses)
            {
                if (!point) continue;
                point.Configure(_resources, warehouse.ProvidedCapacity);
                point.RegisterCapacity();
            }
            var stock = new Dictionary<int, int>();
            foreach (StockEntry entry in _initialStock)
                if (entry.Quantity > 0) stock.Add(entry.ItemId, entry.Quantity);
            if (!_resources.TryRefund(stock)) Debug.LogError("BuildingTestBootstrap: initial stock rejected.", this);
        }
        if (_npcManager)
            for (int i = 0; i < _builderCount; ++i)
            {
                Vector3 position = _plots && _plots.Plots.Count > 0 ? _plots.Plots[0].transform.position : Vector3.zero;
                if (_npcManager.TryReserveWorker(NPCType.Builder, position, out var reservation))
                    _npcManager.CommitReservation(reservation);
            }
    }
    private void OnGUI()
    {
        if (!_plots) return;
        GUILayout.BeginArea(new Rect(20, 580, 420, 250), GUI.skin.box);
        foreach (BuildingPlot plot in _plots.Plots)
        {
            if (!plot || !plot.HasCompletionFailure) continue;
            GUILayout.Label($"{plot.name}: {plot.CompletionFailure}");
            if (GUILayout.Button("Retry completion: " + plot.name) && !plot.TryRetryCompletion(out string reason))
                Debug.LogWarning(reason, plot);
        }
        GUILayout.EndArea();
    }
}
