using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BaseNPCActionSelector : MonoBehaviour
{
    [SerializeField] protected DataManager dataManager;
    [SerializeField] protected ActionPool actionPool;


    protected virtual DestinationDecider HousingDecider => null;
    public virtual bool HasAvailableWork(NPCStat stat, NPCComponent component) => false;

    public virtual float GetReassessmentSeconds(ResidentHousingState residence)
        => residence?.LifeSettings.ReassessmentSeconds ?? float.PositiveInfinity;

    public virtual bool ShouldReplan(NPCStat stat, NPCType npcType, NPCComponent component,
        ActionType currentAction, ResidentHousingState residence)
    {
        if (residence == null || !residence.IsRegistered || currentAction != ActionType.HomeStay)
            return false;
        if (!residence.Home || !residence.Home.isActiveAndEnabled) return true;
        if (residence.RequiresFirstHomeVisit) return false;
        NPCDecision need = DecideHomeNeeds(stat, component);
        return IsHousingSupply(need.Intent) || (!stat.IsOnStrike && HasAvailableWork(stat, component));
    }

    public virtual Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType,
        NPCComponent component, ResidentHousingState residence)
    {
        if (residence == null || !residence.IsRegistered)
            return RequestNewActionQueue(stat, npcType, component);
        if (residence.Home && residence.RequiresFirstHomeVisit)
            return BuildHomeQueue(component, stat, residence);
        bool hasWork = !stat.IsOnStrike && HasAvailableWork(stat, component);
        if (residence.Home && !hasWork)
        {
            NPCDecision need = DecideHomeNeeds(stat, component);
            if (IsHousingSupply(need.Intent))
                return BuildHousingNeedQueue(need, component, stat, residence);
            return BuildHomeQueue(component, stat, residence);
        }
        Queue<IAction> queue = RequestResidentialWorkQueue(stat, npcType, component, residence.LifeSettings);
        if (queue != null)
            foreach (IAction action in queue)
                if (action is SleepAction sleep) sleep.ConfigureRecovery(residence.LifeSettings);
        return queue;
    }

    protected virtual Queue<IAction> RequestResidentialWorkQueue(NPCStat stat, NPCType npcType,
        NPCComponent component, HousingLifeSettings life)
        => RequestNewActionQueue(stat, npcType, component);

    private NPCDecision DecideHomeNeeds(NPCStat stat, NPCComponent component)
        => HousingDecider != null && component
            ? HousingDecider.DecideNeeds(stat, component.Position, allowSleep: false)
            : NPCDecision.Idle(component ? component.Position : Vector3.zero);

    private static bool IsHousingSupply(NPCIntent intent)
        => intent == NPCIntent.Eat || intent == NPCIntent.Drink;

    private Queue<IAction> BuildHousingNeedQueue(NPCDecision decision, NPCComponent component,
        NPCStat stat, ResidentHousingState residence)
    {
        if (!decision.HasLiveDestination || decision.Provider == null)
            return BuildHomeQueue(component, stat, residence);
        var rented = new List<IAction>();
        var movement = new ActionContext(component, stat, decision.DestinationPos,
            moveRequest: MoveRequest.Navigated(decision.DestinationPos), navigation: residence.Navigation);
        var interaction = new ActionContext(component, stat, decision.DestinationPos,
            provider: decision.Provider, request: decision.Request);
        ActionType type = decision.Intent == NPCIntent.Eat ? ActionType.Eat : ActionType.Drink;
        if (!TryRentAction(ActionType.Move, movement, rented) || !TryRentAction(type, interaction, rented))
        {
            ReturnAll(rented);
            return BuildFallbackIdleQueue(component, stat);
        }
        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildHomeQueue(NPCComponent component, NPCStat stat, ResidentHousingState residence)
    {
        House house = residence.Home;
        if (!house || !house.CanInteract(ActionType.HomeStay))
            return BuildFallbackIdleQueue(component, stat);
        IAction rental = GetAction(ActionType.HomeStay);
        if (!(rental is HomeStayAction home))
        {
            if (rental != null) ReturnAction(rental);
            return BuildFallbackIdleQueue(component, stat);
        }
        var context = new ActionContext(component, stat, house.EntrancePosition, provider: house,
            moveRequest: MoveRequest.Navigated(house.EntrancePosition), navigation: residence.Navigation);
        home.Init(context, residence);
        var queue = new Queue<IAction>();
        queue.Enqueue(home);
        return queue;
    }

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
