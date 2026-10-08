public sealed class HousingLifeSettings
{
    public float HomeFatigueRecoveryPerSecond { get; }
    public float InnFatigueRecoveryPerSecond { get; }
    public float HomeHungerPerSecond { get; }
    public float HomeThirstPerSecond { get; }
    public float ReassessmentSeconds { get; }

    public HousingLifeSettings(float homeFatigue, float innFatigue, float hunger, float thirst, float reassessment)
    {
        HomeFatigueRecoveryPerSecond = homeFatigue;
        InnFatigueRecoveryPerSecond = innFatigue;
        HomeHungerPerSecond = hunger;
        HomeThirstPerSecond = thirst;
        ReassessmentSeconds = reassessment;
    }
}
