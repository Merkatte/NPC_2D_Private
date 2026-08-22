public readonly struct InteractionRequest
{
    public const int NoOptionId = -1;

    public ActionType Type { get; }
    public int OptionId { get; }
    public float Strength { get; }

    public bool HasOption => OptionId >= 0;
    public bool HasValidStrength =>
        Strength > 0f && !float.IsNaN(Strength) && !float.IsInfinity(Strength);

    public InteractionRequest(ActionType type, int optionId = NoOptionId, float strength = 1f)
    {
        Type = type;
        OptionId = optionId;
        Strength = strength;
    }
}
