using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionChoiceRow : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private Sprite _selectedSprite;
    [SerializeField] private Sprite _unselectedSprite;
    private Action<int> _select;
    private int _buildingId;

    public void Bind(BuildingDefinition definition, Sprite icon, Action<int> select)
    {
        _label.text = definition.DisplayName;
        _icon.sprite = icon;
        _icon.enabled = icon;
        _buildingId = definition.Id;
        _select = select;
        _button.onClick.RemoveListener(Select);
        _button.onClick.AddListener(Select);
    }
    public void SetSelected(bool selected)
    {
        if (_background) _background.sprite = selected ? _selectedSprite : _unselectedSprite;
    }
    private void Select() => _select?.Invoke(_buildingId);
    private void OnDisable()
    {
        if (_button) _button.onClick.RemoveListener(Select);
        _select = null;
    }
}
