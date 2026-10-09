using System.Collections.Generic;
using UnityEngine;

// Defense role queue uses existing need utility, with assigned duty instead of random patrol.
public sealed class DefenseSoldierActionSelector : BaseNPCActionSelector
{
    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;
    [SerializeField] private TilemapNavigation _navigation;
    [SerializeField] private WanderActionCost _wanderCost;
    [SerializeField] private MonoBehaviour _randomSource;
    [SerializeField] private GuardActionCost _guardCost;
    private DestinationDecider _decider;
    private IRandomSource _random;
    private StatEffect _dutyCost;
    private void Awake()
    {
        _random = _randomSource as IRandomSource;
        if (!_destinationDB || !_decisionTuning || !_guardCost) return;
        _decider = new DestinationDecider(); _decider.Init(_destinationDB, _decisionTuning);
        float duration = _decisionTuning.GuardDutyEvaluationSeconds;
        _dutyCost = new StatEffect(hungerDelta: _guardCost.HungerPerSecond * duration,
            thirstDelta: _guardCost.ThirstPerSecond * duration, fatigueDelta: _guardCost.FatiguePerSecond * duration);
    }
    protected override DestinationDecider HousingDecider => _decider;
    public override bool CanUseStat(NPCStat stat) => stat is GuardStat;
    public override bool HasAvailableWork(NPCStat stat, NPCComponent component)
        => stat != null && !stat.IsOnStrike && defenseResponse && defenseResponse.GetActor(component);
    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
        => RequestResidentialWorkQueue(stat, npcType, component, null);
    protected override Queue<IAction> RequestResidentialWorkQueue(NPCStat stat, NPCType npcType,
        NPCComponent component, HousingLifeSettings life)
    {
        DefenseActor actor = defenseResponse ? defenseResponse.GetActor(component) : null;
        if (!actor || _decider == null || !_guardCost) return BuildFallbackIdleQueue(component, stat);
        NPCDecision decision = _decider.Decide(stat, NPCType.Guard, component.Position, _dutyCost,
            sleepRecoveryPerSecond: life?.InnFatigueRecoveryPerSecond ?? 0f,
            guardDuty: new GuardDutyDecisionContext(actor.DutyPosition,
                actor.CanAct && actor.IsSoldier && (actor.Role != NPCType.Archer || actor.ArcherWall)));
        if (stat.IsOnStrike || decision.Intent == NPCIntent.Eat || decision.Intent == NPCIntent.Drink || decision.Intent == NPCIntent.Sleep)
            return BuildLeisureQueue(decision, _decider, component, stat, MoveMode.Navigation, _navigation, _wanderCost, _random);
        if (_guardCost.ShouldInterrupt(stat)) return BuildFallbackIdleQueue(component, stat);
        return defenseResponse.BuildDutyQueue(this, actor, _guardCost);
    }
}
