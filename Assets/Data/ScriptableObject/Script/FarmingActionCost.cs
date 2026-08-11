using UnityEngine;
[CreateAssetMenu(fileName = "FarmingActionCost", menuName = "Scriptable Objects/ActionCost/FarmingActionCost")]
public class FarmingActionCost: DefaultActionCost
{
    public float FarmingActionPerHunger;
    public float FarmingActionPerThirst;
    public float FarmingActionPerFatigue;
}
