using UnityEngine;

public class GuardAction : DefaultAction
{
    private readonly NPCPathFollower _follower = new NPCPathFollower();
    private Vector3 _destination;
    private IInteractionProvider _provider;
    private Component _providerOwner;

    public GuardAction() : base(ActionType.Guard)
    {
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
        {
            return;
        }

        if (!(actionContext.CostInfo is GuardActionCost))
        {
            Fail("GuardAction has no valid GuardActionCost in ActionContext");
            return;
        }

        _provider = actionContext.InteractionProvider;
        _providerOwner = _provider as Component;
        if (!(actionContext.Stat is GuardStat) || !actionContext.Destination.HasValue || !_providerOwner)
        {
            Fail("GuardAction requires GuardStat, destination and a live provider");
            return;
        }
        _destination = actionContext.Destination.Value;
        BeginPatrolLeg();
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || ReplanIfWorkUnavailable())
        {
            return;
        }

        var component = actionContext.Component;
        var stat = actionContext.Stat;
        var cost = actionContext.CostInfo as GuardActionCost;

        if (!component || stat == null || cost == null)
        {
            Fail("GuardAction lost its required references mid-tick");
            return;
        }

        ApplyNeedDecay(stat, cost);

        // Enemy detection takes priority over need interrupts (approved selector priority order).
        if (component.CombatPerception && component.CombatPerception.HasCandidate)
        {
            RequestReplan();
            return;
        }

        if (cost.ShouldInterrupt(stat))
        {
            RequestReplan();
            return;
        }

        if (!_providerOwner || !_provider.CanInteract(ActionType.Guard))
        {
            RequestReplan();
            return;
        }
        TickPatrol();
    }

    private static void ApplyNeedDecay(NPCStat stat, GuardActionCost cost)
    {
        stat.ChangeHunger(cost.HungerPerSecond * Time.deltaTime);
        stat.ChangeThirst(cost.ThirstPerSecond * Time.deltaTime);
        stat.ChangeFatigue(cost.FatiguePerSecond * Time.deltaTime);
    }

    private void TickPatrol()
    {
        _follower.Tick(Time.deltaTime);
        if (_follower.RequiresReplan)
        {
            RequestReplan();
            return;
        }
        if (!_follower.HasArrived)
            return;
        if (!_provider.TryGetActionPosition(ActionType.Guard, _destination, out _destination))
        {
            RequestReplan();
            return;
        }
        BeginPatrolLeg();
    }

    private void BeginPatrolLeg()
    {
        var cost = (GuardActionCost)actionContext.CostInfo;
        bool navigation = actionContext.MoveRequest.HasValue &&
            actionContext.MoveRequest.Value.Mode == MoveMode.Navigation;
        MoveRequest request = navigation
            ? MoveRequest.Navigated(_destination, cost.PatrolArrivalDistance)
            : MoveRequest.Fixed(_destination, cost.PatrolArrivalDistance);
        _follower.Begin(actionContext.Component, actionContext.Stat, request, actionContext.Navigation);
    }

    public override void Stop()
    {
        _follower.Clear();
        base.Stop();
    }

    public override void Clear()
    {
        _follower.Clear();
        _destination = Vector3.zero;
        _provider = null;
        _providerOwner = null;
        base.Clear();
    }
}
