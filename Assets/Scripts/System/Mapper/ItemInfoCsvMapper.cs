using System.Collections.Generic;
using UnityEngine;

// CSV columns: id,category,itemName,itemDescription,healthDelta,hungerDelta,thirstDelta,fatigueDelta,moodDelta,sellPrice
public static class ItemInfoCsvMapper
{
    private const int ColumnCount = 10;

    public static Dictionary<ItemCategory, List<ItemInfo>> Map(List<string[]> rows)
    {
        var itemInfos = new Dictionary<ItemCategory, List<ItemInfo>>();

        foreach (string[] row in rows)
        {
            if (!TryParseRow(row, out ItemInfo itemInfo))
                continue;

            if (!itemInfos.TryGetValue(itemInfo.Category, out List<ItemInfo> list))
            {
                list = new List<ItemInfo>();
                itemInfos[itemInfo.Category] = list;
            }

            list.Add(itemInfo);
        }

        return itemInfos;
    }

    private static bool TryParseRow(string[] row, out ItemInfo itemInfo)
    {
        itemInfo = default;

        if (row.Length < ColumnCount)
        {
            Debug.LogError($"ItemInfoCsvMapper: expected {ColumnCount} columns but got {row.Length}");
            return false;
        }

        if (!int.TryParse(row[0], out int id) ||
            !System.Enum.TryParse(row[1], true, out ItemCategory category) ||
            !float.TryParse(row[4], out float healthDelta) ||
            !float.TryParse(row[5], out float hungerDelta) ||
            !float.TryParse(row[6], out float thirstDelta) ||
            !float.TryParse(row[7], out float fatigueDelta) ||
            !float.TryParse(row[8], out float moodDelta) ||
            !int.TryParse(row[9], out int sellPrice))
        {
            Debug.LogError($"ItemInfoCsvMapper: failed to parse row '{string.Join(",", row)}'");
            return false;
        }

        var effect = new StatEffect(healthDelta, hungerDelta, thirstDelta, fatigueDelta, moodDelta);
        itemInfo = new ItemInfo(id, effect, category, row[2], row[3], sellPrice);
        return true;
    }
}
