public readonly struct InteractionOption
{
    public ActionType Type { get; }
    public int OptionId { get; }
    public StatEffect ActorEffect { get; }

    public InteractionOption(ActionType type, int optionId, StatEffect actorEffect)
    {
        Type = type;
        OptionId = optionId;
        ActorEffect = actorEffect;
    }
}
