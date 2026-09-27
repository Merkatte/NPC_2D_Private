using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDataContext", menuName = "Scriptable Objects/BuildingDataContext")]
public sealed class BuildingDataContext : ScriptableObject
{
    [Serializable]
    private sealed class AssetEntry
    {
        [SerializeField] private int _buildingId;
        [SerializeField] private CompletedBuildingFacility _prefab;
        [SerializeField] private Sprite _icon;
        public int Id => _buildingId;
        public CompletedBuildingFacility Prefab => _prefab;
        public Sprite Icon => _icon;
    }

    [SerializeField] private TextAsset _buildingData;
    [SerializeField] private TextAsset _buildingCosts;
    [SerializeField] private TextAsset _builderWork;
    [SerializeField] private ItemDataContext _items;
    [SerializeField] private AssetEntry[] _assets = Array.Empty<AssetEntry>();
    private IReadOnlyList<BuildingDefinition> _definitions = Array.Empty<BuildingDefinition>();
    private Dictionary<int, BuildingDefinition> _byId;
    private Dictionary<int, AssetEntry> _assetsById;
    private BuilderWorkDefinition _work;
    private bool _attempted;
    private string _error;
    private void OnValidate()
    {
        _attempted = false;
        _byId = null;
        _assetsById = null;
        _work = null;
        _error = null;
        _definitions = Array.Empty<BuildingDefinition>();
    }

    public IReadOnlyList<BuildingDefinition> Definitions { get { TryInitialize(out _); return _definitions; } }
    public BuilderWorkDefinition Work { get { TryInitialize(out _); return _work; } }

    public bool TryInitialize(out string error)
    {
        if (_attempted) { error = _error; return _byId != null; }
        _attempted = true;
        _error = "BuildingDataContext requires all three CSV files and an item catalog.";
        if (!_buildingData || !_buildingCosts || !_builderWork || !_items) { error = _error; return false; }
        if (!BuildingDefinitionCsvMapper.TryMap(CSVParser.ParseRows(_buildingData), CSVParser.ParseRows(_buildingCosts),
                _items, out var definitions, out _error)
            || !BuilderWorkCsvMapper.TryMap(CSVParser.ParseRows(_builderWork), out var work, out _error))
        { error = _error; return false; }
        var assets = new Dictionary<int, AssetEntry>();
        foreach (AssetEntry entry in _assets)
        {
            if (entry == null || !entry.Prefab || !entry.Icon || assets.ContainsKey(entry.Id))
            { error = _error = "BuildingDataContext: missing prefab/icon or duplicate asset buildingId."; return false; }
            assets.Add(entry.Id, entry);
        }
        var byId = new Dictionary<int, BuildingDefinition>();
        foreach (BuildingDefinition definition in definitions)
        {
            if (!assets.ContainsKey(definition.Id))
            { error = _error = $"BuildingDataContext: missing assets for buildingId {definition.Id}."; return false; }
            byId.Add(definition.Id, definition);
        }
        if (byId.Count != assets.Count)
        { error = _error = "BuildingDataContext: asset entry refers to an unknown buildingId."; return false; }
        // Publish the complete validated set together; no partial table escapes on failure.
        _byId = byId;
        _assetsById = assets;
        _definitions = definitions;
        _work = work;
        error = _error = null;
        return true;
    }

    public bool TryGetBuildingDefinition(int id, out BuildingDefinition definition)
    {
        definition = null;
        return TryInitialize(out _) && _byId.TryGetValue(id, out definition);
    }
    public bool TryGetAssets(int id, out CompletedBuildingFacility prefab, out Sprite icon)
    {
        prefab = null;
        icon = null;
        if (!TryInitialize(out _) || !_assetsById.TryGetValue(id, out var entry))
            return false;
        prefab = entry.Prefab;
        icon = entry.Icon;
        return prefab && icon;
    }
}
