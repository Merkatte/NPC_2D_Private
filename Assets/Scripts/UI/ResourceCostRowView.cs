using UnityEngine;
using UnityEngine.UI;

public sealed class ResourceCostRowView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _name;
    [SerializeField] private Text _required;
    [SerializeField] private Text _owned;
    [SerializeField] private Color _normalColor = new Color(0.25f, 0.15f, 0.08f, 1f);
    [SerializeField] private Color _shortageColor = new Color(0.75f, 0.12f, 0.08f, 1f);
    public bool IsConfigured => _name && _required && _owned;

    public void Bind(string itemName, Sprite icon, int required, int owned, bool isRequirement = true)
    {
        if (!IsConfigured) return;
        _name.text = itemName;
        _required.text = required.ToString();
        _owned.text = owned.ToString();
        _owned.color = isRequirement && owned < required ? _shortageColor : _normalColor;
        if (_icon) { _icon.sprite = icon; _icon.enabled = icon; }
    }
}
