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
    private readonly MessageTyping _typing = new MessageTyping();

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
        _typing.Resume();
        PlayMotion(true);
    }

    protected override void BeginHide()
    {
        _typing.Pause();
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
        _typing.Begin(_typingText, _typingText.text, _characterInterval);
    }

    private void ResetTyping()
    {
        _typing.Reset();
        _isTypingPending = false;
    }

    private void UpdatePosition()
    {
        _rect.localScale = _restingScale * Mathf.Lerp(Mathf.Clamp01(_hiddenScale), 1f, _motionProgress);
        bool visible = MessageBubblePosition.Apply(_rect, _canvasRect, _canvas, _worldCamera,
            _trackingAnchor.position, _edgePadding, _corners,
            Vector2.down * (Mathf.Max(0f, _riseDistance) * (1f - _motionProgress)));
        _canvasGroup.alpha = visible ? _motionProgress : 0f;
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
