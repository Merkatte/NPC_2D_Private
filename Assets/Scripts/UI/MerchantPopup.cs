using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trade UI for the merchant caravan — the first concrete PopBase in the project. Owns the whole
/// sell session's local state (drag/drop warehouse grid, sell cart, quantity prompt) and forwards
/// only finished intent to MerchantTradeSite; never touches GoldManager or item pricing directly.
/// Concrete reference to MerchantTradeSite (no interface): this is a 1:1 feature-specific pairing,
/// not a polymorphic UI/domain boundary like IHoverInfoSource.
/// </summary>
public sealed class MerchantPopup : PopBase
{
    private const string EstimateLabel = "예상 판매 금액";

    /// <summary>
    /// Pure UI itemId-to-icon mapping. Kept nested here rather than in Assets/Data/Struct: that
    /// folder is for values that cross domain boundaries, and this one never leaves the popup.
    /// ItemInfo/the item CSV are intentionally left untouched by this feature.
    /// </summary>
    [Serializable]
    private struct ItemIconEntry
    {
        [SerializeField] private int _itemId;
        [SerializeField] private Sprite _icon;

        public int ItemId => _itemId;
        public Sprite Icon => _icon;
    }

    [SerializeField] private MerchantTradeSite _tradeSite;

    [SerializeField] private GameObject _buyPanelRoot;
    [SerializeField] private GameObject _sellPanelRoot;

    [SerializeField] private ItemSlotView _warehouseSlotPrefab;
    [SerializeField] private RectTransform _warehouseSlotParent;
    [SerializeField] private ItemSlotView _cartSlotPrefab;
    [SerializeField] private RectTransform _cartSlotParent;
    [SerializeField, Min(1)] private int _minimumVisibleCartSlots = 4;

    [SerializeField] private SellCartDropZone _cartDropZone;
    [SerializeField] private DragGhostView _dragGhost;
    [SerializeField] private QuantityPromptPanel _quantityPrompt;
    [SerializeField] private Text _estimateText;
    [SerializeField] private Text _resultText;
    [SerializeField] private ItemIconEntry[] _itemIcons = Array.Empty<ItemIconEntry>();

    private readonly List<MerchantOffer> _offers = new List<MerchantOffer>();
    private readonly Dictionary<int, int> _pendingSales = new Dictionary<int, int>();
    private readonly List<ItemSlotView> _warehouseSlots = new List<ItemSlotView>();
    private readonly List<ItemSlotView> _cartSlots = new List<ItemSlotView>();
    private readonly List<int> _reconcileScratch = new List<int>();

    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        if (!_tradeSite || !_buyPanelRoot || !_sellPanelRoot ||
            !_warehouseSlotPrefab || !_warehouseSlotParent ||
            !_cartSlotPrefab || !_cartSlotParent ||
            !_cartDropZone || !_dragGhost || !_quantityPrompt)
        {
            ReportConfigurationFailure(
                "missing a required reference (_tradeSite/_buyPanelRoot/_sellPanelRoot/" +
                "_warehouseSlotPrefab/_warehouseSlotParent/_cartSlotPrefab/_cartSlotParent/" +
                "_cartDropZone/_dragGhost/_quantityPrompt)");
            return;
        }

