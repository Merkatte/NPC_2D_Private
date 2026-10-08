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

    protected override DestinationDecider HousingDecider => _decider;
    public override bool HasAvailableWork(NPCStat stat, NPCComponent component)
    {
        if (stat == null || stat.IsOnStrike || !component || !_navigation || !_navigation.IsReady ||
            !_buildingPlots || !_buildCost || !_buildCost.IsConfigured) return false;
        foreach (BuildingPlot plot in _buildingPlots.Plots)
            if (plot && plot.CanReserve) return true;
        return false;
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
        => RequestResidentialWorkQueue(stat, npcType, component, null);

    protected override Queue<IAction> RequestResidentialWorkQueue(NPCStat stat, NPCType npcType,
        NPCComponent component, HousingLifeSettings life)
    {
        if (!component || stat == null || !actionPool)
        {
            LogConfigurationError("NPCComponent, NPCStat and ActionPool");
            return new Queue<IAction>();
        }
        if (_decider == null || !_destinationDB || !_decisionTuning)
        {
            LogConfigurationError("destination and tuning");
            return BuildFallbackIdleQueue(component, stat);
        }

        NPCDecision decision = _decider.Decide(stat, NPCType.Builder, component.Position, null,
            sleepRecoveryPerSecond: life?.InnFatigueRecoveryPerSecond ?? 0f);
        if (stat.IsOnStrike || decision.Intent == NPCIntent.Eat || decision.Intent == NPCIntent.Drink || decision.Intent == NPCIntent.Sleep)
            return BuildLeisureQueue(decision, _decider, component, stat, MoveMode.Navigation,
                _navigation, _wanderCost, _randomSource ? _random : null);

        // A critical unmet need must not be replaced with a role activity.
        if (_decider.HasCriticalNeed(stat))
            return BuildFallbackIdleQueue(component, stat);

        var rented = new List<IAction>();
        if (HasAvailableWork(stat, component))
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
                    moveRequest: MoveRequest.Navigated(reservation.WorkPosition), navigation: _navigation, requiresWorkAvailability: true);
                if (TryRentAction(ActionType.Build, buildContext, rented)) return new Queue<IAction>(rented);
                reservation.Dispose();
                ReturnAll(rented);
                return BuildFallbackIdleQueue(component, stat);
            }
        }
        return BuildLeisureQueue(decision, _decider, component, stat, MoveMode.Navigation,
            _navigation, _wanderCost, _randomSource ? _random : null);
    }


    private void LogConfigurationError(string dependencies)
    {
        if (_hasLoggedConfiguration)
            return;
        Debug.LogError($"BuilderActionSelector '{name}' requires {dependencies}.", this);
        _hasLoggedConfiguration = true;
    }
}
