using UnityEngine;

public class GuardAction : DefaultAction
{
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
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
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
        TickPatrol(component, stat, cost);
    }

    private static void ApplyNeedDecay(NPCStat stat, GuardActionCost cost)
    {
        stat.ChangeHunger(cost.HungerPerSecond * Time.deltaTime);
        stat.ChangeThirst(cost.ThirstPerSecond * Time.deltaTime);
        stat.ChangeFatigue(cost.FatiguePerSecond * Time.deltaTime);
    }

    private void TickPatrol(NPCComponent component, NPCStat stat, GuardActionCost cost)
    {
        Vector3 toTarget = _destination - component.Position;
        toTarget.z = 0f;
        if (toTarget.sqrMagnitude <= cost.PatrolArrivalDistance * cost.PatrolArrivalDistance)
        {
            // Repeating the selected duty requests another point from the same facility only.
            if (!_provider.TryGetActionPosition(ActionType.Guard, _destination, out _destination))
                RequestReplan();
            return;
        }

        float step = Mathf.Max(0f, stat.GetMoveSpeed * Time.deltaTime);
        if (step <= 0f)
            return;
        component.Flip(toTarget.x <= 0f);
        component.Move(toTarget.normalized * Mathf.Min(1f, toTarget.magnitude / step));
    }

    public override void Clear()
    {
        _destination = Vector3.zero;
        _provider = null;
        _providerOwner = null;
        base.Clear();
    }
}
