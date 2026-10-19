using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only house sample with an independently revealed upgrade drawer.</summary>
public sealed class WarmPixelHousePopup : MonoBehaviour
{
    private const float DefaultClosedWidth = 1440f;
    private const float DefaultOpenWidth = 940f;
    private const float DefaultTransitionDuration = 0.3f;
    private const float DrawerSlideFraction = 0.08f;

    [Serializable]
    private sealed class LayoutElement
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Vector2 _closedPosition;
        [SerializeField] private Vector2 _openPosition;
        [SerializeField] private Vector2 _closedSize;
        [SerializeField] private Vector2 _openSize;

        public bool IsConfigured => _rect && IsFinite(_closedPosition) && IsFinite(_openPosition)
            && IsFinite(_closedSize) && IsFinite(_openSize);

        public void Apply(float progress)
        {
            _rect.anchoredPosition = Vector2.LerpUnclamped(_closedPosition, _openPosition, progress);
            _rect.sizeDelta = Vector2.LerpUnclamped(_closedSize, _openSize, progress);
        }
    }

    [SerializeField] private RectTransform _mainPanel;
    [SerializeField] private RectTransform _drawerViewport;
    [SerializeField] private RectTransform _drawerPanel;
    [SerializeField] private CanvasGroup _drawerGroup;
    [SerializeField] private Button _mainUpgradeButton;
    [SerializeField] private Button _drawerCollapseButton;
    [SerializeField] private Button _mainCloseButton;
    [SerializeField, Min(0f)] private float _transitionDuration = DefaultTransitionDuration;
    [SerializeField, Min(1f)] private float _closedMainWidth = DefaultClosedWidth;
    [SerializeField, Min(1f)] private float _openMainWidth = DefaultOpenWidth;
    [SerializeField] private LayoutElement[] _layoutElements = Array.Empty<LayoutElement>();

    private Tween _transition;
    private float _progress;
    private bool _isDrawerOpen;
    private bool _isConfigured;

    private void Awake()
    {
        SanitizeSettings();
        _isConfigured = _mainPanel && _drawerViewport && _drawerPanel && _drawerGroup
            && _mainUpgradeButton && _drawerCollapseButton && _mainCloseButton && _layoutElements != null;
        if (_isConfigured)
        {
            foreach (LayoutElement element in _layoutElements)
                _isConfigured &= element != null && element.IsConfigured;
        }
        if (_isConfigured) return;
        Debug.LogError($"{nameof(WarmPixelHousePopup)} '{name}' requires main/drawer rectangles, drawer CanvasGroup, three buttons and valid layout elements.", this);
        if (_drawerGroup)
        {
            _drawerGroup.alpha = 0f;
            _drawerGroup.interactable = false;
            _drawerGroup.blocksRaycasts = false;
        }
        enabled = false;
    }

    private void OnEnable()
    {
        if (!_isConfigured) return;
        ResetDrawer();
        _mainUpgradeButton.onClick.AddListener(ToggleDrawer);
        _drawerCollapseButton.onClick.AddListener(CloseDrawer);
        _mainCloseButton.onClick.AddListener(ClosePopup);
    }

    private void OnDisable()
    {
        if (_mainUpgradeButton) _mainUpgradeButton.onClick.RemoveListener(ToggleDrawer);
        if (_drawerCollapseButton) _drawerCollapseButton.onClick.RemoveListener(CloseDrawer);
        if (_mainCloseButton) _mainCloseButton.onClick.RemoveListener(ClosePopup);
        KillTransition();
        if (_isConfigured) ResetDrawer();
    }

    private void OnValidate() => SanitizeSettings();

    private void ToggleDrawer() => SetDrawerOpen(!_isDrawerOpen);

    private void CloseDrawer() => SetDrawerOpen(false);

    private void ClosePopup()
    {
        ResetDrawer();
        gameObject.SetActive(false);
    }

    private void SetDrawerOpen(bool isOpen)
    {
        if (!_isConfigured || !isActiveAndEnabled) return;
        SanitizeSettings();
        KillTransition();
        _isDrawerOpen = isOpen;
        float target = isOpen ? 1f : 0f;
        float duration = _transitionDuration * Mathf.Abs(target - _progress);
        // A reversal starts at the current progress; no transform or text scale is animated.
        if (duration <= 0f)
        {
            ApplyProgress(target);
            return;
        }
        SetDrawerInteraction(false);
        _transition = DOTween.To(() => _progress, ApplyProgress, target, duration)
            .SetEase(Ease.InOutCubic).SetUpdate(true).OnComplete(CompleteTransition);
    }

    private void CompleteTransition()
    {
        _transition = null;
        ApplyProgress(_isDrawerOpen ? 1f : 0f);
    }

    private void ResetDrawer()
    {
        KillTransition();
        _isDrawerOpen = false;
        ApplyProgress(0f);
    }

    private void KillTransition()
    {
        if (_transition == null) return;
        _transition.Kill(false);
        _transition = null;
    }

    private void ApplyProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
        float drawerWidth = _closedMainWidth - _openMainWidth;
        _mainPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            Mathf.LerpUnclamped(_closedMainWidth, _openMainWidth, _progress));
        // The viewport's right edge stays fixed. Only its uncovered area can render or receive input.
        _drawerViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, drawerWidth * _progress);
        _drawerPanel.anchoredPosition = new Vector2(drawerWidth * DrawerSlideFraction * (1f - _progress), 0f);
        _drawerGroup.alpha = _progress > 0f ? 1f : 0f;
        SetDrawerInteraction(_isDrawerOpen && _progress >= 1f);
        foreach (LayoutElement element in _layoutElements) element.Apply(_progress);
    }

    private void SetDrawerInteraction(bool isInteractive)
    {
        _drawerGroup.interactable = isInteractive;
        _drawerGroup.blocksRaycasts = isInteractive;
    }

    private void SanitizeSettings()
    {
        _transitionDuration = IsFinite(_transitionDuration) ? Mathf.Max(0f, _transitionDuration) : DefaultTransitionDuration;
        _closedMainWidth = IsFinite(_closedMainWidth) ? Mathf.Max(1f, _closedMainWidth) : DefaultClosedWidth;
        _openMainWidth = IsFinite(_openMainWidth) ? Mathf.Clamp(_openMainWidth, 1f, _closedMainWidth) : Mathf.Min(DefaultOpenWidth, _closedMainWidth);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
}
