using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class TownHallPopup : PopBase
{
    [SerializeField] private TownHallRecruitment _recruitment;
    [SerializeField] private GameObject _townStatusPanel;
    [SerializeField] private GameObject _recruitmentPanel;
    [SerializeField] private Button _townStatusTab;
    [SerializeField] private Button _recruitmentTab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _townStatusUnderline;
    [SerializeField] private GameObject _recruitmentUnderline;
    [SerializeField] private TownHallRoleView[] _roles;
    [SerializeField] private TownHallRecruitCard _detailCard;
    [SerializeField] private Text _resultText;
    private TownHallRoleView _selectedRole;
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;
    private bool _isSessionOpen;
    private Component _sourceOwner;
    private IClickPopupSource _source;

    private bool EnsureConfigured()
    {
        if (_isConfigured) return true;
        bool valid = _recruitment && _townStatusPanel && _recruitmentPanel && _townStatusTab &&
            _recruitmentTab && _closeButton && _detailCard && _detailCard.IsConfigured && _resultText &&
            _roles != null && _roles.Length > 0;
        if (valid)
        {
            var roles = new HashSet<NPCType>();
            foreach (TownHallRoleView role in _roles) valid &= role && role.IsConfigured && roles.Add(role.NpcType);
        }
        if (!valid && !_hasLoggedConfigurationFailure)
        {
            Debug.LogError($"TownHallPopup '{name}': missing _recruitment, panel, tab, detail or role reference; roles must be unique.", this);
            _hasLoggedConfigurationFailure = true;
        }
        _isConfigured = valid;
        return valid;
    }

    private void OnEnable()
    {
        if (!EnsureConfigured()) return;
        _townStatusTab.onClick.AddListener(ShowTownStatus);
        _recruitmentTab.onClick.AddListener(ShowRecruitment);
        _closeButton.onClick.AddListener(RequestClose);
        _detailCard.RecruitRequested += HandleRecruitClick;
        foreach (TownHallRoleView role in _roles) role.Selected += SelectRole;
    }

    private void OnDisable()
    {
        EndSession();
        if (!_isConfigured) return;
        _townStatusTab.onClick.RemoveListener(ShowTownStatus);
        _recruitmentTab.onClick.RemoveListener(ShowRecruitment);
        _closeButton.onClick.RemoveListener(RequestClose);
        _detailCard.RecruitRequested -= HandleRecruitClick;
        foreach (TownHallRoleView role in _roles) if (role) role.Selected -= SelectRole;
    }

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!EnsureConfigured() || !_recruitment.isActiveAndEnabled) return false;
        _source = source;
        _sourceOwner = source as Component;
        if (IsOpen) StartSession();
        return true;
    }

    protected override void OnOpened() => StartSession();
    protected override void OnBeforeClose() => EndSession();

    private void StartSession()
    {
        if (!EnsureConfigured()) return;
        _isSessionOpen = true;
        _resultText.text = string.Empty;
        _selectedRole = null;
        foreach (TownHallRoleView role in _roles)
        {
            if (!_recruitment.TryGetRecruitment(role.NpcType, out _)) continue;
            if (!_selectedRole || role.NpcType == NPCType.Farmer) _selectedRole = role;
        }
        if (!_selectedRole) _selectedRole = _roles[0];
        _detailCard.BindRole(_selectedRole);
        ShowRecruitment();
        Refresh();
    }

    private void EndSession()
    {
        _isSessionOpen = false;
        _selectedRole = null;
        _source = null;
        _sourceOwner = null;
    }

    private void Update()
    {
        if (!_isSessionOpen) return;
        if (!_recruitment || !_recruitment.isActiveAndEnabled ||
            (_source != null && (!_sourceOwner || !_sourceOwner.gameObject.activeInHierarchy ||
                (_sourceOwner is Behaviour behaviour && !behaviour.isActiveAndEnabled) ||
                !_source.TryGetClickPopup(out PopupType popupType) || popupType != PopupType.TownHall)))
        {
            RequestClose();
            return;
        }
        if (_recruitmentPanel.activeSelf) Refresh();
    }

    private void ShowTownStatus() => SelectTab(false);
    private void ShowRecruitment() => SelectTab(true);
    private void SelectTab(bool recruitment)
    {
        _townStatusPanel.SetActive(!recruitment);
        _recruitmentPanel.SetActive(recruitment);
        _townStatusTab.interactable = recruitment;
        _recruitmentTab.interactable = !recruitment;
        if (_townStatusUnderline) _townStatusUnderline.SetActive(!recruitment);
        if (_recruitmentUnderline) _recruitmentUnderline.SetActive(recruitment);
    }

    private void SelectRole(NPCType npcType)
    {
        foreach (TownHallRoleView role in _roles)
        {
            if (role.NpcType != npcType) continue;
            _selectedRole = role;
            _detailCard.BindRole(role);
            Refresh();
            return;
        }
    }

    private void Refresh()
    {
        foreach (TownHallRoleView role in _roles)
        {
            bool available = _recruitment.TryGetRecruitment(role.NpcType, out RecruitmentStatus status);
            role.Refresh(status, available, role == _selectedRole);
            if (role == _selectedRole) _detailCard.Refresh(status, available);
        }
    }

    private void HandleRecruitClick(NPCType npcType)
    {
        if (!_isSessionOpen || !_selectedRole || npcType != _selectedRole.NpcType) return;
        RecruitResult result = _recruitment.TryDispatchCandidate(npcType);
        _resultText.text = result switch
        {
            RecruitResult.Success => string.Empty,
            RecruitResult.NotEnoughGold => "정착지원금이 부족합니다.",
            RecruitResult.SpawnUnavailable => "지금은 이 직업을 모집할 수 없습니다.",
            RecruitResult.NoArcherStation => "궁병을 배치할 빈 성벽 자리가 없습니다.",
            _ => "아직 모집할 수 없습니다.",
        };
        Refresh();
    }
}
