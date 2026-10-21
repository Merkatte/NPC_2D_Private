using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TownHallRecruitCard : MonoBehaviour
{
    [SerializeField] private NPCType _npcType;
    [SerializeField] private Image _portrait;
    [SerializeField] private Image _toolIcon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Text _costText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _remainingText;
    [SerializeField] private Button _recruitButton;
    private int _lastSeconds = int.MinValue;
    private int _lastCost = int.MinValue;
    private string _lastStatus;
    public NPCType NpcType => _npcType;
    public bool IsConfigured => _portrait && _nameText && _costText && _statusText && _remainingText && _recruitButton;
    public event Action<NPCType> RecruitRequested;

    private void OnEnable()
    {
        if (_recruitButton) _recruitButton.onClick.AddListener(HandleRecruitClick);
    }

    private void OnDisable()
    {
        if (_recruitButton) _recruitButton.onClick.RemoveListener(HandleRecruitClick);
    }

    private void HandleRecruitClick() => RecruitRequested?.Invoke(_npcType);

    public void BindRole(TownHallRoleView role)
    {
        if (!role || !IsConfigured) return;
        _npcType = role.NpcType;
        _nameText.text = role.DisplayName;
        _portrait.sprite = role.Portrait;
        if (_toolIcon) _toolIcon.sprite = role.ToolIcon;
        if (_descriptionText) _descriptionText.text = role.Description;
        _lastCost = int.MinValue;
        _lastSeconds = int.MinValue;
        _lastStatus = null;
    }

    public static string GetStatusLabel(RecruitmentStatus status, bool available) =>
        !available ? "모집 불가" : status.IsDispatching ? "도착 중" : status.CanRecruit ? "모집 가능" : "모집 중";

    public void Refresh(RecruitmentStatus status, bool available)
    {
        if (!IsConfigured) return;
        _recruitButton.interactable = available && status.CanRecruit;
        int cost = available ? status.SettlementCost : -1;
        if (_lastCost != cost)
        {
            _lastCost = cost;
            _costText.text = available ? $"{cost} G" : "—";
        }
        string label = GetStatusLabel(status, available);
        if (_lastStatus != label) { _lastStatus = label; _statusText.text = label; }
        int seconds = available ? Mathf.CeilToInt(status.CooldownRemaining) : -1;
        if (_lastSeconds != seconds)
        {
            _lastSeconds = seconds;
            _remainingText.text = available ? $"{seconds / 60:00}:{seconds % 60:00}" : "—";
        }
    }
}
