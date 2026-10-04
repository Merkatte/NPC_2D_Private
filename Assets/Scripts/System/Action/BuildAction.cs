using UnityEngine;

public sealed class BuildAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private ConstructionReservation _reservation;
    private BuildActionCost _cost;
    private Component _providerOwner;
    private bool _isWorking;
    public BuildAction() : base(ActionType.Build) { }

    public override void Init(ActionContext context)
    {
        base.Init(context);
        _reservation = context.Request?.Reservation as ConstructionReservation;
        _cost = context.CostInfo as BuildActionCost;
        _providerOwner = context.InteractionProvider as Component;
    }
    public override void Start()
    {
        base.Start();
        if (IsFinished) { Cleanup(); return; }
        if (!_cost || !_cost.IsConfigured || actionContext.Stat == null || _reservation == null
            || !_providerOwner || !actionContext.MoveRequest.HasValue)
        { Fail("BuildAction requires work cost, stat, provider, reservation and navigation request."); return; }
        if (!CheckReservation()) return;
        _follower.Begin(actionContext.Component, actionContext.Stat, actionContext.MoveRequest.Value, actionContext.Navigation);
    }
    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || ReplanIfWorkUnavailable()) return;
        if (!actionContext.Component || !_providerOwner || !CheckReservation())
        { if (!IsFinished) RequestReplan(); return; }
        if (_cost.ShouldInterrupt(actionContext.Stat)) { RequestReplan(); return; }
        float seconds = Time.deltaTime;
        _cost.ApplyElapsedNeeds(actionContext.Stat, seconds);
        if (!_isWorking)
        {
            _follower.Tick(seconds);
            if (_follower.RequiresReplan) { RequestReplan(); return; }
            if (!_follower.HasArrived) return;
            _follower.Clear();
            _isWorking = true;
            actionContext.Component.SetWorking(true);
        }
        if (seconds <= 0f) return;
        var request = new InteractionRequest(ActionType.Build, strength: _cost.Work.WorkPerSecond * seconds,
            reservation: _reservation);
        if (!actionContext.InteractionProvider.TryInteract(request, out _))
        { if (!CheckReservation()) return; RequestReplan(); return; }
        CheckReservation();
    }
    private bool CheckReservation()
    {
        if (_reservation != null && _reservation.Status == ConstructionReservationStatus.Completed)
        { Complete(); return false; }
        if (_reservation == null || !_reservation.IsValid)
        { RequestReplan(); return false; }
        return true;
    }
    private void Cleanup()
    {
        _follower.Clear();
        if (actionContext.Component) actionContext.Component.SetWorking(false);
        _isWorking = false;
        _reservation?.Dispose();
    }
    protected override void Complete() { Cleanup(); base.Complete(); }
    protected override void RequestReplan() { Cleanup(); base.RequestReplan(); }
    protected override void Fail(string reason) { Cleanup(); base.Fail(reason); }
    public override void Stop() { Cleanup(); base.Stop(); }
    public override void Clear()
    {
        Cleanup();
        _reservation = null;
        _providerOwner = null;
        _cost = null;
        base.Clear();
    }
}
