using UnityEngine;
using UnityEngine.UI;

public sealed class HouseInfoRow : MonoBehaviour
{
    [SerializeField] private Text _label;
    [SerializeField] private Color _normalColor = new Color(0.25f, 0.15f, 0.08f, 1f);
    [SerializeField] private Color _shortageColor = new Color(0.75f, 0.12f, 0.08f, 1f);

    public bool IsConfigured => _label;

    public void Bind(string text, bool shortage = false)
    {
        _label.text = text;
        _label.color = shortage ? _shortageColor : _normalColor;
    }
}
