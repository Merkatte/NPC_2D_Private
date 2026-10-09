using UnityEngine;

public sealed class DefenseStationAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private DefenseActor _actor;
    private Vector3 _destination;
    private bool _isClimbing;
    private float _idleElapsed;
    private const float StationReassessmentSeconds = 0.5f;
    public DefenseStationAction() : base(ActionType.DefenseStation) { }
    public void Init(ActionContext context, DefenseActor actor) { base.Init(context); _actor = actor; }
    public override void Start()
    {
        base.Start(); if (IsFinished) return;
        if (!_actor) { Fail("Station requires its assigned actor."); return; }
        _destination = _actor.DutyPosition;
        _isClimbing = _actor.Role == NPCType.Archer && _actor.ArcherWall &&
            ((Vector2)(_actor.Position - _actor.ArcherWall.TopPosition)).sqrMagnitude < 0.09f;
        Vector3 approach = _actor.Role == NPCType.Archer && _actor.ArcherWall ? _actor.ArcherWall.GroundPosition : _destination;
        _follower.Begin(actionContext.Component, actionContext.Stat,
            _isClimbing ? MoveRequest.Fixed(_destination) : MoveRequest.Navigated(approach), _actor.Navigation);
    }
    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || Time.deltaTime <= 0f) return;
        if (actionContext.CostInfo is GuardActionCost cost)
        {
            actionContext.Stat.ChangeHunger(cost.HungerPerSecond * Time.deltaTime);
            actionContext.Stat.ChangeThirst(cost.ThirstPerSecond * Time.deltaTime);
            actionContext.Stat.ChangeFatigue(cost.FatiguePerSecond * Time.deltaTime);
            if (actionContext.Stat.IsOnStrike || cost.ShouldInterrupt(actionContext.Stat)) { RequestReplan(); return; }
        }
        if (!_actor || !_actor.CanAct || ((Vector2)(_actor.DutyPosition - _destination)).sqrMagnitude > 0.01f)
        { RequestReplan(); return; }
        _follower.Tick(Time.deltaTime);
        if (_follower.RequiresReplan) { RequestReplan(); return; }
        if (!_follower.HasArrived) return;
        if (_actor.Role == NPCType.Archer && !_isClimbing && ((Vector2)(_actor.Position - _destination)).sqrMagnitude > 0.01f)
        {
            _isClimbing = true;
            // Assigned wall access, not combat pursuit. The ground anchor is navigated first.
            _follower.Begin(actionContext.Component, actionContext.Stat, MoveRequest.Fixed(_destination), null);
            return;
        }
        _idleElapsed += Time.deltaTime;
        if (_idleElapsed >= StationReassessmentSeconds) Complete();
    }
    public override void Stop() { _follower.Clear(); base.Stop(); }
    public override void Clear()
    { _follower.Clear(); _actor = null; _destination = default; _isClimbing = false; _idleElapsed = 0f; base.Clear(); }
}
