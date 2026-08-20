using UnityEngine;

/// <summary>
/// Tuning knobs for <see cref="DestinationDecider"/>. All values below are temporary
/// prototype placeholders, not balanced game values, until a real need system exists.
/// Threshold fields are normalized 0..1 (fraction of each stat's max), not percentages.
/// </summary>
[CreateAssetMenu(fileName = "NPCDecisionTuning", menuName = "Scriptable Objects/NPCDecisionTuning")]
public class NPCDecisionTuning : ScriptableObject
{
    /// <summary>Hard upper bound on look-ahead. Deeper planning is deliberately out of scope.</summary>
    public const int MaxLookAheadDepth = 3;

    [Header("Need thresholds (0..1 normalized)")]
    [SerializeField] [Range(0f, 1f)] private float _dangerThreshold = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float _criticalNeedThreshold = 0.95f;

    [Header("Work batch")]
    [SerializeField] private int _minimumWorkBatch = 2;
    [SerializeField] private int _maximumWorkBatch = 5;

    [Header("Risk curve")]
    [SerializeField] private float _needRiskExponent = 3f;
    [SerializeField] private float _healthRiskExponent = 2f;
    [SerializeField] private float _dangerPenaltyMultiplier = 5f;
    [SerializeField] private float _healthWeight = 50f;

    [Header("Score weights")]
    [SerializeField] private float _workValue = 100f;
    [SerializeField] private float _travelWeight = 8f;
    [SerializeField] private float _actionTimeWeight = 1f;
    [SerializeField] private float _riskExposureWeight = 1f;
    [SerializeField] private float _terminalRiskWeight = 100f;

    [Header("Bounded look-ahead")]
    [SerializeField] [Range(0f, 1f)] private float _futureDiscount = 0.9f;
    [SerializeField] [Range(1, MaxLookAheadDepth)] private int _lookAheadDepth = 2;

    // Prediction-only estimates. They currently mirror the durations hardcoded inside
    // EatAction/DrinkAction/SleepAction/FarmingAction/IdleAction and can drift from them.
    // Unifying runtime action durations into shared action tuning is a separate later task.
    [Header("Estimated action durations (prediction only - not authoritative runtime values)")]
    [SerializeField] private float _estimatedEatSeconds = 2f;
    [SerializeField] private float _estimatedDrinkSeconds = 1f;
    [SerializeField] private float _estimatedSleepSeconds = 2f;
    [SerializeField] private float _estimatedFarmingSeconds = 3f;
    [SerializeField] private float _estimatedIdleSeconds = 1f;

    // GuardDutyEvaluationSeconds is an evaluation-only approximation used to price "keep
    // standing guard" against other candidates in the same units. It is NOT a runtime replan
    // period and NOT an authoritative duration: GuardAction keeps running until enemy
    // detection or GuardActionCost.ShouldInterrupt(...) fires, never on this schedule.
    [Header("Guard duty (evaluation slice, not a runtime replan period)")]
    [SerializeField] private float _guardDutyEvaluationSeconds = 3f;
    [SerializeField] private float _guardDutyValuePerSecond = 20f;

    [Header("Development")]
    [SerializeField] private bool _logDecisionTrace = false;

    public float DangerThreshold => _dangerThreshold;
    public float CriticalNeedThreshold => _criticalNeedThreshold;
    public int MinimumWorkBatch => _minimumWorkBatch;
    public int MaximumWorkBatch => _maximumWorkBatch;

    public float NeedRiskExponent => _needRiskExponent;
    public float HealthRiskExponent => _healthRiskExponent;
    public float DangerPenaltyMultiplier => _dangerPenaltyMultiplier;
    public float HealthWeight => _healthWeight;

    public float WorkValue => _workValue;
    public float TravelWeight => _travelWeight;
    public float ActionTimeWeight => _actionTimeWeight;
    public float RiskExposureWeight => _riskExposureWeight;
    public float TerminalRiskWeight => _terminalRiskWeight;

    public float FutureDiscount => _futureDiscount;
    public int LookAheadDepth => _lookAheadDepth;

    public float EstimatedEatSeconds => _estimatedEatSeconds;
    public float EstimatedDrinkSeconds => _estimatedDrinkSeconds;
    public float EstimatedSleepSeconds => _estimatedSleepSeconds;
    public float EstimatedFarmingSeconds => _estimatedFarmingSeconds;
    public float EstimatedIdleSeconds => _estimatedIdleSeconds;

    /// <summary>
    /// Length of one imagined Guard duty slice used only to score the duty candidate.
    /// Not a runtime replan period and not an authoritative action duration.
    /// </summary>
    public float GuardDutyEvaluationSeconds => _guardDutyEvaluationSeconds;
    public float GuardDutyValuePerSecond => _guardDutyValuePerSecond;

    public bool LogDecisionTrace => _logDecisionTrace;

    private const float MinEstimatedSeconds = 0.01f;

    private void OnValidate()
    {
        _minimumWorkBatch = Mathf.Max(1, _minimumWorkBatch);
        _maximumWorkBatch = Mathf.Max(_minimumWorkBatch, _maximumWorkBatch);
        _dangerThreshold = Mathf.Clamp01(_dangerThreshold);
        _criticalNeedThreshold = Mathf.Max(_dangerThreshold, Mathf.Clamp01(_criticalNeedThreshold));

        // Exponents stay >= 1 so pow() never turns a clamped 0..1 input into infinity.
        _needRiskExponent = Mathf.Max(1f, _needRiskExponent);
        _healthRiskExponent = Mathf.Max(1f, _healthRiskExponent);
        _dangerPenaltyMultiplier = Mathf.Max(0f, _dangerPenaltyMultiplier);
        _healthWeight = Mathf.Max(0f, _healthWeight);

        _workValue = Mathf.Max(0f, _workValue);
        _travelWeight = Mathf.Max(0f, _travelWeight);
        _actionTimeWeight = Mathf.Max(0f, _actionTimeWeight);
        _riskExposureWeight = Mathf.Max(0f, _riskExposureWeight);
        _terminalRiskWeight = Mathf.Max(0f, _terminalRiskWeight);

        _futureDiscount = Mathf.Clamp01(_futureDiscount);
        _lookAheadDepth = Mathf.Clamp(_lookAheadDepth, 1, MaxLookAheadDepth);

        _estimatedEatSeconds = Mathf.Max(MinEstimatedSeconds, _estimatedEatSeconds);
        _estimatedDrinkSeconds = Mathf.Max(MinEstimatedSeconds, _estimatedDrinkSeconds);
        _estimatedSleepSeconds = Mathf.Max(MinEstimatedSeconds, _estimatedSleepSeconds);
        _estimatedFarmingSeconds = Mathf.Max(MinEstimatedSeconds, _estimatedFarmingSeconds);
        _estimatedIdleSeconds = Mathf.Max(MinEstimatedSeconds, _estimatedIdleSeconds);

        _guardDutyEvaluationSeconds = Mathf.Max(MinEstimatedSeconds, _guardDutyEvaluationSeconds);
        _guardDutyValuePerSecond = Mathf.Max(0f, _guardDutyValuePerSecond);
    }
}
