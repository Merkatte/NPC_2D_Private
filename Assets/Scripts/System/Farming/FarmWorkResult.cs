public readonly struct FarmWorkResult
{
    public bool Success { get; }
    public FarmWorkPhase PreviousPhase { get; }
    public FarmWorkPhase CurrentPhase { get; }
    public float PreviousProgress { get; }
    public float CurrentProgress { get; }
    public int ProducedItemId { get; }
    public int ProducedQuantity { get; }

    public bool PhaseChanged => PreviousPhase != CurrentPhase;
    public bool ProducedAnything => ProducedQuantity > 0;

    public FarmWorkResult(bool success, FarmWorkPhase previousPhase, FarmWorkPhase currentPhase,
        float previousProgress, float currentProgress, int producedItemId, int producedQuantity)
    {
        Success = success;
        PreviousPhase = previousPhase;
        CurrentPhase = currentPhase;
        PreviousProgress = previousProgress;
        CurrentProgress = currentProgress;
        ProducedItemId = producedItemId;
        ProducedQuantity = producedQuantity;
    }

    public static FarmWorkResult Failed(FarmWorkPhase phase, float progress)
    {
        return new FarmWorkResult(false, phase, phase, progress, progress, -1, 0);
    }
}
