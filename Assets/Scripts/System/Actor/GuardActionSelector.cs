using System.Collections.Generic;
using UnityEngine;

public class GuardActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;
    [SerializeField] private MoveMode _facilityMoveMode;
    [SerializeField] private TilemapNavigation _navigation;
    [SerializeField] private WanderActionCost _wanderCost;
    [SerializeField] private MonoBehaviour _randomSource;

    private IRandomSource _random;

    private bool _hasLoggedNavigationSetup;

    private DestinationDecider _decider;
    private GuardActionCost _guardActionCostInfo;

    // Cost of one Guard duty evaluation slice. Derived from shared tuning and the shared cost
    // asset, so it is not per-NPC state and is safe to cache on this shared selector instance.
    private StatEffect _guardDutyCost;
    private readonly List<(ICombatTarget Target, Component Owner)> _candidateBuffer = new List<(ICombatTarget, Component)>();

    private void Awake() => _random = _randomSource as IRandomSource;

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
        return stat is GuardStat;
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        if (!component)
            return new Queue<IAction>();

        if (_decider == null || _decisionTuning == null || _destinationDB == null || stat == null || _guardActionCostInfo == null)
        {
            Debug.LogError("GuardActionSelector is missing required setup (decider/tuning/destinationDB/stat/GuardActionCost); falling back to Idle.");
            return BuildFallbackIdleQueue(component, stat);
        }

        if (!(stat is GuardStat guardStat))
        {
            Debug.LogError("Guard selector requires a stat implementing GuardStat; falling back to Idle.");
            return BuildFallbackIdleQueue(component, stat);
        }

        if (stat.IsOnStrike)
        {
            component.CombatRuntimeState.ClearTarget();
            return BuildLeisureQueue(_decider.Decide(stat, npcType, component.Position, _guardDutyCost), _decider,
                component, stat, _facilityMoveMode, _navigation, _wanderCost, _randomSource ? _random : null);
        }

        if (TryBuildCombatQueue(component, stat, guardStat, out Queue<IAction> combatQueue))
            return combatQueue;

        if (_facilityMoveMode == MoveMode.Navigation && (!_navigation || !_navigation.IsReady))
        {
            if (!_hasLoggedNavigationSetup)
            {
                Debug.LogError("GuardActionSelector: Navigation mode requires a ready TilemapNavigation.", this);
                _hasLoggedNavigationSetup = true;
            }
            return BuildFallbackIdleQueue(component, stat);
        }

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
            return BuildFallbackIdleQueue(component, stat);
        }

        return BuildGuardQueue(component, stat, decision);
    }

    private bool TryBuildCombatQueue(NPCComponent component, NPCStat stat, GuardStat guardStat, out Queue<IAction> queue)
    {
        queue = null;
        CombatRuntimeState runtimeState = component.CombatRuntimeState;

        if (!runtimeState.HasValidTarget && !TryAcquireNearestTarget(component, runtimeState))
        {
            return false;
        }

        queue = BuildCombatQueue(component, stat, guardStat, runtimeState);
        return true;
    }

    private bool TryAcquireNearestTarget(NPCComponent component, CombatRuntimeState runtimeState)
    {
        if (!CombatLib.TryFindNearestTarget(component.CombatPerception, component.Position, _candidateBuffer,
                maxRange: null, out ICombatTarget target, out Component owner))
        {
            return false;
        }

        runtimeState.SetTarget(target, owner);
        return true;
    }

    private Queue<IAction> BuildCombatQueue(NPCComponent component, NPCStat stat, GuardStat guardStat, CombatRuntimeState runtimeState)
    {
        List<IAction> rented = new List<IAction>();
        CombatTargetHandle handle = runtimeState.TargetHandle;

        if (!CombatLib.IsInRange(component.Position, handle.Target.Position, guardStat.AttackRange))
        {
            float stoppingDistance = guardStat.AttackRange * _guardActionCostInfo.AttackStoppingDistanceRatio;
            ActionContext moveContext = new ActionContext(component, stat, moveRequest: MoveRequest.Dynamic(handle, stoppingDistance), requiresWorkAvailability: true);

            if (!TryRentAction(ActionType.Move, moveContext, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        ActionContext attackContext = new ActionContext(component, stat, requiresWorkAvailability: true);
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
        if (!decision.HasLiveDestination) return BuildFallbackIdleQueue(component, stat);
        List<IAction> rented = new List<IAction>();

        ActionContext moveContext = BuildMoveContext(decision.DestinationPos, component, stat);
        if (!TryRentAction(ActionType.Move, moveContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        ActionType actionType = ToActionType(decision.Intent);
        IInteractionProvider provider = decision.Provider;
        ActionContext interactContext = new ActionContext(component, stat, decision.DestinationPos,
            provider: provider, request: decision.Request);

        if (!TryRentAction(actionType, interactContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildGuardQueue(NPCComponent component, NPCStat stat, NPCDecision decision)
    {
        IInteractionProvider provider = decision.HasLiveDestination ? decision.Provider : null;
        Vector3 guardPos = decision.DestinationPos;
        if (provider == null && !DestinationDecider.TrySelectNearest(_destinationDB, BuildingType.GuardPost, ActionType.Guard,
            component.Position, out _, out provider, out guardPos))
            return BuildFallbackIdleQueue(component, stat);
        if (!provider.CanInteract(ActionType.Guard) || !provider.TryGetActionPosition(ActionType.Guard, guardPos, out Vector3 firstPoint))
            return BuildFallbackIdleQueue(component, stat);

        List<IAction> rented = new List<IAction>();
        ActionContext movement = BuildMoveContext(firstPoint, component, stat);
        ActionContext context = new ActionContext(component, stat, firstPoint, _guardActionCostInfo,
            provider: provider, moveRequest: movement.MoveRequest, navigation: movement.Navigation, requiresWorkAvailability: true);
        if (!TryRentAction(ActionType.Guard, context, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }
        return new Queue<IAction>(rented);
    }


    private ActionContext BuildMoveContext(Vector3 destination, NPCComponent component, NPCStat stat)
    {
        MoveRequest request = _facilityMoveMode == MoveMode.Navigation
            ? MoveRequest.Navigated(destination)
            : MoveRequest.Fixed(destination);
        return new ActionContext(component, stat, destination, moveRequest: request, navigation: _navigation);
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
