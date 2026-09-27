public sealed class BuilderWorkDefinition
{
    public float WorkPerSecond { get; }
    public float HungerPerSecond { get; }
    public float ThirstPerSecond { get; }
    public float FatiguePerSecond { get; }
    public BuilderWorkDefinition(float work, float hunger, float thirst, float fatigue)
    {
        WorkPerSecond = work;
        HungerPerSecond = hunger;
        ThirstPerSecond = thirst;
        FatiguePerSecond = fatigue;
    }
}
