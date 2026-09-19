public readonly struct RecruitmentStatus
{
    public NPCType NpcType { get; }
    public RecruitPhase Phase { get; }
    public float CooldownRemaining { get; }
    public int SettlementCost { get; }
    public bool IsDispatching { get; }
    public bool CanRecruit => Phase == RecruitPhase.CandidateReady && !IsDispatching;

    public RecruitmentStatus(NPCType npcType, RecruitPhase phase, float cooldownRemaining,
        int settlementCost, bool isDispatching)
    {
        NpcType = npcType;
        Phase = phase;
        CooldownRemaining = cooldownRemaining;
        SettlementCost = settlementCost;
        IsDispatching = isDispatching;
    }
}
