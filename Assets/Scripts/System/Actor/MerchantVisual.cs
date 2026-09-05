using UnityEngine;

/// <summary>
/// Presentation and click surface for the merchant caravan. Owns the caravan Animator and answers
/// "is this clickable right now?" from the Animator's live state, so the trade popup can only
/// open while the birds actually look landed. Knows nothing about the trade domain (gold, items,
/// pricing) — it only names a PopupType for the pointer input adapter.
/// </summary>
public sealed class MerchantVisual : MonoBehaviour, IClickPopupSource
{
    private const int BaseLayerIndex = 0;

    private static readonly int IsLandedHash = Animator.StringToHash("IsLanded");
    private static readonly int FlyingStateHash = Animator.StringToHash("Base Layer.Flying");
    private static readonly int LandedStateHash = Animator.StringToHash("Base Layer.Landed");

    [SerializeField] private Animator _animator;
    [SerializeField] private PopupType _popupType = PopupType.Merchant;
    [SerializeField] private MonoBehaviour _uiServiceSource;

    private IUIService _uiService;
    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        _uiService = _uiServiceSource as IUIService;

        if (!_animator)
        {
            ReportConfigurationFailure("missing Animator reference");
            return;
        }

        if (!_animator.HasState(BaseLayerIndex, FlyingStateHash) ||
            !_animator.HasState(BaseLayerIndex, LandedStateHash))
        {
            ReportConfigurationFailure("Animator Controller requires Flying and Landed states");
        }

        if (_popupType == PopupType.None)
        {
            ReportConfigurationFailure("_popupType is None; the caravan can never open a popup");
        }
    }

    /// <summary>
    /// Live query, never a cached flag. GetCurrentAnimatorStateInfo still reports the *source*
    /// state while a transition is in flight, so IsInTransition is what closes the leaving edge —
    /// without it, a click landed during take-off would still open the trade popup.
    /// </summary>
    public bool TryGetClickPopup(out PopupType popupType)
    {
        popupType = PopupType.None;

        if (!_animator || _popupType == PopupType.None)
            return false;

        if (_animator.IsInTransition(BaseLayerIndex))
            return false;

        if (_animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).fullPathHash != LandedStateHash)
            return false;

        popupType = _popupType;
        return true;
    }

    public void SetLanded(bool isLanded)
    {
        if (!_animator)
            return;

        _animator.SetBool(IsLandedHash, isLanded);

        // The trade popup has no other way to learn the caravan left — close it here rather than
        // in MerchantCaravan, so the domain actor's timer/state machine stays ignorant of UI.
        if (!isLanded && _uiService != null)
        {
            _uiService.TryHide(_popupType);
        }
    }

    /// <summary>
    /// Snaps straight to Flying with no animation. MerchantArrivalScheduler reuses the same
    /// caravan GameObject visit after visit, so the second visit must start from the same pose
    /// as the first rather than inheriting whatever the Animator rebind happened to leave.
    /// </summary>
    public void ResetToFlying()
    {
        if (!_animator)
            return;

        _animator.SetBool(IsLandedHash, false);
        _animator.Play(FlyingStateHash, BaseLayerIndex, 0f);
        _animator.Update(0f);
    }

    /// <summary>
    /// Animation Event target on the landing clip's contact frame. Intentionally empty for now —
    /// no audio/particle system exists in this project yet. Wire a sound/dust effect here when one does.
    /// </summary>
    public void OnLandingImpact()
    {
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantVisual '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
