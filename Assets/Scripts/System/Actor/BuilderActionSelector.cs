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
    [SerializeField] private DefenseMaintenanceRegistry _maintenance;

    public override bool ShouldReplan(NPCStat stat, NPCType npcType, NPCComponent component,
        ActionType currentAction, ResidentHousingState residence)
    {
        if (base.ShouldReplan(stat, npcType, component, currentAction, residence)) return true;
        if (!_maintenance || stat == null || stat.IsOnStrike || _decider == null || _decider.HasCriticalNeed(stat)) return false;
        DefenseActor actor = defenseResponse ? defenseResponse.GetActor(component) : null;
        if (currentAction == ActionType.Maintain) return actor && _maintenance.HasHigherPriority(actor.CurrentMaintenance);
        return currentAction == ActionType.Build && _maintenance.FindAvailable();
    }

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

    internal override void RegisterWorker(WorkerNPC worker, NPCStat stat)
    {
        if (_buildingPlots) _buildingPlots.Dispatcher.Register(worker, stat, this);
    }

    internal override void UnregisterWorker(WorkerNPC worker)
    {
        if (_buildingPlots) _buildingPlots.Dispatcher.Unregister(worker);
    }

    internal int NavigationRevision => _navigation ? _navigation.Revision : -1;

    internal bool CanTakePriority(WorkerNPC worker, NPCStat stat, BuildingPlot plot, int rank)
    {
        if (!isActiveAndEnabled || !worker || !worker.CanReplaceCurrentAction || !worker.Component ||
            !_navigation || !_navigation.IsReady || !actionPool || !_buildCost || !_buildCost.IsConfigured ||
            _decider == null || !BuilderWorkPolicy.CanWorkOn(stat, plot) || stat.GetMoveSpeed <= 0f ||
            _buildCost.ShouldInterrupt(stat)) return false;

        ActionType current = worker.CurrentAction.GetMyActionType();
        ResidentHousingState residence = worker.Residence;
        if (residence != null && residence.RequiresFirstHomeVisit) return false;
        if (current == ActionType.Build && worker.CurrentAction is BuildAction build)
        {
            int assignedRank = _buildingPlots.GetPriorityRank(build.ReservedPlot);
            if (assignedRank > 0 && assignedRank <= rank) return false;
        }
        else if (current == ActionType.HomeStay)
        {
            if (residence == null || !residence.IsRegistered || !residence.IsInsideHome) return false;
        }
        else if (current != ActionType.Idle && current != ActionType.Wander) return false;

        DefenseActor actor = worker.DefenseActor;
        if (actor && (!actor.CanAct || actor.IsFleeing)) return false;
        if (defenseResponse && defenseResponse.ShouldReplan(worker.Component, current)) return false;
        return stat.IsOnStrike || !_maintenance || !_maintenance.FindAvailable();
    }

    internal bool TryMeasureRoute(Vector3 start, Vector3 destination, List<Vector3> waypoints, out float distance)
    {
        distance = 0f;
        if (!_navigation.TryBuildPath(start, destination, waypoints, out _) || waypoints.Count == 0)
            return false;
        foreach (Vector3 point in waypoints)
        {
            distance += Vector2.Distance(start, point);
            start = point;
        }
        return true;
    }

    internal bool TryDispatch(ConstructionPriorityManager.BuilderRegistration registration,
        ConstructionProjectInfo project, int rank, int slot)
    {
        WorkerNPC worker = registration.Worker;
        NPCStat stat = registration.Stat;
        BuildingPlot plot = project.Plot;
        if (!registration.IsLive || !project.IsCurrent || worker.ActionVersion != registration.ActionVersion ||
            NavigationRevision != registration.NavigationRevision ||
            _buildingPlots.GetPriorityRank(plot) != rank || !CanTakePriority(worker, stat, plot, rank)) return false;
        // A resident leaving home gets the same needs decision as an ordinary work request.
        if (worker.CurrentAction is HomeStayAction && IsLifeActivity(
            DecideActivity(stat, worker.Component, worker.Residence.LifeSettings).Intent)) return false;
        if (!plot.TryReserve(project.ApplicationOrder, slot, out ConstructionReservation reservation)) return false;

        // Reservation listeners can cancel the project, change the order or disable an actor.
        // Keep its current action and lease intact until all of those callbacks have returned.
        if (registration.IsLive && worker.ActionVersion == registration.ActionVersion &&
            NavigationRevision == registration.NavigationRevision &&
            _buildingPlots.GetPriorityRank(plot) == rank && CanTakePriority(worker, stat, plot, rank))
        {
            IAction action = RentBuildAction(worker.Component, stat, plot, reservation);
            if (action != null)
            {
                if (worker.TryReplaceCurrentAction(registration.ActionVersion, action)) return true;
                ReturnAction(action);
            }
        }
        reservation.Dispose();
        return false;
    }

    private IAction RentBuildAction(NPCComponent component, NPCStat stat, BuildingPlot plot, ConstructionReservation reservation)
    {
        IAction action = GetAction(ActionType.Build);
        if (action == null) return null;
        action.Init(new ActionContext(component, stat, reservation.WorkPosition, _buildCost, provider: plot,
            request: new InteractionRequest(ActionType.Build, reservation: reservation),
            moveRequest: MoveRequest.Navigated(reservation.WorkPosition), navigation: _navigation,
            requiresWorkAvailability: true));
        return action;
    }

    private NPCDecision DecideActivity(NPCStat stat, NPCComponent component, HousingLifeSettings life)
        => _decider.Decide(stat, NPCType.Builder, component.Position, null,
            sleepRecoveryPerSecond: life?.InnFatigueRecoveryPerSecond ?? 0f);

    private static bool IsLifeActivity(NPCIntent intent)
        => intent == NPCIntent.Eat || intent == NPCIntent.Drink || intent == NPCIntent.Sleep;

    private bool CanReserveOrdinaryWork(NPCStat stat, BuildingPlot plot)
        => plot && plot.CanReserve && _buildingPlots.GetPriorityRank(plot) == 0 && BuilderWorkPolicy.CanWorkOn(stat, plot);

    public override bool HasAvailableWork(NPCStat stat, NPCComponent component)
    {
        if (stat != null && !stat.IsOnStrike && _maintenance && _maintenance.FindAvailable()) return true;
        if (stat == null || !component || !_navigation || !_navigation.IsReady ||
            !_buildingPlots || !_buildCost || !_buildCost.IsConfigured) return false;
        foreach (BuildingPlot plot in _buildingPlots.Plots)
            if (CanReserveOrdinaryWork(stat, plot)) return true;
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

        NPCDecision decision = DecideActivity(stat, component, life);
        if ((stat.IsOnStrike && !HasAvailableWork(stat, component)) || IsLifeActivity(decision.Intent))
            return BuildLeisureQueue(decision, _decider, component, stat, MoveMode.Navigation,
                _navigation, _wanderCost, _randomSource ? _random : null);

        // A critical unmet need must not be replaced with a role activity.
        if (_decider.HasCriticalNeed(stat))
            return BuildFallbackIdleQueue(component, stat);

        var rented = new List<IAction>();
        DefenseMaintenanceSite maintenanceSite = !stat.IsOnStrike && _maintenance ? _maintenance.FindAvailable() : null;
        if (maintenanceSite && maintenanceSite.TryReserve(out MaintenanceLease maintenanceLease))
        {
            DefenseActor actor = defenseResponse ? defenseResponse.GetActor(component) : null;
            if (actor) actor.CurrentMaintenance = maintenanceSite;
            var context = new ActionContext(component, stat, maintenanceLease.WorkPosition, _buildCost,
                provider: maintenanceSite, request: new InteractionRequest(ActionType.Maintain, reservation: maintenanceLease),
                moveRequest: MoveRequest.Navigated(maintenanceLease.WorkPosition), navigation: actor ? actor.Navigation : _navigation,
                requiresWorkAvailability: true);
            if (TryRentAction(ActionType.Maintain, context, rented)) return new Queue<IAction>(rented);
            maintenanceLease.Dispose(); ReturnAll(rented); return BuildFallbackIdleQueue(component, stat);
        }
        if (HasAvailableWork(stat, component))
        {
            var candidates = new List<BuildingPlot>();
            foreach (BuildingPlot plot in _buildingPlots.Plots)
                if (CanReserveOrdinaryWork(stat, plot)) candidates.Add(plot);
            candidates.Sort((left, right) => left.ApplicationOrder.CompareTo(right.ApplicationOrder));
            foreach (BuildingPlot plot in candidates)
            {
                if (!plot.TryReserve(out ConstructionReservation reservation)) continue;
                IAction action = RentBuildAction(component, stat, plot, reservation);
                if (action != null)
                {
                    var queue = new Queue<IAction>();
                    queue.Enqueue(action);
                    return queue;
                }
                reservation.Dispose();
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
