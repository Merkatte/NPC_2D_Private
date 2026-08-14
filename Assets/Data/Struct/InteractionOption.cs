public readonly struct InteractionOption
{
    public ActionType Type { get; }
    public int ItemId { get; }
    public StatEffect Effect { get; }

    public InteractionOption(ActionType type, int itemId, StatEffect effect)
    {
        Type = type;
        ItemId = itemId;
        Effect = effect;
    }
}
