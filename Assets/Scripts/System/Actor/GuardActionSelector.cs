using System.Collections.Generic;
using UnityEngine;

public class GuardActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;

    private DestinationDecider _decider;
    private GuardActionCost _guardActionCostInfo;
    private AttackActionCost _attackActionCostInfo;
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

        if (dataManager.TryGetActionCostInfo<AttackActionCost>(ActionType.Attack, out var attackCost))
        {
            _attackActionCostInfo = attackCost;
        }
        else
        {
            Debug.LogError("AttackActionCost not found; Guard will be unable to attack.");
        }
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        if (!component)
            return new Queue<IAction>();

        if (_decider == null || _decisionTuning == null || _destinationDB == null || stat == null
            || _guardActionCostInfo == null || _attackActionCostInfo == null)
        {
            Debug.LogError("GuardActionSelector is missing required setup (decider/tuning/destinationDB/stat/GuardActionCost/AttackActionCost); falling back to Idle.");
            return BuildIdleQueue(component, stat);
        }

        if (TryBuildCombatQueue(component, stat, out Queue<IAction> combatQueue))
            return combatQueue;

        if (_guardActionCostInfo.ShouldInterrupt(stat))
        {
            NPCDecision needDecision = _decider.Decide(stat, npcType, component.Position, workCost: null);
            if (IsSupplyIntent(needDecision.Intent))
                return BuildNeedQueue(needDecision, component, stat);

            Debug.LogWarning("Guard need is above threshold but no need destination is currently available; falling back to patrol.");
        }

        return BuildGuardQueue(component, stat);
    }

    private bool TryBuildCombatQueue(NPCComponent component, NPCStat stat, out Queue<IAction> queue)
    {
        queue = null;
        GuardRuntimeState runtimeState = component.GuardRuntimeState;

        if (!runtimeState.HasValidTarget && !TryAcquireNearestTarget(component, runtimeState))
        {
            return false;
        }

        queue = BuildCombatQueue(component, stat, runtimeState);
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

    private Queue<IAction> BuildCombatQueue(NPCComponent component, NPCStat stat, GuardRuntimeState runtimeState)
    {
        List<IAction> rented = new List<IAction>();
        CombatTargetHandle handle = runtimeState.TargetHandle;

        if (!_attackActionCostInfo.IsInRange(component.Position, handle.Target.Position))
        {
            float stoppingDistance = _attackActionCostInfo.AttackRange * 0.9f;
            ActionContext moveContext = new ActionContext(component, stat, moveRequest: MoveRequest.Dynamic(handle, stoppingDistance));

            if (!TryRentAction(ActionType.Move, moveContext, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        ActionContext attackContext = new ActionContext(component, stat, cost: _attackActionCostInfo);
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

        _destinationDB.TryGetInteractionProvider(decision.DestinationKey, out var provider);
        ActionContext interactContext = new ActionContext(component, stat, decision.DestinationPos, provider: provider, request: decision.Request);

        if (!TryRentAction(ToActionType(decision.Intent), interactContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildGuardQueue(NPCComponent component, NPCStat stat)
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
        if (toPost.sqrMagnitude > _guardActionCostInfo.GuardRadius * _guardActionCostInfo.GuardRadius)
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
