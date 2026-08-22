public readonly struct InteractionResult
{
    public StatEffect ActorEffect { get; }
    public bool HasActorEffect => ActorEffect != null;

    public InteractionResult(StatEffect actorEffect)
    {
        ActorEffect = actorEffect;
    }
}
