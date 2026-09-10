using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Town hall recruitment UI. Concrete reference to TownHallRecruitment (no interface) — a 1:1
/// feature-specific pairing like MerchantPopup-MerchantTradeSite, not a polymorphic UI/domain
/// boundary. Polls TownHallRecruitment while open (Update only runs while the GameObject is
/// active, so a closed popup costs nothing) rather than subscribing to an event — this project has
/// no state-change event convention anywhere (not even GoldManager), and the cooldown display needs
/// a per-frame refresh regardless.
/// </summary>
public sealed class TownHallPopup : PopBase
{
    [SerializeField] private TownHallRecruitment _recruitment;

    [SerializeField] private GameObject _recruitingPanelRoot;
    [SerializeField] private GameObject _candidateReadyPanelRoot;

    [SerializeField] private Text _cooldownText;
    [SerializeField] private Text _candidateJobText;
    [SerializeField] private Text _settlementCostText;
    [SerializeField] private Button _recruitButton;
    [SerializeField] private Text _resultText;

    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    private RecruitPhase? _lastDisplayedPhase;
    private int _lastDisplayedSeconds = -1;

    private void Awake()
    {
        if (!_recruitment || !_recruitingPanelRoot || !_candidateReadyPanelRoot)
        {
            ReportConfigurationFailure(
                "missing a required reference (_recruitment/_recruitingPanelRoot/_candidateReadyPanelRoot)");
            return;
        }

        _isConfigured = true;
    }

    /// <summary>
    /// Not OnBeforeOpen(): PopBase.Open() calls OnBeforeOpen() before SetActive(true), so on the
    /// very first open Awake() has not run yet (MerchantPopup precedent).
    /// </summary>
    protected override void OnOpened()
    {
        if (!_isConfigured)
        {
            return;
        }

        _lastDisplayedPhase = null;
        _lastDisplayedSeconds = -1;

        if (_resultText)
        {
            _resultText.text = string.Empty;
        }

        Refresh();
    }

    private void Update()
    {
        if (!_isConfigured)
        {
            return;
        }

        Refresh();
    }

    public void HandleRecruitClick()
    {
        if (!_isConfigured)
        {
            return;
        }

        RecruitResult result = _recruitment.TryDispatchCandidate();

        switch (result)
        {
            case RecruitResult.Success:
                // Close immediately so the drop presentation isn't hidden behind the popup.
                Close();
                break;

            case RecruitResult.NotEnoughGold:
                ShowResult("정착지원금이 부족합니다.");
                break;

            case RecruitResult.SpawnUnavailable:
                ShowResult("지금은 이 직업을 모집할 수 없습니다.");
                break;

            case RecruitResult.NotReady:
                // Stale button click (double-click, or a candidate that stopped being ready between
                // frames) — nothing meaningful to report, the next Refresh() re-syncs the panel.
                break;
        }
    }

    private void Refresh()
    {
        RecruitPhase phase = _recruitment.Phase;

        if (phase != _lastDisplayedPhase)
        {
            _lastDisplayedPhase = phase;
            ApplyPhaseVisibility(phase);

            if (phase == RecruitPhase.CandidateReady)
            {
                RefreshCandidateDisplay();
            }
        }

        if (phase == RecruitPhase.Recruiting)
        {
            RefreshCooldownText();
        }
    }

    private void ApplyPhaseVisibility(RecruitPhase phase)
    {
        _recruitingPanelRoot.SetActive(phase == RecruitPhase.Recruiting);
        _candidateReadyPanelRoot.SetActive(phase == RecruitPhase.CandidateReady);
    }

    private void RefreshCandidateDisplay()
    {
        if (_candidateJobText)
        {
            _candidateJobText.text = $"직업: {GetDisplayName(_recruitment.CandidateNpcType)}";
        }

        if (_settlementCostText)
        {
            _settlementCostText.text = $"정착지원금: {_recruitment.SettlementCost} G";
        }
    }

    private void RefreshCooldownText()
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, _recruitment.CooldownRemaining));
        if (seconds == _lastDisplayedSeconds)
        {
            return;
        }

        _lastDisplayedSeconds = seconds;

        if (_cooldownText)
        {
            _cooldownText.text = $"남은 시간: {FormatSeconds(seconds)}";
        }
    }

    private static string FormatSeconds(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private static string GetDisplayName(NPCType npcType)
    {
        switch (npcType)
        {
            case NPCType.Farmer:
                return "농부";
            case NPCType.Guard:
                return "경비병";
            case NPCType.Cook:
                return "요리사";
            default:
                return npcType.ToString();
        }
    }

    private void ShowResult(string message)
    {
        if (_resultText)
        {
            _resultText.text = message;
        }
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
        {
            return;
        }

        Debug.LogError($"TownHallPopup '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
