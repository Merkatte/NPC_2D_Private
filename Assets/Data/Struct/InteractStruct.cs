public readonly struct InteractRequest
{
    public ActionType Type { get; }
    public int ItemId { get; }

    public InteractRequest(ActionType type, int itemId)
    {
        Type = type;
        ItemId = itemId;
    }
}

public readonly struct InteractResult
{
    public bool Success { get; }
    public StatEffect Effect { get; }

    public InteractResult(bool success, StatEffect effect)
    {
        Success = success;
        Effect = effect;
    }
}
