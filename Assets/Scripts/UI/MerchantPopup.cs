using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trade UI for the merchant caravan — the first concrete PopBase in the project. Displays
/// MerchantTradeSite's offers and forwards sell attempts to it; never touches GoldManager or
/// item data directly. Concrete reference to MerchantTradeSite (no interface): this is a 1:1
/// feature-specific pairing, not a polymorphic UI/domain boundary like IHoverInfoSource.
/// </summary>
public class MerchantPopup : PopBase
{
    [SerializeField] private MerchantTradeSite _tradeSite;
    [SerializeField] private Text _offersText;
    [SerializeField] private Text _resultText;

    protected override void OnOpened()
    {
        RefreshOffers();

        if (_resultText)
            _resultText.text = string.Empty;
    }

    /// <summary>
    /// Wired from a sell button's OnClick (one per offered item, once real UI layout exists).
    /// Passes only intent — MerchantTradeSite re-checks price and stock itself.
    /// </summary>
    public void TrySell(int itemId, int quantity)
    {
        if (!_tradeSite)
            return;

        TradeResult result = _tradeSite.TryTrade(itemId, quantity);
        ShowResult(result);
        RefreshOffers();
    }

    private void RefreshOffers()
    {
        if (!_offersText)
            return;

        if (!_tradeSite)
        {
            _offersText.text = "준비중입니다.";
            return;
        }

        IReadOnlyList<MerchantOffer> offers = _tradeSite.GetAvailableOffers();
        if (offers.Count == 0)
        {
            _offersText.text = "준비중입니다.";
            return;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < offers.Count; ++i)
        {
            MerchantOffer offer = offers[i];
            builder.AppendLine($"{offer.DisplayName} x{offer.AvailableQuantity} — {offer.UnitPrice}G");
        }

        _offersText.text = builder.ToString();
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
}
