using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pointer/raycast input adapter. Detects the <see cref="IHoverInfoSource"/> under the mouse
/// and forwards show/hide intent to <see cref="IUIService"/>. Knows nothing about any concrete
/// domain type (e.g. FarmWorkSite) or concrete UI type (HoverBase, UIManager) — only the
/// IHoverInfoSource/IUIService contracts.
///
/// Requires the Collider2D and the IHoverInfoSource to live on the same GameObject
/// (TryGetComponent only looks at the hit GameObject itself, not its parents/children).
/// </summary>
public class PointerHoverRouter : MonoBehaviour
{
    private const float MinimumPollInterval = 0.02f;

    [SerializeField] private Camera _worldCamera;
    [SerializeField] private MonoBehaviour _uiServiceSource;
    [SerializeField] private LayerMask _hoverableMask;
    [SerializeField, Min(MinimumPollInterval)] private float _pollInterval = 0.05f;

    private IUIService _uiService;
    private float _nextPollTime;

    // Candidate: whatever is currently under the mouse. Only re-resolved when the hit
    // collider changes, so TryGetComponent runs on enter/exit, not every poll.
    private Collider2D _candidateCollider;
    private IHoverInfoSource _candidateSource;
    private HoverType _candidateHoverType;

    // Active: the source currently showing on screen (TryShow succeeded). Kept separate from
    // the candidate so a failed TryShow (e.g. blocked by UI policy) retries on the next poll
    // instead of being stuck until the mouse leaves and re-enters the same collider.
    private IHoverInfoSource _activeSource;
    private HoverType _activeHoverType;

    private void Awake()
    {
        _uiService = _uiServiceSource as IUIService;

        if (!_worldCamera)
        {
            Debug.LogError($"PointerHoverRouter '{name}': missing _worldCamera.", this);
            enabled = false;
            return;
        }

        if (_uiService == null)
        {
            Debug.LogError($"PointerHoverRouter '{name}': _uiServiceSource does not implement IUIService.", this);
            enabled = false;
            return;
        }

        if (_hoverableMask.value == 0)
            Debug.LogError($"PointerHoverRouter '{name}': _hoverableMask is empty, nothing will ever be hit.", this);
    }

    private void OnDisable()
    {
        ClearActiveHover();
        _candidateCollider = null;
        _candidateSource = null;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextPollTime)
            return;

        _nextPollTime = Time.unscaledTime + Mathf.Max(MinimumPollInterval, _pollInterval);

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            ClearActiveHover();
            return;
        }

        Vector3 screenPosition = mouse.position.ReadValue();
        Vector2 worldPosition = _worldCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, _hoverableMask);

        if (hit != _candidateCollider)
            UpdateCandidate(hit);

        if (_candidateSource == null)
        {
            ClearActiveHover();
            return;
        }

        if (ReferenceEquals(_candidateSource, _activeSource))
            return;

        ClearActiveHover();

        if (_uiService.TryShow(_candidateHoverType, _candidateSource))
        {
            _activeSource = _candidateSource;
            _activeHoverType = _candidateHoverType;
        }
    }

    private void UpdateCandidate(Collider2D hit)
    {
        _candidateCollider = hit;
        _candidateSource = null;

        if (!hit || !hit.TryGetComponent(out IHoverInfoSource source))
            return;

        if (source.HoverType == HoverType.None)
            return;

        _candidateSource = source;
        _candidateHoverType = source.HoverType;
    }

    private void ClearActiveHover()
    {
        if (_activeSource == null)
            return;

        _uiService.TryHide(_activeHoverType, _activeSource);
        _activeSource = null;
    }

    private void OnValidate()
    {
        _pollInterval = Mathf.Max(MinimumPollInterval, _pollInterval);
    }
}
