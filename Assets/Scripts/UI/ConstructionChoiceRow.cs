using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionChoiceRow : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private Image _icon;
    [SerializeField] private Button _button;
    public void Bind(BuildingDefinition definition, Sprite icon, Action<int> select)
    {
        _label.text = definition.DisplayName;
        _icon.sprite = icon;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => select(definition.Id));
    }
    private void OnDestroy() { if (_button) _button.onClick.RemoveAllListeners(); }
}
