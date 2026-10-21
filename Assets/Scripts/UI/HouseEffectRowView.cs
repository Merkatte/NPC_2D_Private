using UnityEngine;
using UnityEngine.UI;

public sealed class HouseEffectRowView : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private Text _value;
    [SerializeField] private Text _change;
    public bool IsConfigured => _label && _value;

    public void Bind(string label, string value, string change = "")
    {
        if (!IsConfigured) return;
        _label.text = label;
        _value.text = value;
        if (_change) _change.text = change;
    }
}
