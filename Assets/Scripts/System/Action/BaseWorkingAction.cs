public abstract class BaseWorkingAction : DefaultAction
{
    private bool _isWorking;

    protected BaseWorkingAction(ActionType actionType) : base(actionType) { }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
        {
            return;
        }

        actionContext.Component.SetWorking(true);
        _isWorking = true;
    }

    public override void Stop()
    {
        ExitWorking();
        base.Stop();
    }

    public override void Clear()
    {
        ExitWorking();
        base.Clear();
    }

    protected override void Complete()
    {
        ExitWorking();
        base.Complete();
    }

    protected override void RequestReplan()
    {
        ExitWorking();
        base.RequestReplan();
    }

    protected override void Fail(string reason)
    {
        ExitWorking();
        base.Fail(reason);
    }

    /// <summary>
    /// True when the cached provider still reports this action's capability as usable. A tick
    /// re-check lets a mid-work environment change (another Farmer flipping the site's phase)
    /// be noticed within a frame instead of waiting up to the full working duration — this calls
    /// the already-cached IInteractionProvider, not a scene search, so it is hot-path safe.
    /// </summary>
    protected bool ProviderStillUsable()
        => actionContext.InteractionProvider != null
           && actionContext.InteractionProvider.CanInteract(GetMyActionType());

    private void ExitWorking()
    {
        if (!_isWorking)
        {
            return;
        }

        if (actionContext.Component)
        {
            actionContext.Component.SetWorking(false);
        }

        _isWorking = false;
    }
}
