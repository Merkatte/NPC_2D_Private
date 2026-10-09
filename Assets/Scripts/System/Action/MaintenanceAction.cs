using UnityEngine;

public sealed class MaintenanceAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private MaintenanceLease _lease;
    private DefenseMaintenanceSite _site;
    private BuildActionCost _cost;
    private bool _arrived;
    public MaintenanceAction() : base(ActionType.Maintain) { }
    public override void Init(ActionContext context)
    { base.Init(context); _lease = context.Request?.Reservation as MaintenanceLease; _site = context.InteractionProvider as DefenseMaintenanceSite; _cost = context.CostInfo as BuildActionCost; }
    public override void Start()
    {
        base.Start(); if (IsFinished) return;
        if (_lease == null || !_site || !_cost || !actionContext.MoveRequest.HasValue)
        { Fail("Maintenance requires site, lease, work cost and movement."); return; }
        _follower.Begin(actionContext.Component, actionContext.Stat, actionContext.MoveRequest.Value, actionContext.Navigation);
    }
    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || Time.deltaTime <= 0f || ReplanIfWorkUnavailable()) return;
        if (_lease != null && _lease.IsCompleted) { Complete(); return; }
        if (_lease == null || !_lease.IsValid || !_site || _cost.ShouldInterrupt(actionContext.Stat)) { RequestReplan(); return; }
        _cost.ApplyElapsedNeeds(actionContext.Stat, Time.deltaTime);
        if (!_arrived)
        {
            _follower.Tick(Time.deltaTime);
            if (_follower.RequiresReplan) { RequestReplan(); return; }
            if (!_follower.HasArrived) return;
            _arrived = true; actionContext.Component.SetWorking(true);
        }
        if (!_site.TryInteract(new InteractionRequest(ActionType.Maintain, strength: Time.deltaTime, reservation: _lease), out _)) RequestReplan();
        else if (_lease.IsCompleted) Complete();
    }
    private void Cleanup()
    { _follower.Clear(); _lease?.Dispose(); if (actionContext.Component) actionContext.Component.SetWorking(false); _arrived = false; }
    protected override void Complete() { Cleanup(); base.Complete(); }
    protected override void RequestReplan() { Cleanup(); base.RequestReplan(); }
    protected override void Fail(string reason) { Cleanup(); base.Fail(reason); }
    public override void Stop() { Cleanup(); base.Stop(); }
    public override void Clear() { Cleanup(); _lease = null; _site = null; _cost = null; base.Clear(); }
}
