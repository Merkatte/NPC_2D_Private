using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-bound entry point for choosing and planting a farm's next crop.</summary>
public sealed class FarmSeedSource : MonoBehaviour, IClickPopupSource
{
    [SerializeField] private FarmWorkSite _farm;
    [SerializeField] private ResourceManager _warehouse;
    [SerializeField] private CropCatalog _cropCatalog;
    [SerializeField] private ItemDataContext _itemDataContext;

    private bool _hasValidatedConfiguration;
    private bool _isConfigured;

    public ResourceManager Resources => _warehouse;
    public void Configure(ResourceManager resources, CropCatalog catalog, ItemDataContext items)
    {
        _warehouse = resources;
        _cropCatalog = catalog;
        _itemDataContext = items;
        _hasValidatedConfiguration = false;
        _isConfigured = false;
    }
    public bool TryInitialize() => EnsureConfigured();

    public bool IsAvailable => isActiveAndEnabled && _farm && _farm.isActiveAndEnabled
        && _warehouse && _warehouse.isActiveAndEnabled;
    public bool CanPlant => IsAvailable && EnsureConfigured() && _farm.CanSelectCrop;

    public bool TryGetClickPopup(out PopupType popupType)
    {
        popupType = CanPlant ? PopupType.SeedSelection : PopupType.None;
        return popupType != PopupType.None;
    }

    public void GetAvailableSeeds(List<ItemInfo> output)
    {
        if (output == null)
            return;

        output.Clear();
        if (!IsAvailable || !EnsureConfigured())
            return;

        foreach (FarmProductionDefinition definition in _cropCatalog.Definitions)
        {
            if (TryGetSeedInfo(definition.SeedItemId, out ItemInfo info, out int quantity) && quantity > 0)
                output.Add(info);
        }
    }

    public bool TryGetSeedInfo(int seedItemId, out ItemInfo info, out int quantity)
    {
        info = default;
        quantity = 0;
        if (!IsAvailable || !EnsureConfigured()
            || !_cropCatalog.TryGetDefinitionBySeedItemId(seedItemId, out FarmProductionDefinition definition)
            || !definition.IsValid || !_itemDataContext.TryGetItemInfo(seedItemId, out info)
            || info.Category != ItemCategory.Seed)
            return false;

        quantity = _warehouse.GetQuantity(seedItemId);
        return true;
    }

    public bool TryPlantSeed(int seedItemId, out SeedPlantResult result)
    {
        result = SeedPlantResult.FarmUnavailable;
        if (!IsAvailable)
            return false;

        result = SeedPlantResult.InvalidConfiguration;
        if (!EnsureConfigured())
            return false;

        result = SeedPlantResult.InvalidSeed;
        if (!TryGetSeedInfo(seedItemId, out _, out _)
            || !_cropCatalog.TryGetDefinitionBySeedItemId(seedItemId, out FarmProductionDefinition definition))
            return false;

        return _farm.TryPlantCrop(definition, _warehouse, out result);
    }

    private bool EnsureConfigured()
    {
        if (_hasValidatedConfiguration)
            return _isConfigured && _cropCatalog && _itemDataContext;

        _hasValidatedConfiguration = true;
        string reason = "missing farm, warehouse, crop catalog or item data";
        _isConfigured = _farm && _warehouse && _cropCatalog && _itemDataContext
            && _cropCatalog.TryValidate(_itemDataContext, out reason)
            && _farm.TryInitialize(out reason);
        if (!_isConfigured)
            Debug.LogError($"FarmSeedSource '{name}': {reason}.", this);
        return _isConfigured;
    }
}
