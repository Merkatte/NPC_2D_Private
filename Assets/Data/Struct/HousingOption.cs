public sealed class HousingOption
{
    public int Id { get; }
    public HousingEffectType EffectType { get; }
    public float Value { get; }

    public HousingOption(int id, HousingEffectType effectType, float value)
    {
        Id = id;
        EffectType = effectType;
        Value = value;
    }
}
