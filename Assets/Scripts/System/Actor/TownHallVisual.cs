using UnityEngine;

/// <summary>
/// Click surface for the town hall plus the two status icons above it. Unlike MerchantVisual, the
/// town hall is clickable at all times (requirement: the popup can always be opened), so there is
/// no "is this clickable right now" gate and no IUIService reference — there is never a case where
/// this component needs to force-close an open popup because the town hall itself became
/// unclickable.
/// </summary>
public sealed class TownHallVisual : MonoBehaviour, IClickPopupSource
{
    [SerializeField] private PopupType _popupType = PopupType.TownHall;
    [SerializeField] private GameObject _recruitingIcon;
    [SerializeField] private GameObject _candidateReadyIcon;

    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        if (_popupType == PopupType.None)
        {
            ReportConfigurationFailure("_popupType is None; the town hall can never open a popup");
        }
    }

    public bool TryGetClickPopup(out PopupType popupType)
    {
        popupType = PopupType.None;

        if (_popupType == PopupType.None)
        {
            return false;
        }

        popupType = _popupType;
        return true;
    }

    /// <summary>
    /// The single point where TownHallRecruitment pushes its phase to the world icons — pushed
    /// only on transition, never polled, matching MerchantVisual.SetTradeAvailable's pattern.
    /// </summary>
    public void SetPhase(RecruitPhase phase)
    {
        if (_recruitingIcon)
        {
            _recruitingIcon.SetActive(phase == RecruitPhase.Recruiting);
        }

        if (_candidateReadyIcon)
        {
            _candidateReadyIcon.SetActive(phase == RecruitPhase.CandidateReady);
        }
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
        {
            return;
        }

        Debug.LogError($"TownHallVisual '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
