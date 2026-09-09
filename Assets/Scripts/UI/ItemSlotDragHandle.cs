using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag source for a warehouse slot. Knows nothing about the cart or the popup — it only shows a
/// ghost that follows the cursor while dragging. Whether a drop is accepted is entirely the drop
/// zone's decision (SellCartDropZone); this component never reaches into cart state itself.
///
/// MerchantPopup instantiates warehouse slots at runtime, so this component cannot receive its
/// DragGhostView dependency via a serialized field — Initialize() injects it right after
/// Instantiate, before the slot is ever interacted with.
/// </summary>
[RequireComponent(typeof(ItemSlotView))]
public sealed class ItemSlotDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ItemSlotView _slotView;
    private DragGhostView _ghost;

    public void Initialize(DragGhostView ghost)
    {
        _ghost = ghost;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slotView.IsEmpty || !_ghost)
            return;

        _ghost.Show(_slotView.DisplayName, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_slotView.IsEmpty || !_ghost)
            return;

        _ghost.MoveTo(eventData.position);
    }

    /// <summary>
    /// Only hides the ghost. Cart state is never touched here — a successful drop is committed
    /// entirely inside SellCartDropZone.OnDrop, so this works correctly regardless of whether the
    /// engine calls OnDrop before or after OnEndDrag.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghost)
            _ghost.Hide();
    }

    private void Awake()
    {
        _slotView = GetComponent<ItemSlotView>();
    }

    /// <summary>
    /// Defends against the popup being force-closed mid-drag (e.g. the caravan departs and hides
    /// the popup): OnEndDrag is never delivered to a disabled GameObject, so without this the
    /// ghost would be left showing on screen.
    /// </summary>
    private void OnDisable()
    {
        if (_ghost)
            _ghost.Hide();
    }
}
