using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "FarmingActionCost", menuName = "Scriptable Objects/ActionCost/FarmingActionCost")]
public class FarmingActionCost: DefaultActionCost
{
    public override ActionType MyType => ActionType.Farming;

    public float FarmingActionPerHunger;

    [FormerlySerializedAs("FarmingAcitonPerThirst")]
    public float FarmingActionPerThirst;

    public float FarmingActionPerFatigue;
}
