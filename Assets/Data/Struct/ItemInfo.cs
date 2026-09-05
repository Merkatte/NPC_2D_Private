using UnityEngine;

public readonly struct ItemInfo
{
    public int ID { get; }
    public ItemCategory Category { get; }
    public string ItemName { get; }
    public string ItemDescription { get; }
    public StatEffect Effect { get; }
    public int SellPrice { get; }


    public ItemInfo(int id, StatEffect effect, ItemCategory category, string itemName, string itemDescription,
        int sellPrice)
    {
        ID = id;
        Effect = effect;
        Category = category;
        ItemName = itemName;
        ItemDescription = itemDescription;
        SellPrice = sellPrice;
    }
}
