using System;
using System.Collections.Generic;
using System.Globalization;

public static class BuildingDefinitionCsvMapper
{
    public static bool TryMap(List<string[]> rows, List<string[]> costRows, ItemDataContext items,
        out IReadOnlyList<BuildingDefinition> definitions, out string error)
    {
        definitions = Array.Empty<BuildingDefinition>();
        error = "BuildingData.csv: missing definitions or item catalog.";
        if (rows == null || rows.Count == 0 || costRows == null || !items)
            return false;
        var costs = new Dictionary<int, Dictionary<int, int>>();
        for (int i = 0; i < costRows.Count; ++i)
        {
            string[] row = costRows[i];
            error = $"BuildingCost.csv row {i + 2}: invalid or duplicate building/item ID or quantity.";
            if (row.Length != 3 || !int.TryParse(row[0], out int buildingId) || buildingId <= 0
                || !int.TryParse(row[1], out int itemId) || !items.TryGetItemInfo(itemId, out _)
                || !int.TryParse(row[2], out int quantity) || quantity <= 0)
                return false;
            if (!costs.TryGetValue(buildingId, out var cost))
                costs.Add(buildingId, cost = new Dictionary<int, int>());
            if (cost.ContainsKey(itemId))
                return false;
            cost.Add(itemId, quantity);
        }
        var result = new List<BuildingDefinition>();
        var ids = new HashSet<int>();
        for (int i = 0; i < rows.Count; ++i)
        {
            string[] row = rows[i];
            error = $"BuildingData.csv row {i + 2}: invalid/duplicate ID, type, name, work, workers, capacity or cost.";
            if (row.Length != 6 || !int.TryParse(row[0], out int id) || id <= 0 || !ids.Add(id)
                || !Enum.TryParse(row[1], false, out BuildingType type) || !Enum.IsDefined(typeof(BuildingType), type)
                || (type != BuildingType.Pub && type != BuildingType.Inn && type != BuildingType.Warehouse
                    && type != BuildingType.GuardPost && type != BuildingType.Farm && type != BuildingType.House)
                || string.IsNullOrWhiteSpace(row[2])
                || !float.TryParse(row[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float work)
                || work <= 0f || float.IsNaN(work) || float.IsInfinity(work)
                || !int.TryParse(row[4], out int workers) || workers <= 0
                || !int.TryParse(row[5], out int capacity) || capacity < 0
                || (type == BuildingType.Warehouse ? capacity <= 0 : capacity != 0)
                || !costs.TryGetValue(id, out var cost))
                return false;
            result.Add(new BuildingDefinition(id, type, row[2], work, workers, capacity, cost));
        }
        foreach (int id in costs.Keys)
        {
            if (ids.Contains(id))
                continue;
            error = $"BuildingCost.csv: unknown buildingId {id}.";
            return false;
        }
        definitions = result.AsReadOnly();
        error = null;
        return true;
    }
}
