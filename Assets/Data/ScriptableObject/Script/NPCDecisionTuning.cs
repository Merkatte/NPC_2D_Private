using UnityEngine;

/// <summary>
/// Tuning knobs for <see cref="DestinationDecider"/>. All values below are temporary
/// prototype placeholders, not balanced game values, until a real need system exists.
/// Threshold fields are normalized 0..1 (fraction of each stat's max), not percentages.
/// </summary>
[CreateAssetMenu(fileName = "NPCDecisionTuning", menuName = "Scriptable Objects/NPCDecisionTuning")]
public class NPCDecisionTuning : ScriptableObject
{
    [Header("Need thresholds (0..1 normalized)")]
    [SerializeField] [Range(0f, 1f)] private float _dangerThreshold = 0.85f;
    [SerializeField] [Range(0f, 1f)] private float _criticalNeedThreshold = 0.95f;

    [Header("Work batch")]
    [SerializeField] private int _minimumWorkBatch = 2;
    [SerializeField] private int _maximumWorkBatch = 5;

    [Header("Utility weights")]
    [SerializeField] private float _switchMargin = 30f;
    [SerializeField] private float _workValue = 100f;
    [SerializeField] private float _workCapacityWeight = 10f;
    [SerializeField] private float _travelWeight = 3f;
    [SerializeField] private float _riskWeight = 5f;
    [SerializeField] private float _dangerPenaltyMultiplier = 5f;
    [SerializeField] private float _healthWeight = 50f;

    public float DangerThreshold => _dangerThreshold;
    public float CriticalNeedThreshold => _criticalNeedThreshold;
    public int MinimumWorkBatch => _minimumWorkBatch;
    public int MaximumWorkBatch => _maximumWorkBatch;
    public float SwitchMargin => _switchMargin;
    public float WorkValue => _workValue;
    public float WorkCapacityWeight => _workCapacityWeight;
    public float TravelWeight => _travelWeight;
    public float RiskWeight => _riskWeight;
    public float DangerPenaltyMultiplier => _dangerPenaltyMultiplier;
    public float HealthWeight => _healthWeight;

    private void OnValidate()
    {
        _minimumWorkBatch = Mathf.Max(1, _minimumWorkBatch);
        _maximumWorkBatch = Mathf.Max(_minimumWorkBatch, _maximumWorkBatch);
        _dangerThreshold = Mathf.Clamp01(_dangerThreshold);
        _criticalNeedThreshold = Mathf.Max(_dangerThreshold, Mathf.Clamp01(_criticalNeedThreshold));
        _switchMargin = Mathf.Max(0f, _switchMargin);
        _workValue = Mathf.Max(0f, _workValue);
        _workCapacityWeight = Mathf.Max(0f, _workCapacityWeight);
        _travelWeight = Mathf.Max(0f, _travelWeight);
        _riskWeight = Mathf.Max(0f, _riskWeight);
        _dangerPenaltyMultiplier = Mathf.Max(0f, _dangerPenaltyMultiplier);
        _healthWeight = Mathf.Max(0f, _healthWeight);
    }
}
