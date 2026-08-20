using UnityEngine;

[CreateAssetMenu(fileName = "GuardActionCost", menuName = "Scriptable Objects/ActionCost/GuardActionCost")]
public class GuardActionCost : DefaultActionCost
{
    public override ActionType MyType => ActionType.Guard;

    [Header("Patrol")]
    public float PatrolArrivalDistance = 0.2f;
    public int PatrolPointCount = 4;

    // Rate x FarmingAction._workingTime(3s) should stay below the matching FarmingActionPer* value
    // so Guard's needs decay slower per second than one Farming completion costs. See GQ-009 in
    // PublicMD/Guard_Action_Implementation_Plan.md.
    [Header("Need decay per second")]
    public float HungerPerSecond = 0.3f;
    public float ThirstPerSecond = 0.3f;
    public float FatiguePerSecond = 0.3f;

    [Header("Interrupt thresholds (0..1 normalized)")]
    [Range(0f, 1f)] public float HungerInterruptThreshold = 0.97f;
    [Range(0f, 1f)] public float ThirstInterruptThreshold = 0.97f;
    [Range(0f, 1f)] public float FatigueInterruptThreshold = 0.97f;

    [Header("Combat")]
    [SerializeField, Range(0f, 1f)] private float _attackStoppingDistanceRatio = 0.9f;

    public float AttackStoppingDistanceRatio => _attackStoppingDistanceRatio;

    private void OnValidate()
    {
        PatrolArrivalDistance = Mathf.Max(0.01f, PatrolArrivalDistance);
        PatrolPointCount = Mathf.Max(1, PatrolPointCount);

        HungerPerSecond = Mathf.Max(0f, HungerPerSecond);
        ThirstPerSecond = Mathf.Max(0f, ThirstPerSecond);
        FatiguePerSecond = Mathf.Max(0f, FatiguePerSecond);

        HungerInterruptThreshold = Mathf.Clamp01(HungerInterruptThreshold);
        ThirstInterruptThreshold = Mathf.Clamp01(ThirstInterruptThreshold);
        FatigueInterruptThreshold = Mathf.Clamp01(FatigueInterruptThreshold);

        _attackStoppingDistanceRatio = Mathf.Clamp01(_attackStoppingDistanceRatio);
    }

    /// <summary>
    /// Single source of truth for "is this need above Guard's own interrupt threshold".
    /// Shared by GuardAction (per-tick check) and GuardActionSelector (replan-time check)
    /// so the two never drift out of sync.
    /// </summary>
    public bool ShouldInterrupt(IStatView stat)
    {
        if (Normalize(stat.GetHunger, stat.GetHungerMax) >= HungerInterruptThreshold)
            return true;
        if (Normalize(stat.GetThirst, stat.GetThirstMax) >= ThirstInterruptThreshold)
            return true;
        if (Normalize(stat.GetFatigue, stat.GetFatigueMax) >= FatigueInterruptThreshold)
            return true;

        return false;
    }

    private static float Normalize(float value, float max)
    {
        return max <= 0f ? 0f : value / max;
    }
}
