using UnityEngine;

[CreateAssetMenu(fileName = "DefaultActionCost", menuName = "Scriptable Objects/ActionCost/DefaultActionCost")]
public class DefaultActionCost : ScriptableObject
{
    public float MovePerThirst;
    public float MovePerHunger;
    public float MovePerFatigue;
}
