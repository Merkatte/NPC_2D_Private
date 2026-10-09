using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Defense/Combat Settings")]
public sealed class DefenseCombatSettings : ScriptableObject
{
    [SerializeField, Min(0.05f)] private float _reassessmentSeconds = 0.25f;
    [SerializeField, Min(0.1f)] private float _localCombatRange = 6f;
    [SerializeField, Min(0.1f)] private float _fleeDetectionRange = 5f;
    [SerializeField, Min(0.1f)] private float _fleeDistance = 7f;
    [SerializeField, Range(0.05f, 0.95f)] private float _hitTimeRatio = 0.4f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float _speechDuration = 2f;
    [SerializeField] private string _fleeMessage = "으아아아악! 살려줘!";
    public float ReassessmentSeconds => Mathf.Max(0.05f, _reassessmentSeconds);
    public float LocalCombatRange => Mathf.Max(0.1f, _localCombatRange);
    public float FleeDetectionRange => Mathf.Max(0.1f, _fleeDetectionRange);
    public float FleeDistance => Mathf.Max(0.1f, _fleeDistance);
    public float HitTimeRatio => Mathf.Clamp(_hitTimeRatio, 0.05f, 0.95f);
    public float ProjectileSpeed => Mathf.Max(0.1f, _projectileSpeed);
    public float SpeechDuration => Mathf.Max(0.1f, _speechDuration);
    public string FleeMessage => _fleeMessage;
    private void OnValidate()
    {
        _reassessmentSeconds = ReassessmentSeconds; _localCombatRange = LocalCombatRange;
        _fleeDetectionRange = FleeDetectionRange; _fleeDistance = FleeDistance;
        _hitTimeRatio = HitTimeRatio; _projectileSpeed = ProjectileSpeed; _speechDuration = SpeechDuration;
    }
}
