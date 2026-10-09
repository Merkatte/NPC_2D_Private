using System.Collections.Generic;
using UnityEngine;

// Scene policy shared by selectors. Actor-specific intent remains in DefenseActor/CombatRuntimeState.
public sealed class DefenseResponsePolicy : MonoBehaviour
{
    [SerializeField] private DefenseBattlefield _battlefield;
    [SerializeField] private DefenseCombatSettings _settings;
    public float ReassessmentSeconds => _settings ? _settings.ReassessmentSeconds : 0.25f;
    public DefenseActor GetActor(NPCComponent component) => _battlefield ? _battlefield.FindActor(component) : null;

    private CombatTarget SoldierTarget(DefenseActor actor)
    {
        if (actor.Role == NPCType.Archer)
            return _battlefield.FindEnemy(actor.Position, ((ICombatStatView)actor.Stat).AttackRange, false);
        CombatTarget nearby = _battlefield.FindEnemy(actor.Position, actor.Settings.LocalCombatRange, false);
        return nearby ? nearby : _battlefield.FindEnemy(actor.Position, float.PositiveInfinity, true);
    }
    public bool ShouldReplan(NPCComponent component, ActionType current)
    {
        DefenseActor actor = GetActor(component);
        if (!actor || !actor.CanAct) return false;
        if (actor.IsEnemy)
        {
            bool inside = _battlefield.IsInside(actor.Position);
            if (inside != actor.PlannedInside) return true;
            if (current == ActionType.Idle) return true;
            CombatTarget soldier = _battlefield.FindOutsideSwordsman(actor.Position, actor.Settings.LocalCombatRange);
            CombatTargetHandle selected = component.CombatRuntimeState.TargetHandle;
            if (!_battlefield.IsInside(actor.Position) && soldier && !ReferenceEquals(selected.Target, soldier)) return true;
            if (!inside && !soldier && !actor.CommittedWall && current == ActionType.Move)
            {
                DefenseWallSegment breach = _battlefield.FindWall(actor.Position, true, actor.Navigation);
                if (breach != actor.SelectedBreach) return true;
            }
            return false;
        }
        if (actor.IsSoldier)
        {
            CombatTarget desired = SoldierTarget(actor);
            if (actor.Role == NPCType.Archer && (desired || current == ActionType.DefenseAttack) && current != ActionType.DefenseStation &&
                ((Vector2)(actor.Position - actor.DutyPosition)).sqrMagnitude > 0.04f) return true;
            CombatTargetHandle selected = component.CombatRuntimeState.TargetHandle;
            if (desired) return (actor.Role != NPCType.Archer || current != ActionType.DefenseStation) &&
                (current != ActionType.DefenseAttack && current != ActionType.Move || !ReferenceEquals(selected.Target, desired));
            return current == ActionType.DefenseAttack || (current == ActionType.Move && selected.Target != null);
        }
        bool threat = _battlefield.IsAlert && _battlefield.FindEnemy(actor.Position, actor.Settings.FleeDetectionRange, false);
        return threat != actor.IsFleeing;
    }

