using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Owns only the local sale selection. Prices and transactions remain in MerchantTradeSite.
public sealed class MerchantPopup : PopBase
{
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
    [SerializeField] private GameObject[] _sellPanelRoots;
    [SerializeField] private Button _buyTab;
    [SerializeField] private Button _sellTab;
    [SerializeField] private Button _sellButton;
    [SerializeField] private Button _clearButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _buyUnderline;
    [SerializeField] private GameObject _sellUnderline;
    [SerializeField] private MerchantProductRowView _productRowTemplate;
    [SerializeField] private RectTransform _productRowParent;
    [SerializeField] private MerchantSaleLineView _saleLineTemplate;
    [SerializeField] private RectTransform _saleLineParent;
    [SerializeField] private Text _estimateText;
    [SerializeField] private Text _resultText;
    [SerializeField] private Text _emptyProductsText;
    [SerializeField] private Text _emptySalesText;
    [SerializeField] private Text _itemCountText;
    [SerializeField] private ItemIconEntry[] _itemIcons = Array.Empty<ItemIconEntry>();

    private readonly List<MerchantOffer> _offers = new List<MerchantOffer>();
    private readonly Dictionary<int, string> _knownNames = new Dictionary<int, string>();
    private readonly Dictionary<int, int> _pendingSales = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _quoteLine = new Dictionary<int, int>();
    private readonly List<int> _displayItems = new List<int>();
    private readonly List<int> _reconcileScratch = new List<int>();
    private readonly List<MerchantProductRowView> _productRows = new List<MerchantProductRowView>();
    private readonly List<MerchantSaleLineView> _saleLines = new List<MerchantSaleLineView>();
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;
    private bool _isSessionOpen;
    private Component _sourceOwner;
    private IClickPopupSource _source;
    private ResourceManager _subscribedResources;

    private bool EnsureConfigured()
    {
        if (_isConfigured) return true;
        bool valid = _tradeSite && _buyPanelRoot && _sellPanelRoots != null && _sellPanelRoots.Length > 0 &&
            _buyTab && _sellTab && _sellButton && _clearButton && _closeButton &&
            _productRowTemplate && _productRowTemplate.IsConfigured && _productRowParent &&
            _saleLineTemplate && _saleLineTemplate.IsConfigured && _saleLineParent &&
            _estimateText && _resultText && _emptyProductsText && _emptySalesText && _itemCountText;
        if (valid) foreach (GameObject panel in _sellPanelRoots) valid &= panel;
        if (!valid && !_hasLoggedConfigurationFailure)
        {
            Debug.LogError($"MerchantPopup '{name}': missing _tradeSite, panel, button, row template or text reference.", this);
            _hasLoggedConfigurationFailure = true;
        }
        _isConfigured = valid;
        return valid;
    }

    private void OnEnable()
    {
        if (!EnsureConfigured()) return;
        _buyTab.onClick.AddListener(ShowBuyMode);
        _sellTab.onClick.AddListener(ShowSellMode);
        _sellButton.onClick.AddListener(ConfirmSell);
        _clearButton.onClick.AddListener(ClearCart);
        _closeButton.onClick.AddListener(RequestClose);
    }

