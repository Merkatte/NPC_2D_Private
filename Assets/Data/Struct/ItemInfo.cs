using UnityEngine;

public readonly struct ItemInfo
{
    public int ID { get; }
    public ItemCategory Category { get; }
    public string ItemName { get; }
    public string ItemDescription { get; }
    public StatEffect Effect { get; }
    public int SellPrice { get; }
    public bool UsesStorage { get; }
    public bool ShowInWarehouse { get; }


    public ItemInfo(int id, StatEffect effect, ItemCategory category, string itemName, string itemDescription,
        int sellPrice, bool usesStorage = true, bool showInWarehouse = true)
    {
        ID = id;
        Effect = effect;
        Category = category;
        ItemName = itemName;
        ItemDescription = itemDescription;
        SellPrice = sellPrice;
        UsesStorage = usesStorage;
        ShowInWarehouse = showInWarehouse;
    }
}
