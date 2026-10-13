using UnityEngine;

public sealed class DefenseStationAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private DefenseActor _actor;
    private Vector3 _destination;
    private bool _isAccessLeg;
    private bool _isEmergencyDuty;
    private bool _isGroundExit;
    private float _idleElapsed;
    private const float StationReassessmentSeconds = 0.5f;
    public DefenseStationAction() : base(ActionType.DefenseStation) { }
    public void Init(ActionContext context, DefenseActor actor, bool isEmergencyDuty = false, bool isGroundExit = false)
    {
        Clear();
        base.Init(context);
        _actor = actor;
        _isEmergencyDuty = isEmergencyDuty;
        _isGroundExit = isGroundExit;
    }
    public override void Start()
    {
        base.Start(); if (IsFinished) return;
        if (!_actor) { Fail("Station requires its assigned actor."); return; }
        _actor.RefreshArcherWallOccupancy();
        _actor.SetEmergencyDuty(_isEmergencyDuty);
        _actor.SetGroundExitActive(_isGroundExit);
        if (_isGroundExit && !_actor.ArcherWall)
        { Fail("Ground exit requires the actor's assigned wall access."); return; }
        _destination = _isGroundExit ? _actor.ArcherWall.GroundPosition : _actor.DutyPosition;
        if (_actor.RequiresArcherGroundExit)
        {
            if (_isGroundExit || _actor.IsDescendingArcherAccess || _actor.ArcherWall.IsPassable)
                BeginAccess(towardGround: true);
            else if (_actor.IsUsingArcherAccess)
                BeginAccess(towardGround: false);
            else
                _follower.Begin(actionContext.Component, actionContext.Stat, MoveRequest.Fixed(_destination), null);
            return;
        }
        Vector3 approach = _actor.Role == NPCType.Archer && _actor.ArcherWall ? _actor.ArcherWall.GroundPosition : _destination;
        _follower.Begin(actionContext.Component, actionContext.Stat,
            MoveRequest.Navigated(approach), _actor.Navigation);
    }
    private void BeginAccess(bool towardGround)
    {
        _isAccessLeg = true;
        _actor.BeginArcherAccess(towardGround);
        Vector3 accessDestination = towardGround ? _actor.ArcherWall.GroundPosition : _actor.ArcherWall.TopPosition;
        // Only this actor's wall access leg is direct. All ground travel uses navigation.
        _follower.Begin(actionContext.Component, actionContext.Stat, MoveRequest.Fixed(accessDestination), null);
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
        if (!_actor || !_actor.CanAct || (!_isGroundExit &&
            ((Vector2)(_actor.DutyPosition - _destination)).sqrMagnitude > 0.01f))
        { RequestReplan(); return; }
        _follower.Tick(Time.deltaTime);
        if (_follower.RequiresReplan) { RequestReplan(); return; }
        if (!_follower.HasArrived) return;
        if (_isAccessLeg)
        {
            _actor.CompleteArcherAccess();
            _isAccessLeg = false;
        }
        if (_isGroundExit) { Complete(); return; }
        if (_actor.Role == NPCType.Archer && _actor.ArcherWall && !_actor.ArcherWall.IsPassable && !_actor.IsOnArcherWall)
        {
            BeginAccess(towardGround: false);
            return;
        }
        _idleElapsed += Time.deltaTime;
        if (_idleElapsed >= StationReassessmentSeconds) Complete();
    }
    private void ClearStationActivity()
    {
        if (!_actor) return;
        _actor.SetEmergencyDuty(false);
        _actor.SetGroundExitActive(false);
    }
    protected override void Complete() { ClearStationActivity(); base.Complete(); }
    protected override void RequestReplan() { ClearStationActivity(); base.RequestReplan(); }
    protected override void Fail(string reason) { ClearStationActivity(); base.Fail(reason); }
    public override void Stop() { _follower.Clear(); ClearStationActivity(); base.Stop(); }
    public override void Clear()
    {
        _follower.Clear(); ClearStationActivity(); _actor = null; _destination = default;
        _isAccessLeg = false; _isEmergencyDuty = false; _isGroundExit = false; _idleElapsed = 0f; base.Clear();
    }
}
