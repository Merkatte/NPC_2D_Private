using UnityEngine;

/// <summary>
/// Click surface for the merchant caravan. Answers "is this clickable right now?" from an
/// explicit flag that MerchantCaravan sets at the exact moment its landing/departure presentation
/// completes — it owns no Animator and knows nothing about the visit's presentation steps beyond
/// that single bool. Knows nothing about the trade domain (gold, items, pricing) either — it only
/// names a PopupType for the pointer input adapter.
/// </summary>
public sealed class MerchantVisual : MonoBehaviour, IClickPopupSource
{
    [SerializeField] private PopupType _popupType = PopupType.Merchant;
    [SerializeField] private MonoBehaviour _uiServiceSource;

    private IUIService _uiService;
    private bool _isTradeAvailable;
    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        _uiService = _uiServiceSource as IUIService;

        if (_popupType == PopupType.None)
        {
            ReportConfigurationFailure("_popupType is None; the caravan can never open a popup");
        }
    }

    public bool TryGetClickPopup(out PopupType popupType)
    {
        popupType = PopupType.None;

        if (!_isTradeAvailable || _popupType == PopupType.None)
            return false;

        popupType = _popupType;
        return true;
    }

    /// <summary>
    /// The single point where MerchantCaravan communicates presentation state to this component.
    /// Called true only once bird and merchant landing finishes, and false the instant departure
    /// begins — never inferred from Animator state.
    /// </summary>
    public void SetTradeAvailable(bool isAvailable)
    {
        _isTradeAvailable = isAvailable;

        // The trade popup has no other way to learn the caravan left — close it here rather than
        // in MerchantCaravan, so the domain actor's presentation coroutine stays ignorant of UI.
        if (!isAvailable && _uiServiceSource && _uiService != null)
        {
            _uiService.TryHide(_popupType);
        }
    }

    private void OnDisable()
    {
        SetTradeAvailable(false);
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantVisual '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
