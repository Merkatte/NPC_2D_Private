using UnityEngine;
using UnityEngine.UI;

public sealed class MerchantSaleLineView : MonoBehaviour
{
    [SerializeField] private Text _itemAndQuantityText;
    [SerializeField] private Text _amountText;
    [SerializeField] private Color _normalColor = new Color(0.25f, 0.18f, 0.12f);
    [SerializeField] private Color _shortageColor = new Color(0.7f, 0.15f, 0.1f);
    public bool IsConfigured => _itemAndQuantityText && _amountText;

    public void Bind(string displayName, int quantity, string amount, bool shortage)
    {
        if (!IsConfigured) return;
        _itemAndQuantityText.text = $"{displayName} × {quantity}" + (shortage ? " · 재고 부족" : string.Empty);
        _itemAndQuantityText.color = shortage ? _shortageColor : _normalColor;
        _amountText.text = amount;
    }
}
