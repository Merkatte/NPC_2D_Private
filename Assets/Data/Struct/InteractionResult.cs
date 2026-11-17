public readonly struct InteractionResult
{
    public StatEffect ActorEffect { get; }
    public bool HasActorEffect => ActorEffect != null;

    public WorkContributionKind ContributionKind { get; }
    public float ContributionAmount { get; }

    public InteractionResult(StatEffect actorEffect, WorkContributionKind contributionKind = WorkContributionKind.None, float contributionAmount = 0f)
    {
        ActorEffect = actorEffect;
        ContributionKind = contributionKind;
        ContributionAmount = contributionAmount > 0f && !float.IsInfinity(contributionAmount) ? contributionAmount : 0f;
    }
}
