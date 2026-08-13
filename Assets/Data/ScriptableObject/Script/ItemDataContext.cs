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
}
