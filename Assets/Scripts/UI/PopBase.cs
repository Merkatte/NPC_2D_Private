using DG.Tweening;
using UnityEngine;

public abstract class PopBase : MonoBehaviour
{
    [SerializeField] private PopupType _popupType;

    [Header("Motion")]
    [SerializeField] private float _offScreenDistance = 1200f;
    [SerializeField] private float _enterDuration = 0.35f;
    [SerializeField] private float _exitDuration = 0.25f;

    private RectTransform _rectTransform;
    private Vector2 _restingAnchoredPosition;
    private bool _hasCapturedRestingPosition;
    private bool _hasLoggedMissingRectTransform;
    private bool _hasLoggedMissingUiService;
    private bool _isClosing;
    private Tween _motionTween;
    private IUIService _uiService;

    public PopupType PopupType => _popupType;
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>
    /// Called once by UIManager for every popup it successfully registers, so RequestClose() can
    /// route through the same TryHide() path an external caller would use — instead of every
    /// concrete popup wiring its own reference to the same single UIManager in the Inspector.
    /// </summary>
    internal void Initialize(IUIService uiService)
    {
        _uiService = uiService;
    }

    /// <summary>
    /// Self-close entry point for a popup's own close button (or any other in-popup trigger, e.g. a
    /// completed action that should dismiss the popup). Goes through IUIService.TryHide so
    /// UIManager's popup stack and BackBg stay in sync — calling Close() directly here would close
    /// the GameObject but leave both stale until an unrelated popup event happens to refresh them.
    /// </summary>
    public void RequestClose()
    {
        if (_uiService != null && _uiService.TryHide(_popupType))
            return;

        if (_uiService == null && !_hasLoggedMissingUiService)
        {
            Debug.LogError($"PopBase '{name}': _uiService not initialized (UIManager did not register this popup); closing without stack/BackBg bookkeeping.", this);
            _hasLoggedMissingUiService = true;
        }

        Close();
    }

    internal void Open()
    {
        if (IsOpen)
        {
            transform.SetAsLastSibling();

            // Reached only when a pending Close() motion is interrupted before it deactivates the
            // object (see Close()) — reverse it instead of leaving the exit tween to finish under us.
            if (_isClosing)
                PlayEnterMotion();

            return;
        }

        OnBeforeOpen();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        PlayEnterMotion();
        OnOpened();
    }

    internal void Close()
    {
        if (!IsOpen)
            return;

        OnBeforeClose();
        PlayExitMotion(() =>
        {
            gameObject.SetActive(false);
            OnClosed();
        });
    }

    protected virtual void OnBeforeOpen()
    {
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnBeforeClose()
    {
    }

    protected virtual void OnClosed()
    {
    }

    private void PlayEnterMotion()
    {
        if (!TryGetMotionTarget(out RectTransform rect))
            return;

        _isClosing = false;
        _motionTween?.Kill();
        rect.anchoredPosition = _restingAnchoredPosition + Vector2.down * _offScreenDistance;
        _motionTween = rect.DOAnchorPos(_restingAnchoredPosition, _enterDuration).SetEase(Ease.OutBack);
    }

    private void PlayExitMotion(TweenCallback onDeactivate)
    {
        if (!TryGetMotionTarget(out RectTransform rect))
        {
            onDeactivate();
            return;
        }

        _isClosing = true;
        _motionTween?.Kill();

        Vector2 offScreenPosition = _restingAnchoredPosition + Vector2.down * _offScreenDistance;
        _motionTween = rect.DOAnchorPos(offScreenPosition, _exitDuration)
            .SetEase(Ease.InBack)
            .OnComplete(onDeactivate);
    }

    // Resting position is captured from whatever the prefab was authored with, the first time
    // motion actually runs — not Awake(), since popups start inactive and Awake() does not fire
    // until the very first SetActive(true) (see MerchantPopup/TownHallPopup OnOpened() remarks).
    private bool TryGetMotionTarget(out RectTransform rect)
    {
        if (!_hasCapturedRestingPosition)
        {
            _rectTransform = transform as RectTransform;

            if (!_rectTransform)
            {
                if (!_hasLoggedMissingRectTransform)
                {
                    Debug.LogError($"PopBase '{name}': transform is not a RectTransform; open/close motion disabled.", this);
                    _hasLoggedMissingRectTransform = true;
                }

                rect = null;
                return false;
            }

            _restingAnchoredPosition = _rectTransform.anchoredPosition;
            _hasCapturedRestingPosition = true;
        }

        rect = _rectTransform;
        return true;
    }
}
