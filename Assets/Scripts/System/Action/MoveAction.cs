using UnityEngine;

public sealed class MoveAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();

    public MoveAction() : base(ActionType.Move) { }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
            return;
        if (!actionContext.MoveRequest.HasValue)
        {
            Fail("MoveAction requires an explicit MoveRequest.");
            return;
        }
        _follower.Begin(actionContext.Component, actionContext.Stat,
            actionContext.MoveRequest.Value, actionContext.Navigation);
        UpdateCompletion();
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || ReplanIfWorkUnavailable())
            return;
        _follower.Tick(Time.deltaTime);
        UpdateCompletion();
    }

    public override void Stop()
    {
        _follower.Clear();
        base.Stop();
    }

    public override void Clear()
    {
        _follower.Clear();
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        if (_follower.RequiresReplan)
            RequestReplan();
        else if (_follower.HasArrived)
            Complete();
    }
}