    private void OnDisable()
    {
        ResetSellSession();
        if (!_isConfigured) return;
        _buyTab.onClick.RemoveListener(ShowBuyMode);
        _sellTab.onClick.RemoveListener(ShowSellMode);
        _sellButton.onClick.RemoveListener(ConfirmSell);
        _clearButton.onClick.RemoveListener(ClearCart);
        _closeButton.onClick.RemoveListener(RequestClose);
    }

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!EnsureConfigured() || !_tradeSite.isActiveAndEnabled) return false;
        ResetSellSession();
        _source = source;
        _sourceOwner = source as Component;
        if (IsOpen) StartSession();
        return true;
    }

    protected override void OnOpened() => StartSession();
    protected override void OnBeforeClose() => ResetSellSession();

    private void StartSession()
    {
        if (!EnsureConfigured()) return;
        _isSessionOpen = true;
        _pendingSales.Clear();
        _knownNames.Clear();
        foreach (MerchantProductRowView row in _productRows) row.DiscardPendingInput();
        _subscribedResources = _tradeSite.Resources;
        if (_subscribedResources)
        {
            _subscribedResources.ResourcesChanged -= RefreshResourceOffers;
            _subscribedResources.ResourcesChanged += RefreshResourceOffers;
        }
        _resultText.text = string.Empty;
        ShowSellMode();
        RefreshResourceOffers();
    }

    private void Update()
    {
        if (!_isSessionOpen) return;
        if (!_tradeSite || !_tradeSite.isActiveAndEnabled ||
            (_source != null && (!_sourceOwner || !_sourceOwner.gameObject.activeInHierarchy ||
                !_source.TryGetClickPopup(out PopupType popupType) || popupType != PopupType.Merchant)))
            RequestClose();
    }

    public void ShowBuyMode() => SetMode(true);
    public void ShowSellMode() => SetMode(false);
    private void SetMode(bool buy)
    {
        if (!_isConfigured) return;
        CommitInputs();
        _buyPanelRoot.SetActive(buy);
        foreach (GameObject panel in _sellPanelRoots) panel.SetActive(!buy);
        _buyTab.interactable = !buy;
        _sellTab.interactable = buy;
        if (_buyUnderline) _buyUnderline.SetActive(buy);
        if (_sellUnderline) _sellUnderline.SetActive(!buy);
    }

    public void ConfirmSell()
    {
        if (!_isSessionOpen) return;
        CommitInputs();
        if (_pendingSales.Count == 0) return;
        TradeResult result = _tradeSite.TryTrade(_pendingSales);
        RefreshOffers();
        if (result == TradeResult.Success) _pendingSales.Clear();
        else if (result == TradeResult.OutOfStock) ReconcileCartWithStock();
        _resultText.text = result switch
        {
            TradeResult.Success => "판매 완료.",
            TradeResult.OutOfStock => "재고가 부족합니다.",
            TradeResult.InvalidRequest => "요청이 올바르지 않습니다.",
            _ => string.Empty,
        };
        RefreshViews();
    }

    public void ClearCart()
    {
        foreach (MerchantProductRowView row in _productRows) row.DiscardPendingInput();
        _pendingSales.Clear();
        if (_isConfigured) RefreshViews();
    }

    private void CommitInputs()
    {
        for (int i = 0; i < _productRows.Count; ++i)
            if (_productRows[i].gameObject.activeInHierarchy) _productRows[i].CommitPendingInput();
    }

    private void HandleQuantitySubmitted(int itemId, string value)
    {
        if (!_isSessionOpen) return;
        int.TryParse(value, out int requested);
        SetQuantity(itemId, requested);
    }

    private void HandleStepRequested(int itemId, int step)
    {
        if (!_isSessionOpen) return;
        _pendingSales.TryGetValue(itemId, out int current);
        SetQuantity(itemId, (int)Math.Min(int.MaxValue, Math.Max(0L, (long)current + step)));
    }

    private void SetQuantity(int itemId, int requested)
    {
        RefreshOffers();
        int quantity = Mathf.Clamp(requested, 0, GetAvailable(itemId));
        if (quantity == 0) _pendingSales.Remove(itemId);
        else _pendingSales[itemId] = quantity;
        RefreshViews();
    }

    // Existing helper components retain their public call contracts; new prefabs use row inputs.
    public bool CanAcceptDrop(int itemId) => _isSessionOpen && GetAvailable(itemId) > GetPending(itemId);
    public void RequestAddToCart(int itemId) => HandleStepRequested(itemId, 1);
    public void OnQuantityConfirmed(int itemId, int requestedQuantity)
    {
        if (!_isSessionOpen) return;
        SetQuantity(itemId, (int)Math.Min(int.MaxValue, Math.Max(0L, (long)GetPending(itemId) + requestedQuantity)));
    }
    public void OnQuantityCanceled(int itemId) { }
    public void RequestRemoveFromCart(int itemId)
    {
        if (_pendingSales.Remove(itemId) && _isConfigured) RefreshViews();
    }

    private void RefreshResourceOffers()
    {
        if (!_isSessionOpen) return;
        RefreshOffers();
        // Stock events never reconcile the requested cart. A failed OutOfStock trade owns recovery.
        RefreshViews();
    }

    private void RefreshOffers()
    {
        _offers.Clear();
        if (!_tradeSite) return;
        _offers.AddRange(_tradeSite.GetAvailableOffers());
        foreach (MerchantOffer offer in _offers) _knownNames[offer.ItemId] = offer.DisplayName;
    }

    private void RefreshViews()
    {
        _displayItems.Clear();
        foreach (MerchantOffer offer in _offers) _displayItems.Add(offer.ItemId);
        foreach (int itemId in _pendingSales.Keys) if (!_displayItems.Contains(itemId)) _displayItems.Add(itemId);
        for (int i = 0; i < _displayItems.Count; ++i)
        {
            int itemId = _displayItems[i];
            MerchantProductRowView row = GetProductRow(i);
            row.Bind(itemId, GetName(itemId), GetIcon(itemId), GetAvailable(itemId), GetPending(itemId), GetQuote(itemId, 1));
            row.gameObject.SetActive(true);
        }
        for (int i = _displayItems.Count; i < _productRows.Count; ++i) _productRows[i].gameObject.SetActive(false);
        int lineIndex = 0;
        foreach (int itemId in _displayItems)
        {
            int quantity = GetPending(itemId);
            if (quantity <= 0) continue;
            MerchantSaleLineView line = GetSaleLine(lineIndex++);
            line.Bind(GetName(itemId), quantity, GetQuote(itemId, quantity), quantity > GetAvailable(itemId));
            line.gameObject.SetActive(true);
        }
        for (int i = lineIndex; i < _saleLines.Count; ++i) _saleLines[i].gameObject.SetActive(false);
        _emptyProductsText.gameObject.SetActive(_displayItems.Count == 0);
        _emptySalesText.gameObject.SetActive(lineIndex == 0);
        _itemCountText.text = $"{_displayItems.Count}종";
        _estimateText.text = _tradeSite.TryGetSaleQuote(_pendingSales, out int total) ? $"{total} G" : "— G";
        // Keep the button reachable while an uncommitted input is focused, including an empty cart.
        _sellButton.interactable = true;
        _clearButton.interactable = true;
    }

    private MerchantProductRowView GetProductRow(int index)
    {
        while (_productRows.Count <= index)
        {
            MerchantProductRowView row = Instantiate(_productRowTemplate, _productRowParent);
            row.QuantitySubmitted += HandleQuantitySubmitted;
            row.StepRequested += HandleStepRequested;
            _productRows.Add(row);
        }
        return _productRows[index];
    }

    private MerchantSaleLineView GetSaleLine(int index)
    {
        while (_saleLines.Count <= index) _saleLines.Add(Instantiate(_saleLineTemplate, _saleLineParent));
        return _saleLines[index];
    }

    private string GetQuote(int itemId, int quantity)
    {
        _quoteLine.Clear();
        _quoteLine[itemId] = quantity;
        return _tradeSite.TryGetSaleQuote(_quoteLine, out int amount) ? $"{amount} G" : "— G";
    }

    private int GetPending(int itemId) => _pendingSales.TryGetValue(itemId, out int quantity) ? quantity : 0;
    private string GetName(int itemId) => _knownNames.TryGetValue(itemId, out string name) ? name : itemId.ToString();
    private int GetAvailable(int itemId)
    {
        foreach (MerchantOffer offer in _offers) if (offer.ItemId == itemId) return offer.AvailableQuantity;
        return 0;
    }
    private Sprite GetIcon(int itemId)
    {
        if (_itemIcons != null) foreach (ItemIconEntry entry in _itemIcons) if (entry.ItemId == itemId) return entry.Icon;
        return null;
    }

    private void ReconcileCartWithStock()
    {
        _reconcileScratch.Clear();
        _reconcileScratch.AddRange(_pendingSales.Keys);
        foreach (int itemId in _reconcileScratch)
        {
            int available = GetAvailable(itemId);
            if (available == 0) _pendingSales.Remove(itemId);
            else _pendingSales[itemId] = Mathf.Min(_pendingSales[itemId], available);
        }
    }

    private void ResetSellSession()
    {
        if (_subscribedResources) _subscribedResources.ResourcesChanged -= RefreshResourceOffers;
        _subscribedResources = null;
        _isSessionOpen = false;
        _source = null;
        _sourceOwner = null;
        _pendingSales.Clear();
        foreach (MerchantProductRowView row in _productRows) if (row) row.gameObject.SetActive(false);
        foreach (MerchantSaleLineView line in _saleLines) if (line) line.gameObject.SetActive(false);
    }
}
