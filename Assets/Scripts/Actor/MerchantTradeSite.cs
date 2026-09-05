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
    /// not every frame) rather than cached, since warehouse stock can change between visits.
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

            offers.Add(new MerchantOffer(itemId, info.ItemName, info.SellPrice, quantity));
        }

        return offers;
    }

    /// <summary>
    /// The popup passes only intent (itemId, quantity) — never a pre-computed price. The price
    /// used here is always looked up fresh from DataManager, so a stale or client-guessed number
    /// from the popup can never be trusted for the actual transaction.
    /// </summary>
    public TradeResult TryTrade(int itemId, int quantity)
    {
        if (!_isConfigured || quantity <= 0)
            return TradeResult.InvalidRequest;

        if (!_dataManager.TryGetItemInfo(itemId, out ItemInfo info))
            return TradeResult.InvalidRequest;

        if (_warehouse.GetQuantity(itemId) < quantity)
            return TradeResult.OutOfStock;

        if (!_warehouse.TryRemove(itemId, quantity, out _))
            return TradeResult.OutOfStock;

        _goldManager.Add(info.SellPrice * quantity);
        return TradeResult.Success;
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantTradeSite '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
