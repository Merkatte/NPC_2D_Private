using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Display for one inventory-style slot, shared by the warehouse grid and the sell cart grid in
/// MerchantPopup. Knows nothing about drag, drop, or click behaviour — that lives in the sibling
/// components (ItemSlotDragHandle / CartSlotRemoveHandle) attached only where relevant, so this
/// class stays a pure view usable by both prefabs.
/// </summary>
public sealed class ItemSlotView : MonoBehaviour
{
    private const int EmptyItemId = -1;

    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _quantityText;
    [SerializeField] private Text _unitPriceText; // not wired on the cart prefab; null is expected there

    public int ItemId { get; private set; } = EmptyItemId;
    public bool IsEmpty => ItemId < 0;
    public string DisplayName => _nameText ? _nameText.text : string.Empty;

    public void Bind(int itemId, string displayName, int quantity, int unitPrice, Sprite icon)
    {
        ItemId = itemId;

        if (_nameText)
            _nameText.text = displayName;

        if (_quantityText)
            _quantityText.text = $"x{quantity}";

        if (_unitPriceText)
            _unitPriceText.text = $"{unitPrice}G";

        if (_iconImage)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon;
        }
    }

    public void Clear()
    {
        ItemId = EmptyItemId;

        if (_nameText)
            _nameText.text = string.Empty;

        if (_quantityText)
            _quantityText.text = string.Empty;

        if (_unitPriceText)
            _unitPriceText.text = string.Empty;

        if (_iconImage)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }
    }
}
