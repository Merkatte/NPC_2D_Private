using System;
using System.Collections.Generic;

public sealed class HouseTierDefinition
{
    public int Tier { get; }
    public int BuildingId { get; }
    public int Capacity => (int)GetEffect(HousingEffectType.Capacity);
    public IReadOnlyList<HousingOption> Options { get; }

    public HouseTierDefinition(int tier, int buildingId, HousingOption[] options)
    {
        Tier = tier;
        BuildingId = buildingId;
        Options = Array.AsReadOnly((HousingOption[])options.Clone());
    }

    public float GetEffect(HousingEffectType type)
    {
        foreach (HousingOption option in Options)
            if (option.EffectType == type) return option.Value;
        return 0f;
    }
}
