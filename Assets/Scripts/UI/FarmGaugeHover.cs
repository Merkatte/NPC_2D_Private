using UnityEngine;
using UnityEngine.UI;

public class FarmGaugeHover : HoverBase
{
    [SerializeField] private Image _progressFill;
    [SerializeField] private Camera _worldCamera;

    private RectTransform _rect;
    private RectTransform _parentRect;
    private Canvas _rootCanvas;
    private CanvasGroup _canvasGroup;
    private float _planeZ;
    private float _visibleAlpha;
    private bool _isConfigured;

    private void Awake()
    {
        _rect = transform as RectTransform;
        _parentRect = transform.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>(true);
        _rootCanvas = canvas ? canvas.rootCanvas : null;
        _isConfigured = _progressFill && _worldCamera && _rect && _parentRect && _rootCanvas &&
            (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay || _rootCanvas.worldCamera);

        if (!_isConfigured)
        {
            Debug.LogError($"FarmGaugeHover '{name}': assign _progressFill and _worldCamera under a Canvas with a RectTransform parent; non-overlay Canvas requires worldCamera.", this);
            return;
        }

        _planeZ = _rect.localPosition.z;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (!_canvasGroup)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _visibleAlpha = _canvasGroup.alpha;
    }

    protected override void ApplyInfo(HoverInfo info)
    {
        if (!_isConfigured)
        {
            HideImmediately();
            return;
        }

        _progressFill.gameObject.SetActive(info.HasProgress);
        if (info.HasProgress)
            _progressFill.fillAmount = info.NormalizedProgress;

        Vector3 screenPosition = _worldCamera.WorldToScreenPoint(info.AnchorPosition);
        // Hide only the presentation so HoverBase keeps refreshing when the anchor returns.
        _canvasGroup.alpha = 0f;
        if (screenPosition.z <= 0f)
            return;

        Camera uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        if (_rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && !uiCamera)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPosition, uiCamera, out Vector2 localPosition))
            return;

        _rect.localPosition = new Vector3(localPosition.x, localPosition.y, _planeZ);
        _canvasGroup.alpha = _visibleAlpha;
    }

    protected override void OnDisable()
    {
        if (_canvasGroup)
            _canvasGroup.alpha = _visibleAlpha;
        base.OnDisable();
    }
}
