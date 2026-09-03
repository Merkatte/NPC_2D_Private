using System.Collections.Generic;
using UnityEngine;

public class FarmerActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;

    private DestinationDecider _decider;
    private FarmingActionCost _farmingActionCostInfo;
    private StatEffect _workCost;
    private bool _hasLoggedMissingFarmProvider;
    private bool _hasLoggedMissingDepositTarget;

    protected override void Start()
    {
        _decider = new DestinationDecider();
        _decider.Init(_destinationDB, _decisionTuning);

        if (dataManager.TryGetActionCostInfo<FarmingActionCost>(ActionType.Farming, out var farmingCost))
        {
            _farmingActionCostInfo = farmingCost;
            _workCost = new StatEffect(
                hungerDelta: farmingCost.FarmingActionPerHunger,
                thirstDelta: farmingCost.FarmingActionPerThirst,
                fatigueDelta: farmingCost.FarmingActionPerFatigue);
        }
        else
        {
            Debug.LogError("FarmingActionCost not found; DestinationDecider will never offer Work.");
        }
    }

    /// <summary>
    /// Replaces the decider instance, e.g. when a top-level manager starts distributing
    /// one shared DestinationDecider across every job's selector.
    /// </summary>
    public void SetDecider(DestinationDecider decider)
    {
        _decider = decider;
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        if (!component)
            return new Queue<IAction>();

        if (_decider == null || _decisionTuning == null || stat == null)
        {
            Debug.LogError("FarmerActionSelector is missing required setup (decider/tuning/stat); falling back to Idle.");
            return BuildIdleQueue(component, stat);
        }

        // Logistics (deliver/harvest cargo) takes priority over ordinary Work/Eat/Drink/Sleep
        // decisions, but never over a critical need — matches the approved priority order.
        if (!_decider.HasCriticalNeed(stat))
        {
            if (TryBuildHarvestQueue(component, stat, out Queue<IAction> harvestQueue))
                return harvestQueue;

            if (!component.Cargo.IsEmpty)
            {
                if (TryBuildDepositQueue(component, stat, out Queue<IAction> depositQueue))
                    return depositQueue;

                // Cargo exists but there is nowhere to take it — a real configuration problem,
                // not a normal environment change. Warn once and park safely instead of
                // busy-replanning every frame.
                if (!_hasLoggedMissingDepositTarget)
                {
                    Debug.LogError(
                        "FarmerActionSelector: cargo is non-empty but no usable Warehouse Deposit provider; parking Idle.");
                    _hasLoggedMissingDepositTarget = true;
                }

                return BuildIdleQueue(component, stat);
            }
        }

        NPCDecision decision = _decider.Decide(stat, npcType, component.Position, _workCost);

        IInteractionProvider farmProvider = null;
        if (decision.Intent == NPCIntent.Work)
        {
            Vector3 workPosition = decision.DestinationPos;
            bool hasProvider = _destinationDB.TryGetInteractionProvider(decision.DestinationKey, ActionType.Farming, out farmProvider);
            bool hasWorkPosition = hasProvider && farmProvider.TryGetActionPosition(
                ActionType.Farming,
                decision.DestinationPos,
                out workPosition);

            if (!hasWorkPosition)
            {
                // Farm not registered at all in DestinationDB would be a real config error, but it
                // is effectively unreachable here: AddFarmerWorkCandidate already requires
                // TryGetDestinationPos(Farm) to succeed before the decider ever offers Work. What
                // actually reaches this branch is the farm being registered but not currently
                // interactable for Farming (empty crop, or already flipped to Harvesting) — a
                // normal state, so stay silent unless the registration itself is truly missing.
                if (!_destinationDB.TryGetDestinationPos(BuildingType.Farm, out _) && !_hasLoggedMissingFarmProvider)
                {
                    Debug.LogError("FarmerActionSelector: BuildingType.Farm is not registered in DestinationDB; falling back to Idle.");
                    _hasLoggedMissingFarmProvider = true;
                }

                decision = NPCDecision.Idle(component.Position);
                farmProvider = null;
            }
            else
            {
                decision = new NPCDecision(
                    decision.Intent,
                    decision.DestinationKey,
                    workPosition,
                    decision.RepeatCount,
                    decision.Request);
            }
        }

        ActionContext actionContext = BuildContext(decision, component, stat, farmProvider);
        List<IAction> rented = new List<IAction>();

        if (decision.DestinationKey != BuildingType.None)
        {
            if (!TryRentAction(ActionType.Move, actionContext, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        ActionType actionType = ToActionType(decision.Intent);
        int repeatCount = Mathf.Max(1, decision.RepeatCount);
        for (int i = 0; i < repeatCount; ++i)
        {
            if (!TryRentAction(actionType, actionContext, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        return new Queue<IAction>(rented);
    }

    /// <summary>
    /// One Harvest per queue build (not decision.RepeatCount): capacity boundaries are already
    /// handled by replanning after every attempt (HarvestAction RequestReplans the moment cargo
    /// stops accepting more), so there is nothing to gain from renting several at once.
    /// </summary>
    private bool TryBuildHarvestQueue(NPCComponent component, NPCStat stat, out Queue<IAction> queue)
    {
        queue = null;

        if (component.Cargo.IsFull)
            return false;

        if (!_destinationDB.TryGetDestinationPos(BuildingType.Farm, out Vector3 farmPos))
            return false;

        if (!_destinationDB.TryGetInteractionProvider(BuildingType.Farm, ActionType.Harvest, out var provider))
            return false;

        if (!provider.TryGetActionPosition(ActionType.Harvest, farmPos, out Vector3 workPos))
            return false;

        InteractionRequest request = new InteractionRequest(ActionType.Harvest, strength: 1f, cargo: component.Cargo);
        ActionContext context = new ActionContext(component, stat, workPos, _farmingActionCostInfo,
            provider: provider, request: request);

        List<IAction> rented = new List<IAction>();
        if (!TryRentAction(ActionType.Move, context, rented) || !TryRentAction(ActionType.Harvest, context, rented))
        {
            ReturnAll(rented);
            return false;
        }

        queue = new Queue<IAction>(rented);
        return true;
    }

    private bool TryBuildDepositQueue(NPCComponent component, NPCStat stat, out Queue<IAction> queue)
    {
        queue = null;

        if (!_destinationDB.TryGetDestinationPos(BuildingType.Warehouse, out Vector3 warehousePos))
            return false;

        if (!_destinationDB.TryGetInteractionProvider(BuildingType.Warehouse, ActionType.Deposit, out var provider))
            return false;

        if (!provider.TryGetActionPosition(ActionType.Deposit, warehousePos, out Vector3 depositPos))
            return false;

        InteractionRequest request = new InteractionRequest(ActionType.Deposit, cargo: component.Cargo);
        ActionContext context = new ActionContext(component, stat, depositPos, provider: provider, request: request);

        List<IAction> rented = new List<IAction>();
        if (!TryRentAction(ActionType.Move, context, rented) || !TryRentAction(ActionType.Deposit, context, rented))
        {
            ReturnAll(rented);
            return false;
        }

        queue = new Queue<IAction>(rented);
        return true;
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

    private ActionContext BuildContext(NPCDecision decision, NPCComponent component, NPCStat stat, IInteractionProvider farmProvider)
    {
        switch (decision.Intent)
        {
            case NPCIntent.Work:
            {
                // 1f is a seam for a future Farmer skill/proficiency system.
                InteractionRequest request = new InteractionRequest(ActionType.Farming, strength: 1f);
                return new ActionContext(component, stat, decision.DestinationPos, _farmingActionCostInfo,
                    provider: farmProvider, request: request);
            }

            case NPCIntent.Eat:
            case NPCIntent.Drink:
            {
                ActionType actionType = decision.Request.HasValue ? decision.Request.Value.Type : ToActionType(decision.Intent);
                _destinationDB.TryGetInteractionProvider(decision.DestinationKey, actionType, out var provider);
                return new ActionContext(component, stat, decision.DestinationPos, provider: provider,
                    request: decision.Request);
            }

            case NPCIntent.Sleep:
                return new ActionContext(component, stat, decision.DestinationPos);

            default:
                return new ActionContext(component, stat);
        }
    }

    private static ActionType ToActionType(NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Work:
                return ActionType.Farming;
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
