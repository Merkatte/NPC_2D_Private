using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BaseNPCActionSelector : MonoBehaviour
{
    [SerializeField] protected DataManager dataManager;
    [SerializeField] protected ActionPool actionPool;

    protected virtual void Start()
    {
        
    }

    protected virtual IAction GetAction(ActionType type)
    {
        return actionPool.GetAction(type);
    }

    public virtual void ReturnAction(IAction action)
    {
        actionPool.ReturnAction(action);
    }

    public virtual Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        return null;
    }

    /// <summary>
    /// Whether this selector can operate on the given stat's runtime capabilities.
    /// Called at NPC creation time so a role selector can never be paired with an
    /// incompatible stat definition.
    /// </summary>
    public virtual bool CanUseStat(NPCStat stat)
    {
        return stat != null;
    }

    /// <summary>
    /// Rents one action of the given type, initializes it, and appends it to the caller's
    /// in-progress rental list. Callers must roll back via ReturnAll on any failure so a
    /// partially built queue never leaks rented actions back to the pool.
    /// </summary>
    protected bool TryRentAction(ActionType type, ActionContext context, List<IAction> rented)
    {
        IAction action = GetAction(type);
        if (action == null)
        {
            Debug.LogError($"ActionPool could not provide {type}");
            return false;
        }

        action.Init(context);
        rented.Add(action);
        return true;
    }

    protected void ReturnAll(List<IAction> rented)
    {
        for (int i = 0; i < rented.Count; ++i)
            ReturnAction(rented[i]);
    }

    protected Queue<IAction> BuildFallbackIdleQueue(NPCComponent component, NPCStat stat)
    {
        var rented = new List<IAction>();
        if (!TryRentAction(ActionType.Idle, new ActionContext(component, stat), rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }
        return new Queue<IAction>(rented);
    }

    protected Queue<IAction> BuildLeisureQueue(NPCDecision decision, DestinationDecider decider,
        NPCComponent component, NPCStat stat, MoveMode moveMode, TilemapNavigation navigation,
        WanderActionCost wanderCost, IRandomSource random)
    {
        if (decision.Intent == NPCIntent.Eat || decision.Intent == NPCIntent.Drink || decision.Intent == NPCIntent.Sleep)
        {
            if (!decision.HasLiveDestination ||
                (moveMode == MoveMode.Navigation && (!navigation || !navigation.IsReady)))
                return BuildFallbackIdleQueue(component, stat);
            ActionType type = decision.Intent == NPCIntent.Eat ? ActionType.Eat :
                decision.Intent == NPCIntent.Drink ? ActionType.Drink : ActionType.Sleep;
            if (type != ActionType.Sleep && decision.Provider == null)
                return BuildFallbackIdleQueue(component, stat);
            MoveRequest request = moveMode == MoveMode.Navigation
                ? MoveRequest.Navigated(decision.DestinationPos) : MoveRequest.Fixed(decision.DestinationPos);
            var movement = new ActionContext(component, stat, decision.DestinationPos,
                moveRequest: request, navigation: navigation);
            var interaction = new ActionContext(component, stat, decision.DestinationPos,
                provider: decision.Provider, request: decision.Request);
            var rented = new List<IAction>();
            if (!TryRentAction(ActionType.Move, movement, rented) || !TryRentAction(type, interaction, rented))
            {
                ReturnAll(rented);
                return BuildFallbackIdleQueue(component, stat);
            }
            return new Queue<IAction>(rented);
        }

        // Unresolved critical needs wait; safe leisure uses the existing bounded Wander action.
        if (decider == null || decider.HasCriticalNeed(stat) || !navigation || !navigation.IsReady || !wanderCost || random == null ||
            !navigation.TryGetRandomReachablePosition(component.Position, random, out Vector3 target))
            return BuildFallbackIdleQueue(component, stat);

        var context = new ActionContext(component, stat, target, wanderCost,
            moveRequest: MoveRequest.Navigated(target), navigation: navigation);
        var wanderActions = new List<IAction>();
        if (!TryRentAction(ActionType.Wander, context, wanderActions))
        {
            ReturnAll(wanderActions);
            return BuildFallbackIdleQueue(component, stat);
        }
        return new Queue<IAction>(wanderActions);
    }
}
