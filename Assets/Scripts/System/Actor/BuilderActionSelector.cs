using System.Collections.Generic;
using UnityEngine;

public sealed class BuilderActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;
    [SerializeField] private TilemapNavigation _navigation;
    [SerializeField] private WanderActionCost _wanderCost;
    [SerializeField] private MonoBehaviour _randomSource;
    [SerializeField] private BuildingPlotRegistry _buildingPlots;
    [SerializeField] private BuildActionCost _buildCost;

    private DestinationDecider _decider;
    private IRandomSource _random;
    private bool _hasLoggedConfiguration;

    private void Awake()
    {
        _random = _randomSource as IRandomSource;
        if (_destinationDB && _decisionTuning)
        {
            _decider = new DestinationDecider();
            _decider.Init(_destinationDB, _decisionTuning);
        }
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        if (!component || stat == null || !actionPool)
        {
            LogConfigurationError("NPCComponent, NPCStat and ActionPool");
            return new Queue<IAction>();
        }
        if (_decider == null || !_destinationDB || !_decisionTuning || !_navigation ||
            !_navigation.IsReady || !_wanderCost || !_randomSource || _random == null)
        {
            LogConfigurationError("destination, tuning, ready navigation, wander cost and random source");
            return BuildIdleQueue(component, stat);
        }

        NPCDecision decision = _decider.Decide(stat, NPCType.Builder, component.Position, null);
        var rented = new List<IAction>();
        if (decision.Intent == NPCIntent.Eat || decision.Intent == NPCIntent.Drink || decision.Intent == NPCIntent.Sleep)
        {
            if (!decision.HasLiveDestination) return BuildIdleQueue(component, stat);
            ActionType type = decision.Intent == NPCIntent.Eat ? ActionType.Eat :
                decision.Intent == NPCIntent.Drink ? ActionType.Drink : ActionType.Sleep;
            IInteractionProvider provider = decision.Provider;
            if (type != ActionType.Sleep && provider == null)
                return BuildIdleQueue(component, stat);
            var movement = new ActionContext(component, stat, decision.DestinationPos,
                moveRequest: MoveRequest.Navigated(decision.DestinationPos), navigation: _navigation);
            var interaction = new ActionContext(component, stat, decision.DestinationPos,
                provider: provider, request: decision.Request);
            if (!TryRentAction(ActionType.Move, movement, rented) || !TryRentAction(type, interaction, rented))
            {
                ReturnAll(rented);
                return BuildIdleQueue(component, stat);
            }
            return new Queue<IAction>(rented);
        }

        // A critical unmet need must not be replaced with a role activity.
        if (_decider.HasCriticalNeed(stat))
            return BuildIdleQueue(component, stat);

        if (_buildingPlots && _buildCost && _buildCost.IsConfigured)
        {
            var candidates = new List<BuildingPlot>();
            foreach (BuildingPlot plot in _buildingPlots.Plots)
                if (plot && plot.CanReserve) candidates.Add(plot);
            candidates.Sort((left, right) => left.ApplicationOrder.CompareTo(right.ApplicationOrder));
            foreach (BuildingPlot plot in candidates)
            {
                if (!plot.TryReserve(out ConstructionReservation reservation)) continue;
                var buildContext = new ActionContext(component, stat, reservation.WorkPosition, _buildCost,
                    provider: plot, request: new InteractionRequest(ActionType.Build, reservation: reservation),
                    moveRequest: MoveRequest.Navigated(reservation.WorkPosition), navigation: _navigation);
                if (TryRentAction(ActionType.Build, buildContext, rented)) return new Queue<IAction>(rented);
                reservation.Dispose();
                ReturnAll(rented);
                return BuildIdleQueue(component, stat);
            }
        }
        if (!_navigation.TryGetRandomReachablePosition(component.Position, _random, out Vector3 target))
            return BuildIdleQueue(component, stat);

        var context = new ActionContext(component, stat, target, _wanderCost,
            moveRequest: MoveRequest.Navigated(target), navigation: _navigation);
        if (!TryRentAction(ActionType.Wander, context, rented))
        {
            ReturnAll(rented);
            return BuildIdleQueue(component, stat);
        }
        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildIdleQueue(NPCComponent component, NPCStat stat)
    {
        var rented = new List<IAction>();
        if (!TryRentAction(ActionType.Idle, new ActionContext(component, stat), rented))
            ReturnAll(rented);
        return new Queue<IAction>(rented);
    }

    private void LogConfigurationError(string dependencies)
    {
        if (_hasLoggedConfiguration)
            return;
        Debug.LogError($"BuilderActionSelector '{name}' requires {dependencies}.", this);
        _hasLoggedConfiguration = true;
    }
}
