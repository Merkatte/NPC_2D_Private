using UnityEngine;

public sealed class FleeAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    public FleeAction() : base(ActionType.Flee) { }
    public override void Start()
    {
        base.Start();
        if (IsFinished) return;
        if (!actionContext.MoveRequest.HasValue) { Fail("Flee requires a selected reachable destination."); return; }
        _follower.Begin(actionContext.Component, actionContext.Stat, actionContext.MoveRequest.Value, actionContext.Navigation);
    }
    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || Time.deltaTime <= 0f) return;
        _follower.Tick(Time.deltaTime);
        if (_follower.RequiresReplan) RequestReplan();
        else if (_follower.HasArrived) Complete();
    }
    public override void Stop() { _follower.Clear(); base.Stop(); }
    public override void Clear() { _follower.Clear(); base.Clear(); }
}
