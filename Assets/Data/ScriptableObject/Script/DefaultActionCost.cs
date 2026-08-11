using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultActionCost", menuName = "Scriptable Objects/ActionCost/DefaultActionCost")]
public abstract class DefaultActionCost : ScriptableObject
{
    public abstract ActionType MyType { get; }
    
    public float MovePerThirst;
    public float MovePerHunger;
    public float MovePerFatigue;
}
