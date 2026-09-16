internal readonly struct HarnessToolResult
{
    private HarnessToolResult(HarnessRunState state, string message)
    {
        State = state;
        Message = message;
    }

    public HarnessRunState State { get; }
    public string Message { get; }
    public bool IsFailure => State == HarnessRunState.ValidationFailed || State == HarnessRunState.Failed;

    public static HarnessToolResult Success(string message)
    {
        return new HarnessToolResult(HarnessRunState.Succeeded, message);
    }

    public static HarnessToolResult NoChange(string message)
    {
        return new HarnessToolResult(HarnessRunState.NoChange, message);
    }

    public static HarnessToolResult AwaitingCompilation(string message)
    {
        return new HarnessToolResult(HarnessRunState.AwaitingCompilation, message);
    }

    public static HarnessToolResult ValidationFailure(string message)
    {
        return new HarnessToolResult(HarnessRunState.ValidationFailed, message);
    }

    public static HarnessToolResult Failure(string message)
    {
        return new HarnessToolResult(HarnessRunState.Failed, message);
    }
}
