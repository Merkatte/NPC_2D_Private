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
    [SerializeField] private Text _quantityText;

    private string _displayName = string.Empty;

    public int ItemId { get; private set; } = EmptyItemId;
    public bool IsEmpty => ItemId < 0;
    public string DisplayName => _displayName;

    public void Bind(int itemId, string displayName, int quantity, Sprite icon)
    {
        ItemId = itemId;
        _displayName = displayName;

        if (_quantityText)
            _quantityText.text = $"x{quantity}";

        if (_iconImage)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon;
            _iconImage.gameObject.SetActive(icon);
        }
    }

    public void Clear()
    {
        ItemId = EmptyItemId;
        _displayName = string.Empty;

        if (_quantityText)
            _quantityText.text = string.Empty;

        if (_iconImage)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _iconImage.gameObject.SetActive(false);
        }
    }
}
