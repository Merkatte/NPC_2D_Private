using UnityEngine;

[CreateAssetMenu(fileName = "AttackActionCost", menuName = "Scriptable Objects/ActionCost/AttackActionCost")]
public class AttackActionCost : DefaultActionCost
{
    public override ActionType MyType => ActionType.Attack;

    public float AttackRange = 1f;

    private void OnValidate()
    {
        AttackRange = Mathf.Max(0.01f, AttackRange);
    }
}
