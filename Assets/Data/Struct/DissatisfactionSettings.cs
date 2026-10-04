using System;
using UnityEngine;

[Serializable]
public struct DissatisfactionSettings
{
    [SerializeField, Min(0f)] private float _increasePerSecond;
    [SerializeField, Min(0f)] private float _decreasePerSecond;
    [SerializeField, Min(0.01f)] private float _maximum;
    [SerializeField, Min(0.01f)] private float _strikeThreshold;

    public float IncreasePerSecond => _increasePerSecond;
    public float DecreasePerSecond => _decreasePerSecond;
    public float Maximum => _maximum;
    public float StrikeThreshold => _strikeThreshold;
    public static DissatisfactionSettings Default => new DissatisfactionSettings(1f, 1f, 100f, 60f);

    public bool IsValid => IsFinite(_increasePerSecond) && _increasePerSecond >= 0f
        && IsFinite(_decreasePerSecond) && _decreasePerSecond >= 0f
        && IsFinite(_maximum) && _maximum > 0f
        && IsFinite(_strikeThreshold) && _strikeThreshold > 0f && _strikeThreshold <= _maximum;

    public DissatisfactionSettings(float increasePerSecond, float decreasePerSecond, float maximum, float strikeThreshold)
    {
        _increasePerSecond = increasePerSecond;
        _decreasePerSecond = decreasePerSecond;
        _maximum = maximum;
        _strikeThreshold = strikeThreshold;
    }

    public DissatisfactionSettings Validated()
    {
        DissatisfactionSettings defaults = Default;
        float maximum = IsFinite(_maximum) && _maximum > 0f ? _maximum : defaults.Maximum;
        float threshold = IsFinite(_strikeThreshold) && _strikeThreshold > 0f
            ? Mathf.Min(_strikeThreshold, maximum) : Mathf.Min(defaults.StrikeThreshold, maximum);
        return new DissatisfactionSettings(
            IsFinite(_increasePerSecond) ? Mathf.Max(0f, _increasePerSecond) : defaults.IncreasePerSecond,
            IsFinite(_decreasePerSecond) ? Mathf.Max(0f, _decreasePerSecond) : defaults.DecreasePerSecond,
            maximum, threshold);
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
