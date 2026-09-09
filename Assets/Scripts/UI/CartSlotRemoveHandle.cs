using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Click-to-remove for a sell cart slot. MerchantPopup instantiates cart slots at runtime, so —
/// like ItemSlotDragHandle's DragGhostView — the MerchantPopup reference cannot be a serialized
/// field (a runtime-instantiated prefab instance cannot pre-reference the popup that will create
/// it). Initialize() injects it right after Instantiate.
/// </summary>
[RequireComponent(typeof(ItemSlotView))]
public sealed class CartSlotRemoveHandle : MonoBehaviour, IPointerClickHandler
{
    private ItemSlotView _slotView;
    private MerchantPopup _popup;

    public void Initialize(MerchantPopup popup)
    {
        _popup = popup;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_slotView.IsEmpty || !_popup)
            return;

        _popup.RequestRemoveFromCart(_slotView.ItemId);
    }

    private void Awake()
    {
        _slotView = GetComponent<ItemSlotView>();
    }
}
