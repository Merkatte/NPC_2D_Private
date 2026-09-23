using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pointer/raycast input adapter for discrete clicks. Detects the <see cref="IClickPopupSource"/>
/// under the mouse on the press frame and forwards popup-open intent to <see cref="IUIService"/>.
/// Knows nothing about any concrete domain type or concrete UI type, and nothing about *why* a
/// source declines a click — a source that is not currently clickable simply returns false.
///
/// Not a PointerHoverRouter extension: hover retains enter/exit state across frames and throttles
/// a continuous poll, while a click is a single-frame edge with no state to retain — polling it
/// on an interval would silently drop most presses instead of saving any work, since the actual
/// Physics2D.OverlapPoint only runs on the (rare) press frame either way.
///
/// Requires the Collider2D and the IClickPopupSource to live on the same GameObject
/// (TryGetComponent only looks at the hit GameObject itself, not its parents/children).
/// </summary>
public class PointerClickRouter : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private MonoBehaviour _uiServiceSource;
    [SerializeField] private LayerMask _clickableMask;

    private IUIService _uiService;
    private readonly List<Collider2D> _hits = new List<Collider2D>();
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    private void Awake()
    {
        _uiService = _uiServiceSource as IUIService;

        if (!_worldCamera)
        {
            Debug.LogError($"PointerClickRouter '{name}': missing _worldCamera.", this);
            enabled = false;
            return;
        }

        if (_uiService == null)
        {
            Debug.LogError($"PointerClickRouter '{name}': _uiServiceSource does not implement IUIService.", this);
            enabled = false;
            return;
        }

        if (_clickableMask.value == 0)
            Debug.LogError($"PointerClickRouter '{name}': _clickableMask is empty, nothing will ever be hit.", this);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Vector3 screenPosition = mouse.position.ReadValue();
        if (!_worldCamera || !_uiServiceSource || IsOverUi(screenPosition))
            return;

        Vector2 worldPosition = _worldCamera.ScreenToWorldPoint(screenPosition);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(_clickableMask);
        filter.useTriggers = true;
        _hits.Clear();
        Physics2D.OverlapPoint(worldPosition, filter, _hits);

        foreach (Collider2D hit in _hits)
        {
            if (!hit || !hit.TryGetComponent(out IClickPopupSource source)
                || source is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                continue;

            if (source.TryGetClickPopup(out PopupType popupType) && popupType != PopupType.None
                && _uiService.TryShow(popupType, source))
                return;
        }
    }

    private bool IsOverUi(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (!eventSystem)
            return false;

        // Query this press directly; IsPointerOverGameObject can reflect the previous input update.
        var pointer = new PointerEventData(eventSystem) { position = screenPosition };
        _uiHits.Clear();
        eventSystem.RaycastAll(pointer, _uiHits);
        foreach (RaycastResult hit in _uiHits)
        {
            if (hit.module is GraphicRaycaster)
                return true;
        }
        return false;
    }
}
