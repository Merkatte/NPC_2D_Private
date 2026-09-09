using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The only component that knows GoldManager for the merchant feature. Scoped to merchant trades
/// only — future gold-spending features (recruitment, upgrades) get their own small provider
/// each, rather than being routed through this one. Not an IInteractionProvider: that system is
/// for Worker NPC action-queue interactions, and a trade here is a direct player-UI transaction,
/// a different call path entirely.
/// </summary>
public sealed class MerchantTradeSite : MonoBehaviour
{
    [SerializeField] private WarehouseInventory _warehouse;
    [SerializeField] private CropCatalog _cropCatalog;
    [SerializeField] private GoldManager _goldManager;
    [SerializeField] private MonoBehaviour _dataManagerSource;

    private IDataManager _dataManager;
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        _dataManager = _dataManagerSource as IDataManager;

        if (!_warehouse || !_cropCatalog || !_goldManager || _dataManager == null)
        {
            ReportConfigurationFailure(
                "missing _warehouse/_cropCatalog/_goldManager, or _dataManagerSource does not implement IDataManager");
            return;
        }

        _isConfigured = true;
    }

    /// <summary>
    /// Display-only read model for the popup. Recomputed on demand (called when the popup opens,
    /// not every frame) rather than cached, since warehouse stock can change between visits. Skips
    /// non-positive prices for the same reason TryComputeTotal rejects them — a listed item that
    /// can never actually be sold would just be a dead-end for the player.
    /// </summary>
    public IReadOnlyList<MerchantOffer> GetAvailableOffers()
    {
        var offers = new List<MerchantOffer>();

        if (!_isConfigured)
            return offers;

        IReadOnlyList<FarmProductionDefinition> definitions = _cropCatalog.Definitions;
        for (int i = 0; i < definitions.Count; ++i)
        {
            FarmProductionDefinition definition = definitions[i];
            if (!definition)
                continue;

            int itemId = definition.OutputItemId;
            int quantity = _warehouse.GetQuantity(itemId);
            if (quantity <= 0)
                continue;

            if (!_dataManager.TryGetItemInfo(itemId, out ItemInfo info))
                continue;

            if (info.SellPrice <= 0)
                continue;

            offers.Add(new MerchantOffer(itemId, info.ItemName, info.SellPrice, quantity));
        }

        return offers;
    }

    /// <summary>
    /// Pure read: computes what a batch would cost without touching warehouse or gold. Shares
    /// TryComputeTotal with TryTrade so the displayed estimate and the actual charged total can
    /// never drift apart. An empty cart is defined as a successful zero-total quote — the popup's
    /// own fast path skips calling this when the cart is empty, but callers that do pass an empty
    /// dictionary get a well-defined answer instead of a rejection.
    /// </summary>
    public bool TryGetSaleQuote(IReadOnlyDictionary<int, int> quantitiesByItemId, out int totalPrice)
    {
        if (quantitiesByItemId != null && quantitiesByItemId.Count == 0)
        {
            totalPrice = 0;
            return true;
        }

        return TryComputeTotal(quantitiesByItemId, out totalPrice);
    }

    /// <summary>
    /// The popup passes only intent (itemId -> quantity per line) — never a pre-computed price.
    /// Price, sellability, and overflow are all re-checked here from authoritative sources, so a
    /// stale or client-guessed cart from the popup can never be trusted for the actual transaction.
    /// All-or-nothing across the whole cart: warehouse and gold are only touched once every line
    /// has passed every check.
    /// </summary>
    public TradeResult TryTrade(IReadOnlyDictionary<int, int> quantitiesByItemId)
    {
        if (!_isConfigured)
            return TradeResult.InvalidRequest;

        if (!TryComputeTotal(quantitiesByItemId, out int total))
            return TradeResult.InvalidRequest;

        // Checked before touching the warehouse: GoldManager.Add saturates at int.MaxValue instead
        // of failing, so without this guard a sale could remove every item from the warehouse while
        // only partially crediting the gold for it.
        if ((long)_goldManager.CurrentGold + total > int.MaxValue)
            return TradeResult.InvalidRequest;

        if (!_warehouse.TryRemoveBatch(quantitiesByItemId))
            return TradeResult.OutOfStock;

        _goldManager.Add(total);
        return TradeResult.Success;
    }

    /// <summary>
    /// Sellability and price-total computation shared by TryGetSaleQuote and TryTrade so a quote
    /// and the trade it describes can never use different rules. Pure — never mutates warehouse or
    /// gold. Rejects non-positive per-line prices individually (not just a non-positive grand
    /// total) since a mix of positive and negative prices could otherwise sum to a positive total
    /// while still being nonsense. Promotes to long before multiplying so the multiplication itself
    /// cannot overflow, then checks the running sum against int.MaxValue before every addition.
    /// </summary>
    private bool TryComputeTotal(IReadOnlyDictionary<int, int> quantitiesByItemId, out int total)
    {
        total = 0;

        if (quantitiesByItemId == null || quantitiesByItemId.Count == 0)
            return false;

        long runningTotal = 0;
        foreach (KeyValuePair<int, int> entry in quantitiesByItemId)
        {
            int itemId = entry.Key;
            int quantity = entry.Value;

            if (quantity <= 0)
                return false;

            if (!IsSellableItem(itemId))
                return false;

            if (!_dataManager.TryGetItemInfo(itemId, out ItemInfo info))
                return false;

            if (info.SellPrice <= 0)
                return false;

            long lineTotal = (long)info.SellPrice * quantity;
            if (lineTotal > int.MaxValue - runningTotal)
                return false;

            runningTotal += lineTotal;
        }

        total = (int)runningTotal;
        return true;
    }

    /// <summary>
    /// Whether itemId is currently a farm-output item the catalog recognizes, independent of
    /// whether the warehouse happens to hold any of it right now. TryTrade must not simply trust
    /// that a caller-supplied itemId was ever a real offer — GetAvailableOffers() already filters
    /// to this same set, so this makes TryTrade enforce that filter itself instead of relying on
    /// the UI to have only ever sent well-formed requests.
    /// </summary>
    private bool IsSellableItem(int itemId)
    {
        IReadOnlyList<FarmProductionDefinition> definitions = _cropCatalog.Definitions;
        for (int i = 0; i < definitions.Count; ++i)
        {
            FarmProductionDefinition definition = definitions[i];
            if (definition && definition.OutputItemId == itemId)
                return true;
        }

        return false;
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantTradeSite '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
