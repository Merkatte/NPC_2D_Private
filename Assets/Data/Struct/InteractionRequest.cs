public readonly struct InteractionRequest
{
    public const int NoOptionId = -1;

    public ActionType Type { get; }
    public int OptionId { get; }
    public float Strength { get; }

    // The actor's own carried inventory taking part in this transaction. A provider that
    // PRODUCES items (Farm/Harvest) adds into it; one that RECEIVES items (Warehouse/Deposit)
    // drains from it. Null for interactions that move no items (Eat/Drink/Farming/Sleep).
    // Always a plain C# object (never a Unity object), so a plain null check is correct.
    public ICarriedInventory Cargo { get; }

    public bool HasOption => OptionId >= 0;
    public bool HasCargo => Cargo != null;
    public bool HasValidStrength =>
        Strength > 0f && !float.IsNaN(Strength) && !float.IsInfinity(Strength);

    public InteractionRequest(ActionType type, int optionId = NoOptionId, float strength = 1f,
        ICarriedInventory cargo = null)
    {
        Type = type;
        OptionId = optionId;
        Strength = strength;
        Cargo = cargo;
    }
}
