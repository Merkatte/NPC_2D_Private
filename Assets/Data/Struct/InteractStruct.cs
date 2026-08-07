public readonly struct InteractRequest
{
    public ActionType Type { get; }
    public WorkerNPC Worker { get; }

    public InteractRequest(ActionType type, WorkerNPC worker)
    {
        Type = type;
        Worker = worker;
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
