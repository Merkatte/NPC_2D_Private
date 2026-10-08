using UnityEngine;

public sealed class HomeStayAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private ResidentHousingState _resident;
    private House _home;
    private bool _isInside;

    public HomeStayAction() : base(ActionType.HomeStay) { }

    public void Init(ActionContext context, ResidentHousingState resident)
    {
        base.Init(context);
        _resident = resident;
        _home = context.InteractionProvider as House;
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished) return;
        if (_resident == null || actionContext.Stat == null || !_home || !actionContext.MoveRequest.HasValue)
        { Fail("HomeStay requires residence, stat, house and movement request."); return; }
        _follower.Begin(actionContext.Component, actionContext.Stat, actionContext.MoveRequest.Value, actionContext.Navigation);
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished) return;
        if (!_home || !_home.CanInteract(ActionType.HomeStay) || !_resident.IsRegistered || _resident.Home != _home)
        { RequestReplan(); return; }
        float seconds = Time.deltaTime;
        if (!_isInside)
        {
            _follower.Tick(seconds);
            if (_follower.RequiresReplan) { RequestReplan(); return; }
            if (!_follower.HasArrived) return;
            _follower.Clear();
            if (!_resident.TryEnterHome(_home)) { RequestReplan(); return; }
            _isInside = true;
            actionContext.Component.SetInsideBuilding(true);
        }
        HousingLifeSettings life = _resident.LifeSettings;
        actionContext.Stat.ChangeFatigue(-(life.HomeFatigueRecoveryPerSecond + _home.Tier.GetEffect(HousingEffectType.FatigueRecovery)) * seconds);
        actionContext.Stat.ChangeHunger((life.HomeHungerPerSecond - _home.Tier.GetEffect(HousingEffectType.HungerRecovery)) * seconds);
        actionContext.Stat.ChangeThirst((life.HomeThirstPerSecond - _home.Tier.GetEffect(HousingEffectType.ThirstRecovery)) * seconds);
    }

    private void Cleanup()
    {
        _follower.Clear();
        if (_isInside && actionContext.Component) actionContext.Component.SetInsideBuilding(false);
        _isInside = false;
        _resident?.ExitHome();
    }
    protected override void RequestReplan() { Cleanup(); base.RequestReplan(); }
    protected override void Fail(string reason) { Cleanup(); base.Fail(reason); }
    public override void Stop() { Cleanup(); base.Stop(); }
    public override void Clear() { Cleanup(); _resident = null; _home = null; base.Clear(); }
}
