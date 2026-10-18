using System;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class GoldNotificationHUD : MonoBehaviour
{
    [SerializeField] private ResourceManager _resourceManager;
    [SerializeField] private RectTransform _panel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Text _amountText;
    [SerializeField] private Text _changeText;

    [Header("Motion")]
    [SerializeField] private Vector2 _visibleAnchoredPosition = new Vector2(16f, -16f);
    [SerializeField, Min(0f)] private float _hiddenOffset = 96f;
    [SerializeField, Min(0f)] private float _slideDuration = 0.25f;
    [SerializeField, Min(0f)] private float _countDuration = 0.65f;
    [SerializeField, Min(0f)] private float _holdDuration = 1.5f;

    private ResourceManager _subscribedResources;
    private Tween _motionTween;
    private Tween _countTween;
    private Tween _holdTween;
    private double _visualGold;
    private int _targetGold;
    private int _renderedGold = -1;
    private float _motionProgress;
    private bool _isRevealing;
    private bool _hasPendingCount;
    private bool _isContextVisible;
    private bool _hasStarted;
    private bool _hasReportedConfigurationError;

    public void Configure(ResourceManager resourceManager)
    {
        Unsubscribe();
        ResetPresentation();
        _resourceManager = resourceManager;
        if (Application.isPlaying && isActiveAndEnabled)
            Initialize();
    }

    public void SetContextVisible(bool isVisible)
    {
        if (_isContextVisible == isVisible)
            return;
        _isContextVisible = isVisible;
        if (!_subscribedResources || !isActiveAndEnabled)
            return;
        StopTween(ref _holdTween);
        if (isVisible)
            Reveal();
        else if (!_hasPendingCount && _motionProgress >= 1f)
            BeginHold();
    }

    private void Awake() => ResetPresentation();

    private void Start()
    {
        _hasStarted = true;
        Initialize();
    }

    private void OnEnable()
    {
        if (_hasStarted)
            Initialize();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ResetPresentation();
        _isContextVisible = false;
    }

    private void Initialize()
    {
        if (_subscribedResources)
            return;
        if (!_resourceManager || !_panel || _panel == transform || !_panel.IsChildOf(transform) ||
            !_canvasGroup || _canvasGroup.transform != _panel || !_amountText || !_changeText)
        {
            if (!_hasReportedConfigurationError)
            {
                Debug.LogError($"GoldNotificationHUD '{name}': assign _resourceManager, separate child _panel, its _canvasGroup, _amountText and _changeText.", this);
                _hasReportedConfigurationError = true;
            }
            return;
        }
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        foreach (Graphic graphic in _panel.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        _subscribedResources = _resourceManager;
        _subscribedResources.ResourcesChanged += HandleResourcesChanged;
        // Binding samples the current balance without presenting it as newly earned gold.
        _targetGold = _subscribedResources.GetQuantity(ResourceManager.GoldItemId);
        _visualGold = _targetGold;
        RenderAmount();
        if (_isContextVisible)
            Reveal();
    }

    private void Unsubscribe()
    {
        if (_subscribedResources)
            _subscribedResources.ResourcesChanged -= HandleResourcesChanged;
        _subscribedResources = null;
    }

    private void HandleResourcesChanged()
    {
        if (!_subscribedResources)
            return;
        int gold = _subscribedResources.GetQuantity(ResourceManager.GoldItemId);
        if (gold == _targetGold)
            return;
        long change = (long)gold - _targetGold;
        _targetGold = gold;
        if (_changeText)
            _changeText.text = change.ToString("+#,0;-#,0;0", CultureInfo.InvariantCulture);
        StopTween(ref _countTween);
        StopTween(ref _holdTween);
        _hasPendingCount = true;
        Reveal();
    }

    private void Reveal()
    {
        StopTween(ref _holdTween);
        // An exit tween can still be at 1 before its first update; cancel it before the position shortcut.
        if (!_isRevealing && _motionTween != null)
        {
            PlayMotion(true);
            return;
        }
        if (_motionProgress >= 1f)
        {
            if (_hasPendingCount)
                BeginCount();
            return;
        }
        // Further resource events during entrance update the target without restarting motion.
        if (_isRevealing && _motionTween != null)
            return;
        PlayMotion(true);
    }

    private void PlayMotion(bool reveal)
    {
        StopTween(ref _motionTween);
        _isRevealing = reveal;
        float target = reveal ? 1f : 0f;
        float duration = NonNegativeFinite(_slideDuration) * Mathf.Abs(target - _motionProgress);
        if (duration <= 0f)
        {
            _motionProgress = target;
            ApplyMotion();
            CompleteMotion();
            return;
        }
        _motionTween = DOTween.To(() => _motionProgress, value =>
            {
                _motionProgress = value;
                ApplyMotion();
            }, target, duration)
            .SetEase(reveal ? Ease.OutCubic : Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _motionTween = null;
                CompleteMotion();
            });
    }

    private void CompleteMotion()
    {
        if (_isRevealing)
        {
            if (_hasPendingCount)
                BeginCount();
            else
                BeginHold();
        }
        else if (_changeText)
            _changeText.text = string.Empty;
    }

    private void BeginCount()
    {
        StopTween(ref _countTween);
        double startGold = _visualGold;
        int endGold = _targetGold;
        float duration = NonNegativeFinite(_countDuration);
        if (duration <= 0f || startGold == endGold)
        {
            CompleteCount(endGold);
            return;
        }
        float progress = 0f;
        _countTween = DOTween.To(() => progress, value =>
            {
                progress = value;
                // Interpolate in double precision: float balances lose units near int.MaxValue.
                _visualGold = startGold + (endGold - startGold) * value;
                RenderAmount();
            }, 1f, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _countTween = null;
                CompleteCount(endGold);
            });
    }

    private void CompleteCount(int gold)
    {
        _visualGold = gold;
        RenderAmount();
        _hasPendingCount = false;
        BeginHold();
    }

    private void BeginHold()
    {
        StopTween(ref _holdTween);
        if (_isContextVisible)
            return;
        float duration = NonNegativeFinite(_holdDuration);
        if (duration <= 0f)
        {
            PlayMotion(false);
            return;
        }
        _holdTween = DOVirtual.DelayedCall(duration, () =>
        {
            _holdTween = null;
            PlayMotion(false);
        }, true);
    }

    private void RenderAmount()
    {
        int gold = (int)Math.Max(0d, Math.Min(int.MaxValue, Math.Round(_visualGold, MidpointRounding.AwayFromZero)));
        if (gold == _renderedGold || !_amountText)
            return;
        _renderedGold = gold;
        _amountText.text = gold.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void ApplyMotion()
    {
        if (_panel)
            _panel.anchoredPosition = _visibleAnchoredPosition + Vector2.up *
                (NonNegativeFinite(_hiddenOffset) * (1f - _motionProgress));
        if (_canvasGroup)
            _canvasGroup.alpha = _motionProgress > 0f ? 1f : 0f;
    }

    private void ResetPresentation()
    {
        StopTween(ref _motionTween);
        StopTween(ref _countTween);
        StopTween(ref _holdTween);
        _motionProgress = 0f;
        _isRevealing = false;
        _hasPendingCount = false;
        _visualGold = 0d;
        _targetGold = 0;
        _renderedGold = -1;
        if (_amountText)
            _amountText.text = string.Empty;
        if (_changeText)
            _changeText.text = string.Empty;
        ApplyMotion();
    }

    private static void StopTween(ref Tween tween)
    {
        tween?.Kill();
        tween = null;
    }

    private static float NonNegativeFinite(float value)
        => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

    private void OnValidate()
    {
        _visibleAnchoredPosition = new Vector2(
            float.IsNaN(_visibleAnchoredPosition.x) || float.IsInfinity(_visibleAnchoredPosition.x) ? 16f : _visibleAnchoredPosition.x,
            float.IsNaN(_visibleAnchoredPosition.y) || float.IsInfinity(_visibleAnchoredPosition.y) ? -16f : _visibleAnchoredPosition.y);
        _hiddenOffset = NonNegativeFinite(_hiddenOffset);
        _slideDuration = NonNegativeFinite(_slideDuration);
        _countDuration = NonNegativeFinite(_countDuration);
        _holdDuration = NonNegativeFinite(_holdDuration);
    }
}
