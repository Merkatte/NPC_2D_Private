using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop target for the sell cart panel. A fixed child of the MerchantPopup prefab (not a runtime
/// instantiation), so the popup reference is a plain serialized field. Requires an Image with
/// raycastTarget enabled on the same GameObject so it actually receives OnDrop.
/// </summary>
public sealed class SellCartDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private MerchantPopup _popup;

    public void OnDrop(PointerEventData eventData)
    {
        if (!_popup || !eventData.pointerDrag)
            return;

        // Requiring ItemSlotDragHandle specifically (not just any ItemSlotView) rules out a cart
        // slot — which only carries CartSlotRemoveHandle, never a drag handle — ever being read as
        // a valid drop source. Without this check a stray pointerDrag reference could route a cart
        // slot's own ItemSlotView back into RequestAddToCart.
        if (!eventData.pointerDrag.TryGetComponent(out ItemSlotDragHandle dragHandle))
            return;

        if (!dragHandle.TryGetComponent(out ItemSlotView slotView) || slotView.IsEmpty)
            return;

        if (!_popup.CanAcceptDrop(slotView.ItemId))
            return;

        _popup.RequestAddToCart(slotView.ItemId);
    }
}
