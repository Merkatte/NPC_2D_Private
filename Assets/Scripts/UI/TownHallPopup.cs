using UnityEngine;
using UnityEngine.UI;

public sealed class TownHallPopup : PopBase
{
    [SerializeField] private TownHallRecruitment _recruitment;
    [SerializeField] private GameObject _townStatusPanel;
    [SerializeField] private GameObject _recruitmentPanel;
    [SerializeField] private Button _townStatusTab;
    [SerializeField] private Button _recruitmentTab;
    [SerializeField] private TownHallRecruitCard[] _cards;
    [SerializeField] private Text _resultText;
    private bool _isConfigured;

    private void Awake()
    {
        _isConfigured = _recruitment && _townStatusPanel && _recruitmentPanel &&
            _townStatusTab && _recruitmentTab && _resultText && _cards != null && _cards.Length == 2;
        if (_isConfigured)
            foreach (TownHallRecruitCard card in _cards)
                _isConfigured &= card;
        if (!_isConfigured)
            Debug.LogError($"TownHallPopup '{name}': missing recruitment, tabs, panels or cards.", this);
    }

    private void OnEnable()
    {
        if (!_isConfigured)
            return;
        _townStatusTab.onClick.AddListener(ShowTownStatus);
        _recruitmentTab.onClick.AddListener(ShowRecruitment);
        foreach (TownHallRecruitCard card in _cards)
            card.RecruitRequested += HandleRecruitClick;
    }

    private void OnDisable()
    {
        if (!_isConfigured)
            return;
        _townStatusTab.onClick.RemoveListener(ShowTownStatus);
        _recruitmentTab.onClick.RemoveListener(ShowRecruitment);
        foreach (TownHallRecruitCard card in _cards)
            if (card)
                card.RecruitRequested -= HandleRecruitClick;
    }

    protected override void OnOpened()
    {
        if (!_isConfigured)
            return;
        _resultText.text = string.Empty;
        ShowRecruitment();
        Refresh();
    }

    private void Update()
    {
        if (_isConfigured && _recruitmentPanel.activeSelf)
            Refresh();
    }

    private void ShowTownStatus() => SelectTab(false);
    private void ShowRecruitment() => SelectTab(true);

    private void SelectTab(bool recruitment)
    {
        _townStatusPanel.SetActive(!recruitment);
        _recruitmentPanel.SetActive(recruitment);
        _townStatusTab.interactable = recruitment;
        _recruitmentTab.interactable = !recruitment;
    }

    private void Refresh()
    {
        foreach (TownHallRecruitCard card in _cards)
        {
            bool available = _recruitment.TryGetRecruitment(card.NpcType, out RecruitmentStatus status);
            card.Refresh(status, available);
        }
    }

    private void HandleRecruitClick(NPCType npcType)
    {
        RecruitResult result = _recruitment.TryDispatchCandidate(npcType);
        switch (result)
        {
            case RecruitResult.Success:
                _resultText.text = string.Empty;
                break;
            case RecruitResult.NotEnoughGold:
                _resultText.text = "정착지원금이 부족합니다.";
                break;
            case RecruitResult.SpawnUnavailable:
                _resultText.text = "지금은 이 직업을 모집할 수 없습니다.";
                break;
        }
        Refresh();
    }
}