        _isConfigured = true;
    }

    /// <summary>
    /// Session start. Not OnBeforeOpen(): PopBase.Open() calls OnBeforeOpen() before
    /// SetActive(true), so on the very first open Awake() has not run yet and _isConfigured would
    /// still be false.
    /// </summary>
    protected override void OnOpened()
    {
        if (!_isConfigured)
            return;

        _pendingSales.Clear();
        ShowSellMode();
        RefreshOffers();
        RebuildWarehouseSlots();
        RebuildCartSlots();
        RefreshEstimate();

        if (_resultText)
            _resultText.text = string.Empty;
    }

    protected override void OnBeforeClose()
    {
        ResetSellSession();
    }

    /// <summary>
    /// Defends the case where the popup is force-disabled without going through PopBase.Close()
    /// (e.g. UIManager.HideAll, or the parent Canvas being disabled) — idempotent alongside
    /// OnBeforeClose so it is safe however the popup actually ends up hidden.
    /// </summary>
    private void OnDisable()
    {
        ResetSellSession();
    }

    public void ShowBuyMode()
    {
        if (_buyPanelRoot)
            _buyPanelRoot.SetActive(true);
        if (_sellPanelRoot)
            _sellPanelRoot.SetActive(false);
    }

    public void ShowSellMode()
    {
        if (_buyPanelRoot)
            _buyPanelRoot.SetActive(false);
        if (_sellPanelRoot)
            _sellPanelRoot.SetActive(true);
    }

    /// <summary>
    /// "Nothing changes on failure" means the warehouse and gold stay untouched — not that the UI
    /// cart is frozen. OutOfStock reconciles the cart against fresh stock (a real inventory
    /// mismatch, worth fixing automatically so retrying doesn't just fail the same way again).
    /// InvalidRequest leaves the cart alone: its causes (gold cap, unsellable item, bad price) have
    /// nothing to do with quantities, so clamping against stock would not fix anything.
    /// </summary>
    public void ConfirmSell()
    {
        if (!_isConfigured || _pendingSales.Count == 0)
            return;

        TradeResult result = _tradeSite.TryTrade(_pendingSales);
        switch (result)
        {
            case TradeResult.Success:
                _pendingSales.Clear();
                RefreshOffers();
                break;

            case TradeResult.OutOfStock:
                RefreshOffers();
                ReconcileCartWithStock();
                break;

            case TradeResult.InvalidRequest:
                break;
        }

        ShowResult(result);
        RebuildWarehouseSlots();
        RebuildCartSlots();
        RefreshEstimate();
    }

    public void ClearCart()
    {
        if (_pendingSales.Count == 0)
            return;

        _pendingSales.Clear();
        RebuildWarehouseSlots();
        RebuildCartSlots();
        RefreshEstimate();
    }

    public bool CanAcceptDrop(int itemId)
    {
        return _isConfigured && GetRemaining(itemId) > 0;
    }

    /// <summary>
    /// Drop-zone entry point. A single remaining unit skips the quantity prompt entirely — asking
    /// "how many? (only 1 available)" would just be an extra click for a foregone choice.
    /// </summary>
    public void RequestAddToCart(int itemId)
    {
        if (!_isConfigured)
            return;

        int remaining = GetRemaining(itemId);
        if (remaining <= 0)
            return;

        if (remaining == 1)
        {
            CommitToCart(itemId, 1);
            return;
        }

        TryGetOffer(itemId, out MerchantOffer offer);
        _quantityPrompt.Open(itemId, offer.DisplayName, remaining);
    }

    /// <summary>
    /// Re-queries live stock before committing: the prompt may have sat open for a while, during
    /// which the snapshot it was opened with could have gone stale.
    /// </summary>
    public void OnQuantityConfirmed(int itemId, int requestedQuantity)
    {
        RefreshOffers();
        int clamped = Mathf.Min(requestedQuantity, GetRemaining(itemId));

        if (clamped <= 0)
        {
            ShowMessage("재고가 없어졌습니다.");
            RebuildWarehouseSlots();
            RebuildCartSlots();
            RefreshEstimate();
            return;
        }

        CommitToCart(itemId, clamped);
    }

    public void OnQuantityCanceled(int itemId)
    {
        // Cart is intentionally left untouched — canceling commits nothing.
    }

    public void RequestRemoveFromCart(int itemId)
    {
        if (!_pendingSales.Remove(itemId))
            return;

        RebuildWarehouseSlots();
        RebuildCartSlots();
        RefreshEstimate();
    }

    private void RefreshOffers()
    {
        _offers.Clear();
        if (_tradeSite)
            _offers.AddRange(_tradeSite.GetAvailableOffers());
    }

    private ItemSlotView RentWarehouseSlot(int index)
    {
        while (_warehouseSlots.Count <= index)
        {
            ItemSlotView slot = Instantiate(_warehouseSlotPrefab, _warehouseSlotParent);
            if (slot.TryGetComponent(out ItemSlotDragHandle dragHandle))
                dragHandle.Initialize(_dragGhost);
            _warehouseSlots.Add(slot);
        }

        return _warehouseSlots[index];
    }

    private ItemSlotView RentCartSlot(int index)
    {
        while (_cartSlots.Count <= index)
        {
            ItemSlotView slot = Instantiate(_cartSlotPrefab, _cartSlotParent);
            if (slot.TryGetComponent(out CartSlotRemoveHandle removeHandle))
                removeHandle.Initialize(this);
            _cartSlots.Add(slot);
        }

        return _cartSlots[index];
    }

    private void RebuildWarehouseSlots()
    {
        int index = 0;
        for (int i = 0; i < _offers.Count; ++i)
        {
            MerchantOffer offer = _offers[i];
            int remaining = GetRemaining(offer.ItemId);
            if (remaining <= 0)
                continue;

            ItemSlotView slot = RentWarehouseSlot(index++);
            slot.gameObject.SetActive(true);
            slot.Bind(offer.ItemId, offer.DisplayName, remaining, offer.UnitPrice, TryGetIcon(offer.ItemId));
        }

        for (int i = index; i < _warehouseSlots.Count; ++i)
        {
            _warehouseSlots[i].Clear();
            _warehouseSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Always shows at least _minimumVisibleCartSlots slots, even with an empty cart — without
    /// this, the cart grid would start with zero visible slots and never look like "the same kind
    /// of slot grid" the warehouse side does. Iterates _offers order (not the pending dictionary)
    /// so cart slot order stays stable across rebuilds regardless of Dictionary enumeration order.
    /// </summary>
    private void RebuildCartSlots()
    {
        int index = 0;
        for (int i = 0; i < _offers.Count; ++i)
        {
            int itemId = _offers[i].ItemId;
            if (!_pendingSales.TryGetValue(itemId, out int quantity))
                continue;

            ItemSlotView slot = RentCartSlot(index++);
            slot.gameObject.SetActive(true);
            slot.Bind(itemId, _offers[i].DisplayName, quantity, _offers[i].UnitPrice, TryGetIcon(itemId));
        }

        int visibleCount = Mathf.Max(_minimumVisibleCartSlots, index);
        for (int i = index; i < visibleCount; ++i)
        {
            ItemSlotView slot = RentCartSlot(i);
            slot.gameObject.SetActive(true);
            slot.Clear();
        }

        for (int i = visibleCount; i < _cartSlots.Count; ++i)
        {
            _cartSlots[i].Clear();
            _cartSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Never computes UnitPrice*quantity itself — an empty cart shows "0 G" without even calling
    /// TradeSite, and a non-empty cart always re-asks TradeSite.TryGetSaleQuote so the displayed
    /// estimate and the amount an actual sale would charge can never drift apart.
    /// </summary>
    private void RefreshEstimate()
    {
        if (!_estimateText)
            return;

        if (_pendingSales.Count == 0)
        {
            _estimateText.text = $"{EstimateLabel}: 0 G";
            return;
        }

        if (_tradeSite && _tradeSite.TryGetSaleQuote(_pendingSales, out int total))
            _estimateText.text = $"{EstimateLabel}: {total} G";
        else
            _estimateText.text = $"{EstimateLabel}: - G";
    }

    private bool TryGetOffer(int itemId, out MerchantOffer offer)
    {
        for (int i = 0; i < _offers.Count; ++i)
        {
            if (_offers[i].ItemId == itemId)
            {
                offer = _offers[i];
                return true;
            }
        }

        offer = default;
        return false;
    }

    private int GetAvailable(int itemId)
    {
        return TryGetOffer(itemId, out MerchantOffer offer) ? offer.AvailableQuantity : 0;
    }

    private int GetRemaining(int itemId)
    {
        _pendingSales.TryGetValue(itemId, out int pending);
        return GetAvailable(itemId) - pending;
    }

    private Sprite TryGetIcon(int itemId)
    {
        if (_itemIcons == null)
            return null;

        for (int i = 0; i < _itemIcons.Length; ++i)
        {
            if (_itemIcons[i].ItemId == itemId)
                return _itemIcons[i].Icon;
        }

        return null;
    }

    private void CommitToCart(int itemId, int quantity)
    {
        _pendingSales.TryGetValue(itemId, out int existing);
        _pendingSales[itemId] = existing + quantity;

        RebuildWarehouseSlots();
        RebuildCartSlots();
        RefreshEstimate();
    }

    /// <summary>
    /// OutOfStock-only recovery: clamps every pending line down to whatever is actually available
    /// now (dropping lines that hit zero), so a retry doesn't immediately fail on the same stale
    /// quantity. Never called for InvalidRequest — that failure has nothing to do with quantities.
    /// </summary>
    private void ReconcileCartWithStock()
    {
        _reconcileScratch.Clear();
        _reconcileScratch.AddRange(_pendingSales.Keys);

        for (int i = 0; i < _reconcileScratch.Count; ++i)
        {
            int itemId = _reconcileScratch[i];
            int available = GetAvailable(itemId);

            if (available <= 0)
            {
                _pendingSales.Remove(itemId);
                continue;
            }

            if (_pendingSales[itemId] > available)
                _pendingSales[itemId] = available;
        }
    }

    /// <summary>
    /// Idempotent: safe to call from both OnBeforeClose and OnDisable regardless of which fires,
    /// or whether both do. The cart is never worth preserving across a close — every line in it is
    /// still fully present in the warehouse, so there is nothing external left to roll back.
    /// </summary>
    private void ResetSellSession()
    {
        if (_quantityPrompt)
            _quantityPrompt.Close();
        if (_dragGhost)
            _dragGhost.Hide();

        _pendingSales.Clear();

        for (int i = 0; i < _warehouseSlots.Count; ++i)
        {
            _warehouseSlots[i].Clear();
            _warehouseSlots[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < _cartSlots.Count; ++i)
        {
            _cartSlots[i].Clear();
            _cartSlots[i].gameObject.SetActive(false);
        }

        if (_resultText)
            _resultText.text = string.Empty;
        if (_estimateText)
            _estimateText.text = $"{EstimateLabel}: 0 G";
    }

    private void ShowMessage(string message)
    {
        if (_resultText)
            _resultText.text = message;
    }

    private void ShowResult(TradeResult result)
    {
        if (!_resultText)
            return;

        _resultText.text = result switch
        {
            TradeResult.Success => "판매 완료.",
            TradeResult.OutOfStock => "재고가 부족합니다.",
            TradeResult.InvalidRequest => "요청이 올바르지 않습니다.",
            _ => string.Empty,
        };
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantPopup '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
