using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TownHallRecruitCard : MonoBehaviour
{
    [SerializeField] private NPCType _npcType;
    [SerializeField] private Image _portrait;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _costText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _remainingText;
    [SerializeField] private Button _recruitButton;

    private int _lastSeconds = -1;
    private int _lastCost = -1;
    private string _lastStatus;
    public NPCType NpcType => _npcType;
    public event Action<NPCType> RecruitRequested;

    private void Awake()
    {
        if (!_portrait || !_nameText || !_costText || !_statusText || !_remainingText || !_recruitButton)
        {
            Debug.LogError($"TownHallRecruitCard '{name}': missing a view reference.", this);
            enabled = false;
            return;
        }
        _nameText.text = _npcType == NPCType.Guard ? "경비병" : "농부";
    }

    private void OnEnable()
    {
        if (_recruitButton)
            _recruitButton.onClick.AddListener(HandleRecruitClick);
    }

    private void OnDisable()
    {
        if (_recruitButton)
            _recruitButton.onClick.RemoveListener(HandleRecruitClick);
    }

    private void HandleRecruitClick() => RecruitRequested?.Invoke(_npcType);

    public void Refresh(RecruitmentStatus status, bool available)
    {
        if (!enabled)
            return;
        _recruitButton.interactable = available && status.CanRecruit;
        if (_lastCost != status.SettlementCost)
        {
            _lastCost = status.SettlementCost;
            _costText.text = $"{_lastCost} 골드";
        }
        string label = !available ? "모집 불가" : status.IsDispatching ? "도착 중" : status.CanRecruit ? "모집 가능" : "모집 중";
        if (_lastStatus != label)
        {
            _lastStatus = label;
            _statusText.text = label;
        }
        int seconds = Mathf.CeilToInt(status.CooldownRemaining);
        if (_lastSeconds != seconds)
        {
            _lastSeconds = seconds;
            _remainingText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
