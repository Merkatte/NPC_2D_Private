using UnityEngine;

[CreateAssetMenu(fileName = "DefaultActionCost", menuName = "Scriptable Objects/ActionCost/DefaultActionCost")]
public abstract class DefaultActionCost : ScriptableObject
{
    public abstract ActionType MyType { get; }
}
