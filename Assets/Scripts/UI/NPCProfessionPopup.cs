using UnityEngine;
using UnityEngine.UI;

public sealed class NPCProfessionPopup : PopBase
{
    [SerializeField] private LocalizeText _titleText;
    [SerializeField] private LocalizeText _professionText;
    [SerializeField] private LocalizeText _levelText;
    [SerializeField] private LocalizeText _experienceText;
    [SerializeField] private LocalizeText _capText;
    [SerializeField] private Image _progressFill;
    [SerializeField] private Button _closeButton;

    private NPCProfessionPopupSource _source;
    private uint _lifetimeVersion;
    private NPCType _displayedProfession;
    private ProfessionProgress _displayedProgress;
    private bool _hasDisplayedProgress;
    private bool _hasLoggedConfigurationFailure;

    internal override bool TryBindSource(IClickPopupSource source)
    {
        NPCProfessionPopupSource resident = source as NPCProfessionPopupSource;
        if (!resident || !HasConfiguration()
            || !resident.TryGetProgress(out NPCType profession, out ProfessionProgress progress, out uint lifetimeVersion))
            return false;

        _source = resident;
        _lifetimeVersion = lifetimeVersion;
        _hasDisplayedProgress = false;
        ApplyProgress(profession, progress);
        return true;
    }

    private void OnEnable()
    {
        if (HasConfiguration())
            _closeButton.onClick.AddListener(RequestClose);
    }

    private void OnDisable()
    {
        if (_closeButton)
            _closeButton.onClick.RemoveListener(RequestClose);
        ClearSource();
    }

    protected override void OnBeforeClose() => ClearSource();

    private void Update()
    {
        // Clearing before the exit animation also prevents repeated close requests.
        if (!_hasDisplayedProgress)
            return;
        if (!_source || !_source.TryGetProgress(out NPCType profession, out ProfessionProgress progress, out uint lifetimeVersion)
            || lifetimeVersion != _lifetimeVersion)
        {
            RequestClose();
            return;
        }

        if (profession != _displayedProfession || progress.Level != _displayedProgress.Level
            || progress.LevelCap != _displayedProgress.LevelCap || progress.Experience != _displayedProgress.Experience
            || progress.RequiredExperience != _displayedProgress.RequiredExperience || progress.IsAtCap != _displayedProgress.IsAtCap)
            ApplyProgress(profession, progress);
    }

    private void ApplyProgress(NPCType profession, ProfessionProgress progress)
    {
        _titleText.SetKey(LocalizeKey.UI_NPCProfession_Title);
        _professionText.SetKey(GetProfessionKey(profession));
        _levelText.SetKey(LocalizeKey.UI_NPCProfession_Level);
        _levelText.SetFormatArguments(progress.Level, progress.LevelCap);
        _experienceText.SetKey(LocalizeKey.UI_NPCProfession_Experience);
        _experienceText.SetFormatArguments(progress.Experience, progress.RequiredExperience);
        _capText.SetKey(LocalizeKey.UI_NPCProfession_CapReached);
        _capText.gameObject.SetActive(progress.IsAtCap);
        _progressFill.fillAmount = progress.IsAtCap ? 1f
            : progress.RequiredExperience > 0f ? Mathf.Clamp01(progress.Experience / progress.RequiredExperience) : 0f;
        _displayedProfession = profession;
        _displayedProgress = progress;
        _hasDisplayedProgress = true;
    }

    private void ClearSource()
    {
        _source = null;
        _lifetimeVersion = 0;
        _hasDisplayedProgress = false;
    }

    private bool HasConfiguration()
    {
        if (_titleText && _professionText && _levelText && _experienceText && _capText && _progressFill && _closeButton)
            return true;
        if (!_hasLoggedConfigurationFailure)
            Debug.LogError($"NPCProfessionPopup '{name}': assign all LocalizeText fields, _progressFill and _closeButton.", this);
        _hasLoggedConfigurationFailure = true;
        return false;
    }

    private static LocalizeKey GetProfessionKey(NPCType profession)
    {
        switch (profession)
        {
            case NPCType.Farmer: return LocalizeKey.UI_NPCProfession_Farmer;
            case NPCType.Builder: return LocalizeKey.UI_NPCProfession_Builder;
            case NPCType.Guard: return LocalizeKey.UI_NPCProfession_Guard;
            case NPCType.Archer: return LocalizeKey.UI_NPCProfession_Archer;
            default: return LocalizeKey.UI_NPCProfession_Title;
        }
    }
}
