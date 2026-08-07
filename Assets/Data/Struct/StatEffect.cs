using UnityEngine;

public class StatEffect
{
    public float HealthDelta { get; }
    public float HungerDelta { get; }
    public float ThirstDelta { get; }
    public float FatigueDelta { get; }
    public float MoodDelta { get; }

    public StatEffect(
        float healthDelta = 0f,
        float hungerDelta = 0f,
        float thirstDelta = 0f,
        float fatigueDelta = 0f,
        float moodDelta = 0f)
    {
        HealthDelta = healthDelta;
        HungerDelta = hungerDelta;
        ThirstDelta = thirstDelta;
        FatigueDelta = fatigueDelta;
        MoodDelta = moodDelta;
    }
}
