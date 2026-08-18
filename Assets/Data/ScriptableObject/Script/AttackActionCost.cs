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

    /// <summary>
    /// Single source of truth for the attack-range check, shared by GuardActionSelector
    /// (replan-time Move-vs-Attack decision) and AttackAction (per-hit range recheck).
    /// </summary>
    public bool IsInRange(Vector3 from, Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - from;
        offset.z = 0f;
        return offset.sqrMagnitude <= AttackRange * AttackRange;
    }
}
