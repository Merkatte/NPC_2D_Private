using System.Collections.Generic;
using UnityEngine;

public class FarmerActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;

    private DestinationDecider _decider;

    protected override void Start()
    {
        _decider = new DestinationDecider();
        _decider.Init(_destinationDB);
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
        Queue<IAction> queue = new Queue<IAction>();

        if (_decider == null || stat == null || !component)
            return queue;

        NPCDecision decision = _decider.Decide(stat, npcType, component.Position);

        foreach (NPCDecisionStep step in decision.Steps)
        {
            ActionContext actionContext;
            if (step.Intent == NPCIntent.Work)
            {
                bool getSuccess = dataManager.TryGetActionCostInfo<FarmingActionCost>(ActionType.Farming, out var info);
                if (!getSuccess)
                {
                    Debug.LogError("Farming action cost not found");
                    return queue;
                }
                actionContext = new ActionContext(component, stat, step.DestinationPos, info);
            }
            else
            {
                _destinationDB.TryGetInteractionProvider(step.DestinationKey, out var provider);
                actionContext = new ActionContext(component, stat, step.DestinationPos, provider: provider);
            }

            if (!TryEnqueueMove(queue, actionContext))
                continue;

            ActionType actionType = ToActionType(step.Intent);
            for (int i = 0; i < step.RepeatCount; ++i)
            {
                IAction action = GetAction(actionType);
                
                action.Init(actionContext);
                queue.Enqueue(action);
            }
        }

        return queue;
    }
    

    private bool TryEnqueueMove(Queue<IAction> queue, ActionContext context)
    {
        MoveAction moveAction = GetAction(ActionType.Move) as MoveAction;
        if (moveAction == null)
        {
            Debug.LogError("MoveAction not found in ActionPool");
            return false;
        }

        moveAction.Init(context);
        queue.Enqueue(moveAction);
        return true;
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
                return ActionType.Move;
        }
    }
}