    public bool TryBuildQueue(BaseNPCActionSelector selector, NPCStat stat, NPCComponent component, out Queue<IAction> queue)
    {
        queue = null;
        DefenseActor actor = GetActor(component);
        if (!actor || !actor.CanAct) return false;
        if (actor.IsEnemy) { queue = BuildEnemyQueue(selector, actor); return true; }
        if (actor.IsSoldier)
        {
            CombatTarget target = SoldierTarget(actor);
            if (actor.Role == NPCType.Archer && target &&
                ((Vector2)(actor.Position - actor.DutyPosition)).sqrMagnitude > 0.04f)
            { queue = BuildDutyQueue(selector, actor); return true; }
            if (target) { queue = BuildCombatQueue(selector, actor, target, null); return true; }
            component.CombatRuntimeState.ClearTarget();
            return false;
        }
        CombatTarget threat = _battlefield.IsAlert ? _battlefield.FindEnemy(actor.Position, actor.Settings.FleeDetectionRange, false) : null;
        actor.SetFleeing(threat);
        if (!threat) return false;
        if (!_battlefield.TryFindFleePosition(actor, threat, out Vector3 destination))
        { queue = selector.CreateDefenseIdle(component, stat); return true; }
        var rented = new List<IAction>();
        var context = new ActionContext(component, stat, destination,
            moveRequest: MoveRequest.Navigated(destination), navigation: actor.Navigation);
        if (!selector.RentDefenseAction(ActionType.Flee, context, rented)) selector.ReturnDefenseActions(rented);
        queue = new Queue<IAction>(rented);
        return true;
    }
    private Queue<IAction> BuildEnemyQueue(BaseNPCActionSelector selector, DefenseActor actor)
    {
        CombatTarget target;
        actor.PlannedInside = _battlefield.IsInside(actor.Position);
        actor.SelectedBreach = null;
        if (actor.PlannedInside)
        {
            actor.CommittedWall = null;
            target = _battlefield.FindInsideTarget(actor.Position, actor.Navigation);
            return target ? BuildCombatQueue(selector, actor, target, null) : selector.CreateDefenseIdle(actor.Component, actor.Stat);
        }
        target = _battlefield.FindOutsideSwordsman(actor.Position, actor.Settings.LocalCombatRange);
        if (target) return BuildCombatQueue(selector, actor, target, null);
        if (actor.CommittedWall && actor.CommittedWall.Target.CanBeTargeted)
            return BuildCombatQueue(selector, actor, actor.CommittedWall.Target, actor.CommittedWall);
        actor.CommittedWall = null;
        DefenseWallSegment breach = _battlefield.FindWall(actor.Position, true, actor.Navigation);
        if (breach) { actor.SelectedBreach = breach; return BuildMoveQueue(selector, actor, breach.EntryPosition); }
        DefenseWallSegment wall = _battlefield.FindWall(actor.Position, false, actor.Navigation);
        return wall ? BuildCombatQueue(selector, actor, wall.Target, wall) : selector.CreateDefenseIdle(actor.Component, actor.Stat);
    }
    public Queue<IAction> BuildDutyQueue(BaseNPCActionSelector selector, DefenseActor actor, GuardActionCost cost = null)
    {
        actor.Component.CombatRuntimeState.ClearTarget();
        var rented = new List<IAction>();
        IAction action = selector.RentDefenseAction(ActionType.DefenseStation);
        if (action is DefenseStationAction station)
        {
            station.Init(new ActionContext(actor.Component, actor.Stat, cost: cost), actor);
            rented.Add(station);
        }
        else if (action != null) selector.ReturnAction(action);
        return new Queue<IAction>(rented);
    }
    private Queue<IAction> BuildMoveQueue(BaseNPCActionSelector selector, DefenseActor actor, Vector3 destination)
    {
        actor.Component.CombatRuntimeState.ClearTarget();
        var rented = new List<IAction>();
        var context = new ActionContext(actor.Component, actor.Stat, destination,
            moveRequest: MoveRequest.Navigated(destination), navigation: actor.Navigation);
        if (!selector.RentDefenseAction(ActionType.Move, context, rented)) selector.ReturnDefenseActions(rented);
        return new Queue<IAction>(rented);
    }
    private Queue<IAction> BuildCombatQueue(BaseNPCActionSelector selector, DefenseActor actor, CombatTarget target, DefenseWallSegment wall)
    {
        actor.Component.CombatRuntimeState.SetTarget(target, target);
        var rented = new List<IAction>();
        var combat = actor.Stat as ICombatStatView;
        if (combat == null) return selector.CreateDefenseIdle(actor.Component, actor.Stat);
        if (actor.Role != NPCType.Archer && !CombatLib.IsInRange(actor.Position, target.Position, combat.AttackRange))
        {
            MoveRequest request = wall ? MoveRequest.Navigated(wall.AttackPosition, 0.1f)
                : MoveRequest.Navigated(actor.Component.CombatRuntimeState.TargetHandle, combat.AttackRange * 0.85f);
            if (!selector.RentDefenseAction(ActionType.Move, new ActionContext(actor.Component, actor.Stat,
                moveRequest: request, navigation: actor.Navigation), rented))
            { selector.ReturnDefenseActions(rented); return new Queue<IAction>(); }
        }
        IAction action = selector.RentDefenseAction(ActionType.DefenseAttack);
        if (action is DefenseAttackAction attack)
        {
            attack.Init(new ActionContext(actor.Component, actor.Stat), actor, wall);
            rented.Add(attack);
        }
        else
        {
            if (action != null) selector.ReturnAction(action);
            selector.ReturnDefenseActions(rented); rented.Clear();
        }
        return new Queue<IAction>(rented);
    }
}
