using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CropCatalog", menuName = "Scriptable Objects/CropCatalog")]
public class CropCatalog : ScriptableObject
{
    [SerializeField] private List<FarmProductionDefinition> _definitions = new List<FarmProductionDefinition>();

    public IReadOnlyList<FarmProductionDefinition> Definitions => _definitions ?? (IReadOnlyList<FarmProductionDefinition>)System.Array.Empty<FarmProductionDefinition>();

    public bool TryGetDefinition(int cropId, out FarmProductionDefinition definition)
    {
        definition = null;

        if (cropId < 0 || _definitions == null)
            return false;

        for (int i = 0; i < _definitions.Count; ++i)
        {
            FarmProductionDefinition candidate = _definitions[i];
            if (!candidate || candidate.CropId != cropId)
                continue;

            definition = candidate;
            return true;
        }

        return false;
    }

    public bool TryGetDefinitionBySeedItemId(int seedItemId, out FarmProductionDefinition definition)
    {
        definition = null;
        if (seedItemId < 0 || _definitions == null)
            return false;

        foreach (FarmProductionDefinition candidate in _definitions)
        {
            if (candidate && candidate.SeedItemId == seedItemId)
            {
                definition = candidate;
                return true;
            }
        }
        return false;
    }

    // itemDataContext is required so output items are cross-checked against real item data, not
    // just structurally validated (see PublicMD/Systems/Farming/Definition_and_Catalog.md invariants).
    public bool TryValidate(ItemDataContext itemDataContext, out string failureReason)
    {
        if (_definitions == null)
        {
            failureReason = "_definitions list is null";
            return false;
        }

        if (!itemDataContext)
        {
            failureReason = "itemDataContext is null";
            return false;
        }

        var seenCropIds = new HashSet<int>();
        var seenSeedIds = new HashSet<int>();

        for (int i = 0; i < _definitions.Count; ++i)
        {
            FarmProductionDefinition definition = _definitions[i];

            if (!definition)
            {
                failureReason = $"entry {i} is null";
                return false;
            }

            if (!definition.IsValid)
            {
                failureReason = $"'{definition.name}' has invalid values";
                return false;
            }

            if (!seenCropIds.Add(definition.CropId))
            {
                failureReason = $"duplicate crop id {definition.CropId} ('{definition.name}')";
                return false;
            }

            if (!seenSeedIds.Add(definition.SeedItemId)
                || !itemDataContext.TryGetItemInfo(definition.SeedItemId, out ItemInfo seed)
                || seed.Category != ItemCategory.Seed)
            {
                failureReason = $"'{definition.name}' seed item id {definition.SeedItemId} is duplicate, missing or not Seed";
                return false;
            }

            if (!itemDataContext.TryGetItemInfo(definition.OutputItemId, out _))
            {
                failureReason = $"'{definition.name}' output item id {definition.OutputItemId} not found in item data";
                return false;
            }
        }

        failureReason = null;
        return true;
    }
}
