using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HousingDataContext", menuName = "Scriptable Objects/HousingDataContext")]
public sealed class HousingDataContext : ScriptableObject
{
    [SerializeField] private TextAsset _houseTiers;
    [SerializeField] private TextAsset _housingOptions;
    [SerializeField] private TextAsset _tierOptions;
    [SerializeField] private TextAsset _lifeSettings;
    [SerializeField] private BuildingDataContext _buildings;
    private IReadOnlyList<HouseTierDefinition> _tiers = Array.Empty<HouseTierDefinition>();
    private HousingLifeSettings _life;
    private bool _attempted;
    private string _error;

    public IReadOnlyList<HouseTierDefinition> Tiers { get { TryInitialize(out _); return _tiers; } }
    public HousingLifeSettings LifeSettings { get { TryInitialize(out _); return _life; } }

    public bool TryInitialize(out string error)
    {
        if (_attempted) { error = _error; return _life != null; }
        _attempted = true;
        _error = "HousingDataContext requires tiers, options, memberships, life CSV and buildings.";
        if (!_houseTiers || !_housingOptions || !_tierOptions || !_lifeSettings || !_buildings)
        { error = _error; return false; }
        if (!HousingCsvMapper.TryMap(CSVParser.ParseRows(_houseTiers), CSVParser.ParseRows(_housingOptions),
                CSVParser.ParseRows(_tierOptions), CSVParser.ParseRows(_lifeSettings), _buildings,
                out var tiers, out var life, out _error))
        { error = _error; return false; }
        _tiers = tiers;
        _life = life;
        error = null;
        return true;
    }

    public bool TryGetTier(int tier, out HouseTierDefinition definition)
    {
        definition = null;
        if (!TryInitialize(out _) || tier < 1 || tier > _tiers.Count) return false;
        definition = _tiers[tier - 1];
        return true;
    }

    public bool TryGetTierByBuildingId(int buildingId, out HouseTierDefinition definition)
    {
        definition = null;
        if (!TryInitialize(out _)) return false;
        foreach (var tier in _tiers)
            if (tier.BuildingId == buildingId) { definition = tier; return true; }
        return false;
    }

    public bool TryGetNextTier(int currentTier, out HouseTierDefinition definition)
        => TryGetTier(currentTier + 1, out definition);

    private void OnValidate()
    {
        _attempted = false;
        _error = null;
        _life = null;
        _tiers = Array.Empty<HouseTierDefinition>();
    }
}
