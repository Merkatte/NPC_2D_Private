/// <summary>
/// One sellable item as MerchantTradeSite reports it to the trade popup: what it is, what it's
/// worth, and how much of it is currently in the warehouse. A display value only — the popup
/// must not compute or cache a price itself; MerchantTradeSite re-checks the authoritative price
/// at the moment a trade is actually attempted.
/// </summary>
public readonly struct MerchantOffer
{
    public int ItemId { get; }
    public string DisplayName { get; }
    public int UnitPrice { get; }
    public int AvailableQuantity { get; }

    public MerchantOffer(int itemId, string displayName, int unitPrice, int availableQuantity)
    {
        ItemId = itemId;
        DisplayName = displayName;
        UnitPrice = unitPrice;
        AvailableQuantity = availableQuantity;
    }
}
