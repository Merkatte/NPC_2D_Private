using UnityEngine;

// Executes one bounded wander leg and a rest. The selector chooses the next activity.
public sealed class WanderAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private WanderActionCost _cost;
    private float _moveElapsed;
    private float _restElapsed;
    private bool _isResting;

    public WanderAction() : base(ActionType.Wander) { }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
            return;
        _cost = actionContext.CostInfo as WanderActionCost;
        if (!_cost || actionContext.Stat == null || !actionContext.MoveRequest.HasValue)
        {
            Fail("WanderAction requires stat, WanderActionCost and a navigation request.");
            return;
        }
        _follower.Begin(actionContext.Component, actionContext.Stat,
            actionContext.MoveRequest.Value, actionContext.Navigation);
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
            return;
        if (!actionContext.Component || !_cost || actionContext.Stat == null)
        {
            RequestReplan();
            return;
        }

        float deltaTime = Time.deltaTime;
        _cost.ApplyElapsedNeeds(actionContext.Stat, deltaTime);
        if (_isResting)
        {
            _restElapsed += deltaTime;
            if (_restElapsed >= _cost.RestSeconds)
                Complete();
            return;
        }

        _follower.Tick(deltaTime);
        if (_follower.RequiresReplan)
        {
            RequestReplan();
            return;
        }
        _moveElapsed += deltaTime;
        if (_follower.HasArrived || _moveElapsed >= _cost.MoveSeconds)
        {
            _isResting = true;
            _follower.Clear();
        }
    }

    public override void Stop()
    {
        _follower.Clear();
        base.Stop();
    }

    public override void Clear()
    {
        _follower.Clear();
        _cost = null;
        _moveElapsed = 0f;
        _restElapsed = 0f;
        _isResting = false;
        base.Clear();
    }
}
