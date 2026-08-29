using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataContext", menuName = "Scriptable Objects/ItemDataContext")]
public class ItemDataContext : ScriptableObject
{
    [SerializeField] private TextAsset _itemTextData;

    private Dictionary<ItemCategory, List<ItemInfo>> _itemInfos;

    public Dictionary<ItemCategory, List<ItemInfo>> ItemInfos()
    {
        if (_itemInfos == null)
            _itemInfos = ItemInfoCsvMapper.Map(CSVParser.ParseRows(_itemTextData));

        return _itemInfos;
    }

    public bool TryGetItemInfo(int id, out ItemInfo info)
    {
        foreach (List<ItemInfo> items in ItemInfos().Values)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i].ID != id)
                    continue;

                info = items[i];
                return true;
            }
        }

        info = default;
        return false;
    }
}
