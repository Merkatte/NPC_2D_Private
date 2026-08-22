using System.Collections.Generic;
using UnityEngine;

public class GuardActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;

    private DestinationDecider _decider;
    private GuardActionCost _guardActionCostInfo;

    // Cost of one Guard duty evaluation slice. Derived from shared tuning and the shared cost
    // asset, so it is not per-NPC state and is safe to cache on this shared selector instance.
    private StatEffect _guardDutyCost;
    private readonly List<(ICombatTarget Target, Component Owner)> _candidateBuffer = new List<(ICombatTarget, Component)>();

    protected override void Start()
    {
        _decider = new DestinationDecider();
        _decider.Init(_destinationDB, _decisionTuning);

        if (dataManager.TryGetActionCostInfo<GuardActionCost>(ActionType.Guard, out var guardCost))
        {
            _guardActionCostInfo = guardCost;
        }
        else
        {
            Debug.LogError("GuardActionCost not found; Guard will be unable to patrol.");
        }

        BuildGuardDutyCost();
        WarnOnInterruptThresholdInversion();
    }

    /// <summary>
    /// Projects Guard's authoritative per-second need growth onto one duty evaluation slice.
    /// The slice length is a planning approximation only: GuardAction does not complete or
    /// replan on that interval, it runs until enemy detection or ShouldInterrupt(...) fires.
    /// </summary>
    private void BuildGuardDutyCost()
    {
        if (_guardActionCostInfo == null || _decisionTuning == null)
            return;

        float seconds = _decisionTuning.GuardDutyEvaluationSeconds;

        _guardDutyCost = new StatEffect(
            hungerDelta: _guardActionCostInfo.HungerPerSecond * seconds,
            thirstDelta: _guardActionCostInfo.ThirstPerSecond * seconds,
            fatigueDelta: _guardActionCostInfo.FatiguePerSecond * seconds);
    }

    /// <summary>
    /// Guard's own interrupt thresholds must sit at or above the decider's critical threshold.
    /// If one drops below it, ShouldInterrupt(...) can be true while the critical gate is still
    /// closed, so Guard duty stays a valid, possibly winning candidate and the Guard ends up
    /// repeating the timed Idle fallback instead of patrolling.
    /// </summary>
    private void WarnOnInterruptThresholdInversion()
    {
        if (_guardActionCostInfo == null || _decisionTuning == null)
            return;

        float critical = _decisionTuning.CriticalNeedThreshold;
        if (_guardActionCostInfo.HungerInterruptThreshold >= critical &&
            _guardActionCostInfo.ThirstInterruptThreshold >= critical &&
            _guardActionCostInfo.FatigueInterruptThreshold >= critical)
        {
            return;
        }

        Debug.LogWarning(
            $"GuardActionCost interrupt threshold is below NPCDecisionTuning.CriticalNeedThreshold ({critical}); " +
            "Guard can end up repeating the timed Idle fallback instead of patrolling.");
    }

    public override bool CanUseStat(NPCStat stat)
    {
        return stat is IGuardStatView;
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        if (!component)
            return new Queue<IAction>();

        if (_decider == null || _decisionTuning == null || _destinationDB == null || stat == null || _guardActionCostInfo == null)
        {
            Debug.LogError("GuardActionSelector is missing required setup (decider/tuning/destinationDB/stat/GuardActionCost); falling back to Idle.");
            return BuildIdleQueue(component, stat);
        }

        if (!(stat is IGuardStatView guardStat))
        {
            Debug.LogError("Guard selector requires a stat implementing IGuardStatView; falling back to Idle.");
            return BuildIdleQueue(component, stat);
        }

        if (TryBuildCombatQueue(component, stat, guardStat, out Queue<IAction> combatQueue))
            return combatQueue;

        // The decider is now consulted on every replan, not only above the interrupt threshold,
        // so a Guard that just ate re-decides from its real stats and its real position.
        // workCost units are role-dependent: for Guard this is one duty EVALUATION slice
        // (a planning approximation), not the per-action cost that Farmer passes.
        NPCDecision decision = _decider.Decide(stat, npcType, component.Position, _guardDutyCost);

        if (IsSupplyIntent(decision.Intent))
            return BuildNeedQueue(decision, component, stat);

        // A non-supply result does not prove there was no usable supply candidate: the internal
        // Guard duty candidate and the plain Idle candidate both surface as NPCDecision.Idle, so
        // supply may simply have scored lower. Handing back the patrol queue here would make
        // GuardAction request another replan immediately, every frame.
        if (_guardActionCostInfo.ShouldInterrupt(stat))
        {
            Debug.LogWarning("Guard interrupt remains active, but the decider selected no supply action; using timed Idle to avoid a replan loop.");
            return BuildIdleQueue(component, stat);
        }

        return BuildGuardQueue(component, stat, guardStat);
    }

    private bool TryBuildCombatQueue(NPCComponent component, NPCStat stat, IGuardStatView guardStat, out Queue<IAction> queue)
    {
        queue = null;
        GuardRuntimeState runtimeState = component.GuardRuntimeState;

        if (!runtimeState.HasValidTarget && !TryAcquireNearestTarget(component, runtimeState))
        {
            return false;
        }

        queue = BuildCombatQueue(component, stat, guardStat, runtimeState);
        return true;
    }

    private bool TryAcquireNearestTarget(NPCComponent component, GuardRuntimeState runtimeState)
    {
        GuardPerception perception = component.GuardPerception;
        if (!perception || !perception.HasCandidate)
            return false;

        perception.CopyCandidatesTo(_candidateBuffer);

        ICombatTarget bestTarget = null;
        Component bestOwner = null;
        float bestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < _candidateBuffer.Count; ++i)
        {
            (ICombatTarget target, Component owner) = _candidateBuffer[i];
            if (!CombatTargetHandle.IsValidPair(target, owner))
                continue;

            float sqrDistance = (target.Position - component.Position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestTarget = target;
                bestOwner = owner;
            }
        }

        if (bestTarget == null)
            return false;

        runtimeState.SetTarget(bestTarget, bestOwner);
        return true;
    }

    private Queue<IAction> BuildCombatQueue(NPCComponent component, NPCStat stat, IGuardStatView guardStat, GuardRuntimeState runtimeState)
    {
        List<IAction> rented = new List<IAction>();
        CombatTargetHandle handle = runtimeState.TargetHandle;

        if (!CombatRange.IsInRange(component.Position, handle.Target.Position, guardStat.AttackRange))
        {
            float stoppingDistance = guardStat.AttackRange * _guardActionCostInfo.AttackStoppingDistanceRatio;
            ActionContext moveContext = new ActionContext(component, stat, moveRequest: MoveRequest.Dynamic(handle, stoppingDistance));

            if (!TryRentAction(ActionType.Move, moveContext, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        ActionContext attackContext = new ActionContext(component, stat);
        if (!TryRentAction(ActionType.Attack, attackContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private static bool IsSupplyIntent(NPCIntent intent)
    {
        return intent == NPCIntent.Eat || intent == NPCIntent.Drink || intent == NPCIntent.Sleep;
    }

    private Queue<IAction> BuildNeedQueue(NPCDecision decision, NPCComponent component, NPCStat stat)
    {
        List<IAction> rented = new List<IAction>();

        ActionContext moveContext = new ActionContext(component, stat, decision.DestinationPos);
        if (!TryRentAction(ActionType.Move, moveContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        ActionType actionType = ToActionType(decision.Intent);
        _destinationDB.TryGetInteractionProvider(decision.DestinationKey, actionType, out var provider);
        ActionContext interactContext = new ActionContext(component, stat, decision.DestinationPos, provider: provider, request: decision.Request);

        if (!TryRentAction(actionType, interactContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildGuardQueue(NPCComponent component, NPCStat stat, IGuardStatView guardStat)
    {
        if (!_destinationDB.TryGetDestinationPos(BuildingType.GuardPost, out Vector3 guardPos))
        {
            Debug.LogError("GuardPost destination not found; Guard cannot patrol.");
            return BuildIdleQueue(component, stat);
        }

        List<IAction> rented = new List<IAction>();
        ActionContext context = new ActionContext(component, stat, guardPos, _guardActionCostInfo);

        Vector3 toPost = guardPos - component.Position;
        toPost.z = 0f;
        if (toPost.sqrMagnitude > guardStat.GuardRadius * guardStat.GuardRadius)
        {
            if (!TryRentAction(ActionType.Move, context, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        if (!TryRentAction(ActionType.Guard, context, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildIdleQueue(NPCComponent component, NPCStat stat)
    {
        List<IAction> rented = new List<IAction>();
        ActionContext context = new ActionContext(component, stat);

        if (!TryRentAction(ActionType.Idle, context, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private static ActionType ToActionType(NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Drink:
                return ActionType.Drink;
            case NPCIntent.Eat:
                return ActionType.Eat;
            case NPCIntent.Sleep:
                return ActionType.Sleep;
            default:
                return ActionType.Idle;
        }
    }
}
