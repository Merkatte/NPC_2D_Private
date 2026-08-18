using UnityEngine;

public class GuardAction : DefaultAction
{
    private Vector3 _center;
    private int _patrolIndex;

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

        _center = actionContext.Destination ?? actionContext.Component.Position;
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
        if (component.GuardPerception && component.GuardPerception.HasCandidate)
        {
            RequestReplan();
            return;
        }

        if (cost.ShouldInterrupt(stat))
        {
            RequestReplan();
            return;
        }

        TickPatrol(component, cost);
    }

    private static void ApplyNeedDecay(NPCStat stat, GuardActionCost cost)
    {
        stat.ChangeHunger(cost.HungerPerSecond * Time.deltaTime);
        stat.ChangeThirst(cost.ThirstPerSecond * Time.deltaTime);
        stat.ChangeFatigue(cost.FatiguePerSecond * Time.deltaTime);
    }

    private void TickPatrol(NPCComponent component, GuardActionCost cost)
    {
        Vector3 target = GetPatrolPoint(cost);
        Vector3 toTarget = target - component.Position;
        toTarget.z = 0f;

        if (toTarget.sqrMagnitude <= cost.PatrolArrivalDistance * cost.PatrolArrivalDistance)
        {
            _patrolIndex = (_patrolIndex + 1) % Mathf.Max(1, cost.PatrolPointCount);
            return;
        }

        component.Flip(toTarget.x <= 0f);
        component.Move(toTarget.normalized);
    }

    private Vector3 GetPatrolPoint(GuardActionCost cost)
    {
        int count = Mathf.Max(1, cost.PatrolPointCount);
        float angle = _patrolIndex * (Mathf.PI * 2f / count);
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * cost.GuardRadius;
        return _center + offset;
    }

    public override void Clear()
    {
        _center = Vector3.zero;
        _patrolIndex = 0;
        base.Clear();
    }
}
