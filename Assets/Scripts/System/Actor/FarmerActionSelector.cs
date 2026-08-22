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

        NPCDecision decision;
        if (_decider == null || _decisionTuning == null || stat == null)
        {
            Debug.LogError("FarmerActionSelector is missing required setup (decider/tuning/stat); falling back to Idle.");
            decision = NPCDecision.Idle(component.Position);
        }
        else
        {
            decision = _decider.Decide(stat, npcType, component.Position, _workCost);
        }

        IInteractionProvider farmProvider = null;
        if (decision.Intent == NPCIntent.Work)
        {
            bool hasProvider = _destinationDB.TryGetInteractionProvider(decision.DestinationKey, ActionType.Farming, out farmProvider);
            if (!hasProvider)
            {
                if (!_hasLoggedMissingFarmProvider)
                {
                    Debug.LogError($"FarmerActionSelector: no usable IInteractionProvider for {decision.DestinationKey}/{ActionType.Farming}; falling back to Idle.");
                    _hasLoggedMissingFarmProvider = true;
                }

                decision = NPCDecision.Idle(component.Position);
                farmProvider = null;
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

    private ActionContext BuildContext(NPCDecision decision, NPCComponent component, NPCStat stat, IInteractionProvider farmProvider)
    {
        switch (decision.Intent)
        {
            case NPCIntent.Work:
            {
                // 1f is a seam for a future Farmer skill/proficiency system.
                InteractionRequest request = new InteractionRequest(ActionType.Farming, strength: 1f);
                return new ActionContext(component, stat, decision.DestinationPos, _farmingActionCostInfo, provider: farmProvider, request: request);
            }

            case NPCIntent.Eat:
            case NPCIntent.Drink:
            {
                ActionType actionType = decision.Request.HasValue ? decision.Request.Value.Type : ToActionType(decision.Intent);
                _destinationDB.TryGetInteractionProvider(decision.DestinationKey, actionType, out var provider);
                return new ActionContext(component, stat, decision.DestinationPos, provider: provider, request: decision.Request);
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
