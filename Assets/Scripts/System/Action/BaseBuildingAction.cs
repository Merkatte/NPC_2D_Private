public abstract class BaseBuildingAction : DefaultAction
{
    private bool _isInsideBuilding;

    protected BaseBuildingAction(ActionType actionType) : base(actionType) { }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
        {
            return;
        }

        actionContext.Component.SetInsideBuilding(true);
        _isInsideBuilding = true;
    }

    public override void Stop()
    {
        ExitBuilding();
        base.Stop();
    }

    public override void Clear()
    {
        ExitBuilding();
        base.Clear();
    }

    protected override void Complete()
    {
        ExitBuilding();
        base.Complete();
    }

    protected override void RequestReplan()
    {
        ExitBuilding();
        base.RequestReplan();
    }

    protected override void Fail(string reason)
    {
        ExitBuilding();
        base.Fail(reason);
    }

    private void ExitBuilding()
    {
        if (!_isInsideBuilding)
        {
            return;
        }

        if (actionContext.Component)
        {
            actionContext.Component.SetInsideBuilding(false);
        }

        _isInsideBuilding = false;
    }
}
