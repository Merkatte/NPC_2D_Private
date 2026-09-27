using UnityEngine;
using UnityEngine.UI;

public sealed class ResourceQuantityRow : MonoBehaviour
{
    [SerializeField] private Text _label;
    public void Bind(string displayName, int quantity) { _label.text = $"{displayName}  {quantity}"; }
}
