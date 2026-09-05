using UnityEngine;
using UnityEngine.InputSystem;

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
        Vector2 worldPosition = _worldCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, _clickableMask);

        if (!hit || !hit.TryGetComponent(out IClickPopupSource source))
            return;

        if (!source.TryGetClickPopup(out PopupType popupType) || popupType == PopupType.None)
            return;

        _uiService.TryShow(popupType);
    }
}
