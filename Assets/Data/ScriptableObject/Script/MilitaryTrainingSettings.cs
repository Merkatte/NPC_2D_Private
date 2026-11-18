using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Military Training Settings")]
public sealed class MilitaryTrainingSettings : ScriptableObject
{
    [SerializeField, Min(0.01f)] private float _durationSeconds = 30f;
    [SerializeField, Min(0.01f)] private float _proficiencyGain = 20f;
    [SerializeField, Min(1)] private int _trainingLevelCap = 2;

    public float DurationSeconds => PositiveOrDefault(_durationSeconds, 30f);
    public float ProficiencyGain => PositiveOrDefault(_proficiencyGain, 20f);
    public int TrainingLevelCap => Mathf.Max(1, _trainingLevelCap);

    private static float PositiveOrDefault(float value, float fallback)
        => value > 0f && !float.IsInfinity(value) ? value : fallback;

    private void OnValidate()
    {
        _durationSeconds = DurationSeconds;
        _proficiencyGain = ProficiencyGain;
        _trainingLevelCap = TrainingLevelCap;
    }
}
