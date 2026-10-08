using System;
using System.Collections.Generic;
using System.Globalization;

public static class HousingCsvMapper
{
    public static bool TryMap(List<string[]> tierRows, List<string[]> optionRows, List<string[]> membershipRows,
        List<string[]> lifeRows, BuildingDataContext buildings, out IReadOnlyList<HouseTierDefinition> tiers,
        out HousingLifeSettings life, out string error)
    {
        tiers = null;
        life = null;
        error = "Housing CSV: invalid rows, duplicate IDs/effects, missing references, or decreasing capacity.";
        if (!buildings || !buildings.TryInitialize(out error) || tierRows == null || tierRows.Count == 0 ||
            optionRows == null || membershipRows == null || lifeRows == null) return false;
        error = "Housing CSV: invalid rows, duplicate IDs/effects, missing references, or decreasing capacity.";
        var options = new Dictionary<int, HousingOption>();
        foreach (string[] row in optionRows)
        {
            if (row.Length != 3 || !TryPositiveInt(row[0], out int id) || options.ContainsKey(id) ||
                !int.TryParse(row[1], out int effectId) || !Enum.IsDefined(typeof(HousingEffectType), effectId) ||
                !TryNonnegative(row[2], out float value)) return false;
            HousingEffectType effect = (HousingEffectType)effectId;
            if (effect == HousingEffectType.Capacity && (value < 1f || value >= int.MaxValue || value != Math.Floor(value))) return false;
            options.Add(id, new HousingOption(id, effect, value));
        }
        var buildingIds = new HashSet<int>();
        var tierBuildings = new SortedDictionary<int, int>();
        foreach (string[] row in tierRows)
        {
            if (row.Length != 2 || !TryPositiveInt(row[0], out int tier) || tierBuildings.ContainsKey(tier) ||
                !TryPositiveInt(row[1], out int buildingId) || !buildingIds.Add(buildingId) ||
                !buildings.TryGetBuildingDefinition(buildingId, out var building) || building.BuildingType != BuildingType.House) return false;
            tierBuildings.Add(tier, buildingId);
        }
        var memberships = new Dictionary<int, List<HousingOption>>();
        foreach (int tier in tierBuildings.Keys) memberships.Add(tier, new List<HousingOption>());
        foreach (string[] row in membershipRows)
        {
            if (row.Length != 2 || !TryPositiveInt(row[0], out int tier) || !memberships.TryGetValue(tier, out var list) ||
                !TryPositiveInt(row[1], out int optionId) || !options.TryGetValue(optionId, out var option)) return false;
            foreach (HousingOption existing in list)
                if (existing.EffectType == option.EffectType) return false;
            list.Add(option);
        }
        var result = new List<HouseTierDefinition>();
        int expectedTier = 1;
        int previousCapacity = 1;
        foreach (var entry in tierBuildings)
        {
            var definition = new HouseTierDefinition(entry.Key, entry.Value, memberships[entry.Key].ToArray());
            if (entry.Key != expectedTier++ || definition.Capacity < previousCapacity) return false;
            previousCapacity = definition.Capacity;
            result.Add(definition);
        }
        if (lifeRows.Count != 1 || lifeRows[0].Length != 5) return false;
        var values = new float[5];
        for (int i = 0; i < values.Length; ++i)
            if (!TryNonnegative(lifeRows[0][i], out values[i])) return false;
        if (values[0] <= 0f || values[1] <= 0f || values[4] <= 0f) return false;
        life = new HousingLifeSettings(values[0], values[1], values[2], values[3], values[4]);
        tiers = result.AsReadOnly();
        error = null;
        return true;
    }

    private static bool TryPositiveInt(string text, out int value)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0;

    private static bool TryNonnegative(string text, out float value)
        => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
           !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
}
