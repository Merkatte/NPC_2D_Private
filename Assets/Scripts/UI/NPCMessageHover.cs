using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class NPCMessageHover : HoverBase
{
    [SerializeField] private LocalizeText _messageText;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField, Min(0f)] private float _edgePadding = 8f;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float _enterDuration = 0.2f;
    [SerializeField, Min(0f)] private float _exitDuration = 0.15f;
    [SerializeField, Min(0f)] private float _riseDistance = 18f;
    [SerializeField, Range(0f, 1f)] private float _hiddenScale = 0.65f;

    [Header("Typing")]
    [SerializeField, Min(0f)] private float _characterInterval = 0.025f;

    private readonly Vector3[] _corners = new Vector3[4];
    private RectTransform _rect;
    private RectTransform _canvasRect;
    private Transform _trackingAnchor;
    private LocalizeKey? _displayedKey;
    private bool _isConfigured;
    private Vector3 _restingScale;
    private float _motionProgress;
    private Tween _motionTween;
    private Text _typingText;
    private bool _isTypingPending;
    private string _fullMessage;
    private Tween _typingTween;

    private void Awake()
    {
        _rect = transform as RectTransform;
        _canvasRect = _canvas ? _canvas.transform as RectTransform : null;
        _typingText = _messageText ? _messageText.GetComponent<Text>() : null;
        _isConfigured = _messageText && _worldCamera && _canvas && _canvasRect && _rect && _canvasGroup;
        if (!_isConfigured)
        {
            Debug.LogError($"NPCMessageHover '{name}': assign _messageText, _worldCamera, _canvas and _canvasGroup on a RectTransform.", this);
            return;
        }
        _restingScale = _rect.localScale;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            graphic.raycastTarget = false;
    }

    protected override void ApplyInfo(HoverInfo info)
    {
        if (!_isConfigured || !_messageText || !_worldCamera || !_canvas || !_canvasRect || !_rect || !_canvasGroup ||
            !info.MessageKey.HasValue || !info.TrackingAnchor || !info.TrackingAnchor.gameObject.activeInHierarchy)
        {
            HideImmediately();
            return;
        }
        bool hasAnchorChanged = _trackingAnchor != info.TrackingAnchor;
        if (_trackingAnchor && hasAnchorChanged)
        {
            StopMotion();
            _motionProgress = 0f;
        }
        _trackingAnchor = info.TrackingAnchor;
        if (_displayedKey != info.MessageKey || hasAnchorChanged)
        {
            ResetTyping();
            _displayedKey = info.MessageKey;
            _messageText.SetKey(info.MessageKey.Value);
            _isTypingPending = true;
        }
        UpdatePosition();
    }

    protected override void OnShown()
    {
        _typingTween?.Play();
        PlayMotion(true);
    }

    protected override void BeginHide()
    {
        _typingTween?.Pause();
        if (!_isConfigured || !isActiveAndEnabled || !_trackingAnchor)
        {
            HideImmediately();
            return;
        }
        PlayMotion(false);
    }

    private void PlayMotion(bool show)
    {
        StopMotion();
        float target = show ? 1f : 0f;
        float duration = Mathf.Max(0f, show ? _enterDuration : _exitDuration) * Mathf.Abs(target - _motionProgress);
        if (duration <= 0f)
        {
            _motionProgress = target;
            if (!show)
                CompleteHide();
            return;
        }
        _motionTween = DOTween.To(() => _motionProgress, value => _motionProgress = value, target, duration)
            .SetUpdate(true)
            .SetEase(show ? Ease.OutCubic : Ease.InCubic)
            .OnComplete(() =>
            {
                _motionTween = null;
                if (!show)
                    CompleteHide();
            });
    }

    private void StopMotion()
    {
        _motionTween?.Kill();
        _motionTween = null;
    }

    protected override void OnDisable()
    {
        StopMotion();
        ResetTyping();
        _motionProgress = 0f;
        _trackingAnchor = null;
        _displayedKey = null;
        if (_isConfigured && _rect)
            _rect.localScale = _restingScale;
        if (_canvasGroup)
            _canvasGroup.alpha = 0f;
        base.OnDisable();
    }

    private void LateUpdate()
    {
        if (!_isConfigured || !_trackingAnchor || !_trackingAnchor.gameObject.activeInHierarchy ||
            !_worldCamera || !_canvas || !_canvasRect || !_rect || !_canvasGroup)
        {
            HideImmediately();
            return;
        }
        UpdateTyping();
        UpdatePosition();
    }

    private void UpdateTyping()
    {
        if (!_isTypingPending || !IsVisible || !_typingText)
            return;
        // Wait until LateUpdate so LocalizeText's first Start has resolved its key.
        _isTypingPending = false;
        _messageText.Refresh();
        _fullMessage = _typingText.text;
        if (_characterInterval <= 0f || string.IsNullOrEmpty(_fullMessage))
            return;
        _typingText.text = string.Empty;
        _typingTween = _typingText.DOText(_fullMessage, _fullMessage.Length * _characterInterval,
                _typingText.supportRichText)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => _typingTween = null);
    }

    private void ResetTyping()
    {
        _typingTween?.Kill();
        _typingTween = null;
        if (_typingText && _fullMessage != null)
            _typingText.text = _fullMessage;
        _isTypingPending = false;
        _fullMessage = null;
    }

    private void UpdatePosition()
    {
        Vector3 screenPosition = _worldCamera.WorldToScreenPoint(_trackingAnchor.position);
        _canvasGroup.alpha = screenPosition.z > 0f ? _motionProgress : 0f;
        if (screenPosition.z <= 0f)
            return;
        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, screenPosition, uiCamera, out Vector3 worldPosition))
            return;
        // Compose motion with this frame's tracked anchor, then clamp the animated bounds.
        _rect.localScale = _restingScale * Mathf.Lerp(Mathf.Clamp01(_hiddenScale), 1f, _motionProgress);
        _rect.position = worldPosition + _canvasRect.TransformVector(Vector2.down *
            (Mathf.Max(0f, _riseDistance) * (1f - _motionProgress)));

        _rect.GetWorldCorners(_corners);
        Vector2 minimum = _canvasRect.InverseTransformPoint(_corners[0]);
        Vector2 maximum = minimum;
        for (int i = 1; i < _corners.Length; ++i)
        {
            Vector2 corner = _canvasRect.InverseTransformPoint(_corners[i]);
            minimum = Vector2.Min(minimum, corner);
            maximum = Vector2.Max(maximum, corner);
        }
        Rect bounds = _canvasRect.rect;
        Vector2 shift = new Vector2(
            GetClampShift(minimum.x, maximum.x, bounds.xMin + _edgePadding, bounds.xMax - _edgePadding),
            GetClampShift(minimum.y, maximum.y, bounds.yMin + _edgePadding, bounds.yMax - _edgePadding));
        _rect.position += _canvasRect.TransformVector(shift);
    }

    private static float GetClampShift(float minimum, float maximum, float lower, float upper)
    {
        // An oversized bubble is centered instead of oscillating between both edges.
        if (maximum - minimum > upper - lower)
            return (lower + upper - minimum - maximum) * 0.5f;
        if (minimum < lower)
            return lower - minimum;
        if (maximum > upper)
            return upper - maximum;
        return 0f;
    }

    private void OnValidate()
    {
        _edgePadding = Mathf.Max(0f, _edgePadding);
        _enterDuration = Mathf.Max(0f, _enterDuration);
        _exitDuration = Mathf.Max(0f, _exitDuration);
        _riseDistance = Mathf.Max(0f, _riseDistance);
        _hiddenScale = Mathf.Clamp01(_hiddenScale);
        _characterInterval = Mathf.Max(0f, _characterInterval);
    }
}
