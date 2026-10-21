using UnityEngine;
using UnityEngine.UI;

public sealed class HouseResidentView : MonoBehaviour
{
    [SerializeField] private Text _name;
    [SerializeField] private Image _portrait;
    public bool IsConfigured => _name;

    public void Bind(string residentName, Sprite portrait)
    {
        if (_name) _name.text = residentName;
        if (_portrait) { _portrait.sprite = portrait; _portrait.enabled = portrait; }
    }
}
