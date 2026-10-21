using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TownHallRoleView : MonoBehaviour
{
    [SerializeField] private NPCType _npcType;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Image _portrait;
    [SerializeField] private Image _toolIcon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Button _selectButton;
    [SerializeField] private GameObject _selectionIndicator;
    [SerializeField] private Image _background;
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _unselectedColor = new Color(1f, 1f, 1f, 0.65f);
    private string _lastStatus;
    public NPCType NpcType => _npcType;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Portrait => _portrait ? _portrait.sprite : null;
    public Sprite ToolIcon => _toolIcon ? _toolIcon.sprite : null;
    public bool IsConfigured => _portrait && _toolIcon && _nameText && _statusText && _selectButton && _selectionIndicator;
    public event Action<NPCType> Selected;

    private void OnEnable()
    {
        if (_selectButton) _selectButton.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (_selectButton) _selectButton.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick() => Selected?.Invoke(_npcType);

    public void Refresh(RecruitmentStatus status, bool available, bool selected)
    {
        if (!IsConfigured) return;
        _nameText.text = _displayName;
        _selectionIndicator.SetActive(selected);
        if (_background) _background.color = selected ? _selectedColor : _unselectedColor;
        string label = TownHallRecruitCard.GetStatusLabel(status, available);
        if (_lastStatus != label) { _lastStatus = label; _statusText.text = label; }
    }
}
