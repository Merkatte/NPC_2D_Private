using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MilitaryTrainingPopup : PopBase
{
    [SerializeField] private LocalizeText _statusText;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _assignButton;

    private MilitaryTrainingCandidate _selectedCandidate;
    private float _nextRefresh;
    private bool _hasSession;
    private bool _hasLoggedConfigurationFailure;
    private MilitaryTrainingFacility _source;
    private readonly List<MilitaryTrainingCandidate> _candidates = new List<MilitaryTrainingCandidate>();

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!(source is MilitaryTrainingFacility facility) || !facility || !facility.TryGetClickPopup(out _)
            || !HasConfiguration())
            return false;
        bool newSource = !_hasSession || _source != facility;
        _source = facility;
        _hasSession = true;
        Refresh(selectFirst: newSource);
        return true;
    }

    private void ClearSource()
    {
        _hasSession = false;
        _source = null;
        _selectedCandidate = default;
        _candidates.Clear();
    }

    private void Refresh(bool selectFirst = false)
    {
        if (!_hasSession) return;
        if (!_source || !_source.TryGetClickPopup(out _))
        {
            RequestClose();
            return;
        }
        _nextRefresh = Time.unscaledTime + 0.25f;
        _candidates.Clear();
        if (_source.TryGetSession(out MilitaryTrainingCandidate trainee, out bool arrived, out float remainingSeconds))
        {
            _selectedCandidate = default;
            ShowControls(true, -1);
            LocalizeKey key = arrived ? LocalizeKey.TrainingActive : LocalizeKey.TrainingWalking;
            _statusText.SetFormattedKey(key, trainee.DisplayName, Mathf.CeilToInt(remainingSeconds));
            return;
        }
        _source.CollectCandidates(_candidates);
        int selectedIndex = selectFirst && _candidates.Count > 0 ? 0 : FindSelectedIndex();
        _selectedCandidate = selectedIndex >= 0 ? _candidates[selectedIndex] : default;
        ShowControls(false, selectedIndex);
        if (selectedIndex < 0)
        {
            _statusText.SetFormattedKey(_candidates.Count == 0
                ? LocalizeKey.TrainingEmpty : LocalizeKey.TrainingChooseCandidate);
            return;
        }
        MilitaryTrainingCandidate candidate = _selectedCandidate;
        LocalizeKey profession = candidate.Profession == NPCType.Archer
            ? LocalizeKey.UI_NPCProfession_Archer : LocalizeKey.UI_NPCProfession_Guard;
        LocalizeKey candidateKey = candidate.CanAssign ? LocalizeKey.TrainingCandidate : LocalizeKey.TrainingLevelLimit;
        _statusText.SetFormattedKey(candidateKey, candidate.DisplayName, profession, candidate.Level, candidate.TrainingLevelCap);
    }

    private void Assign()
    {
        if (_source && _selectedCandidate.CanAssign)
            _source.TryAssign(_selectedCandidate);
        Refresh();
    }

    private void CancelTraining()
    {
        if (_source) _source.CancelTraining();
        Refresh();
    }

    private void OnEnable()
    {
        if (!HasConfiguration()) return;
        _closeButton.onClick.AddListener(RequestClose);
        _cancelButton.onClick.AddListener(CancelTraining);
        _assignButton.onClick.AddListener(Assign);
        _previousButton.onClick.AddListener(Previous);
        _nextButton.onClick.AddListener(Next);
    }

    private void OnDisable()
    {
        if (_closeButton) _closeButton.onClick.RemoveListener(RequestClose);
        if (_cancelButton) _cancelButton.onClick.RemoveListener(CancelTraining);
        if (_assignButton) _assignButton.onClick.RemoveListener(Assign);
        if (_previousButton) _previousButton.onClick.RemoveListener(Previous);
        if (_nextButton) _nextButton.onClick.RemoveListener(Next);
        ClearSource();
    }

    private bool HasConfiguration()
    {
        if (_statusText && _cancelButton && _closeButton && _previousButton && _nextButton && _assignButton)
            return true;
        if (!_hasLoggedConfigurationFailure)
            Debug.LogError($"MilitaryTrainingPopup '{name}' requires _statusText and all five buttons.", this);
        _hasLoggedConfigurationFailure = true;
        return false;
    }

    protected override void OnBeforeClose() => ClearSource();

    private void Update()
    {
        if (_hasSession && Time.unscaledTime >= _nextRefresh)
            Refresh();
    }

    private int FindSelectedIndex()
    {
        if (!_selectedCandidate.Actor) return -1;
        for (int i = 0; i < _candidates.Count; ++i)
        {
            MilitaryTrainingCandidate candidate = _candidates[i];
            if (candidate.Actor == _selectedCandidate.Actor && candidate.LifetimeVersion == _selectedCandidate.LifetimeVersion)
                return i;
        }
        return -1;
    }

    private void Previous() => SelectCandidate(FindSelectedIndex() - 1);
    private void Next() => SelectCandidate(FindSelectedIndex() + 1);

    private void SelectCandidate(int index)
    {
        if (index < 0 || index >= _candidates.Count) return;
        _selectedCandidate = _candidates[index];
        Refresh();
    }

    private void ShowControls(bool busy, int selectedIndex)
    {
        int count = _candidates.Count;
        _cancelButton.gameObject.SetActive(busy);
        _assignButton.gameObject.SetActive(!busy && count > 0);
        _assignButton.interactable = selectedIndex >= 0 && _selectedCandidate.CanAssign;
        _previousButton.gameObject.SetActive(!busy && count > 1);
        _nextButton.gameObject.SetActive(!busy && count > 0 && (count > 1 || selectedIndex < 0));
        _previousButton.interactable = selectedIndex > 0;
        _nextButton.interactable = selectedIndex + 1 < count;
    }
}
