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
    [SerializeField] private ScrollRect _recruitmentScrollRect;
    [SerializeField] private bool _fitToCanvas;
    [SerializeField, Min(0f)] private float _screenMargin = 24f;
    private bool _isConfigured;
    private RectTransform _popupRect;
    private RectTransform _parentRect;
    private RectTransform _canvasRect;
    private Vector3 _authoredScale;

    private void Awake()
    {
        _popupRect = transform as RectTransform;
        _parentRect = transform.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        _canvasRect = canvas ? canvas.rootCanvas.transform as RectTransform : null;
        _authoredScale = transform.localScale;
        _isConfigured = _recruitment && _townStatusPanel && _recruitmentPanel &&
            _townStatusTab && _recruitmentTab && _resultText && _cards != null && _cards.Length > 0;
        if (_isConfigured)
        {
            var roles = new System.Collections.Generic.HashSet<NPCType>();
            foreach (TownHallRecruitCard card in _cards)
                _isConfigured &= card && roles.Add(card.NpcType);
        }
        if (!_isConfigured)
            Debug.LogError($"TownHallPopup '{name}': missing recruitment, tabs, panels or cards, or duplicate card role.", this);
    }

    private void OnEnable()
    {
        FitToCanvas();
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
    private void ShowRecruitment()
    {
        SelectTab(true);
        if (!_recruitmentScrollRect) return;
        Canvas.ForceUpdateCanvases();
        _recruitmentScrollRect.StopMovement();
        _recruitmentScrollRect.horizontalNormalizedPosition = 0f;
    }

    private void LateUpdate() => FitToCanvas();

    private void OnValidate() => _screenMargin = Mathf.Max(0f, _screenMargin);

    private void FitToCanvas()
    {
        if (!_fitToCanvas || !_popupRect || !_parentRect || !_canvasRect) return;
        Vector2 size = _popupRect.rect.size;
        Vector3 canvasSize = _parentRect.InverseTransformVector(_canvasRect.TransformVector(_canvasRect.rect.size));
        Vector2 available = new Vector2(Mathf.Abs(canvasSize.x), Mathf.Abs(canvasSize.y)) -
            Vector2.one * (_screenMargin * 2f);
        float width = size.x * Mathf.Abs(_authoredScale.x);
        float height = size.y * Mathf.Abs(_authoredScale.y);
        if (width <= 0f || height <= 0f || available.x <= 0f || available.y <= 0f) return;
        float scale = Mathf.Min(1f, Mathf.Min(available.x / width, available.y / height));
        // PopBase owns anchoredPosition during entry/exit; fitting changes only scale.
        _popupRect.localScale = new Vector3(_authoredScale.x * scale, _authoredScale.y * scale, _authoredScale.z);
    }

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
            case RecruitResult.NoArcherStation:
                _resultText.text = "궁병을 배치할 빈 성벽 자리가 없습니다.";
                break;
        }
        Refresh();
    }
}
