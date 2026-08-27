using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy has no instinct: unlike Farmer/Guard, this selector never consults DestinationDecider
/// or DestinationDB. It only reacts to CombatPerception - no target means Idle, nothing else.
/// A single scene instance is shared by every spawned Enemy (mirrors GuardActionSelector); it is
/// not attached to the Enemy prefab because prefabs cannot reference the scene's ActionPool.
/// </summary>
public class EnemyActionSelector : BaseNPCActionSelector
{
    private readonly List<(ICombatTarget Target, Component Owner)> _candidateBuffer = new List<(ICombatTarget, Component)>();

    public override bool CanUseStat(NPCStat stat)
    {
        return stat is IEnemyStatView;
    }

    public override Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        if (!component)
        {
            return new Queue<IAction>();
        }

        if (!actionPool)
        {
            Debug.LogError("EnemyActionSelector has no ActionPool assigned; cannot build a queue.");
            return new Queue<IAction>();
        }

        if (!(stat is IEnemyStatView enemyStat))
        {
            Debug.LogError("Enemy selector requires a stat implementing IEnemyStatView; falling back to Idle.");
            return BuildIdleQueue(component, stat);
        }

        switch (enemyStat.Style)
        {
            case AttackStyle.Melee:
                return BuildMeleeQueue(component, stat, enemyStat);
            case AttackStyle.Ranged:
                return BuildRangedQueue(component, stat, enemyStat);
            default:
                return BuildIdleQueue(component, stat);
        }
    }

    /// <summary>
    /// Closes distance and attacks, exactly like GuardActionSelector's combat queue: sticky
    /// target (kept until dead/destroyed), chase if out of range.
    /// </summary>
    private Queue<IAction> BuildMeleeQueue(NPCComponent component, NPCStat stat, IEnemyStatView enemyStat)
    {
        CombatRuntimeState runtimeState = component.CombatRuntimeState;

        if (!runtimeState.HasValidTarget)
        {
            if (!CombatTargeting.TryFindNearestTarget(component.CombatPerception, component.Position, _candidateBuffer,
                    maxRange: null, out ICombatTarget target, out Component owner))
            {
                return BuildIdleQueue(component, stat);
            }

            runtimeState.SetTarget(target, owner);
        }

        List<IAction> rented = new List<IAction>();
        CombatTargetHandle handle = runtimeState.TargetHandle;

        if (!CombatRange.IsInRange(component.Position, handle.Target.Position, enemyStat.AttackRange))
        {
            float stoppingDistance = enemyStat.AttackRange * enemyStat.PreferredAttackRangeRatio;
            ActionContext moveContext = new ActionContext(component, stat, moveRequest: MoveRequest.Dynamic(handle, stoppingDistance));

            if (!TryRentAction(ActionType.Move, moveContext, rented))
            {
                ReturnAll(rented);
                return new Queue<IAction>();
            }
        }

        ActionContext attackContext = new ActionContext(component, stat);
        if (!TryRentAction(ActionType.Attack, attackContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    /// <summary>
    /// Never moves. Re-scans every replan (not sticky) so a target that drifts out of range is
    /// dropped instead of being chased - "does not approach melee-range units."
    /// </summary>
    private Queue<IAction> BuildRangedQueue(NPCComponent component, NPCStat stat, IEnemyStatView enemyStat)
    {
        CombatRuntimeState runtimeState = component.CombatRuntimeState;

        if (!CombatTargeting.TryFindNearestTarget(component.CombatPerception, component.Position, _candidateBuffer,
                maxRange: enemyStat.AttackRange, out ICombatTarget target, out Component owner))
        {
            runtimeState.ClearTarget();
            return BuildIdleQueue(component, stat);
        }

        runtimeState.SetTarget(target, owner);

        List<IAction> rented = new List<IAction>();
        ActionContext attackContext = new ActionContext(component, stat);
        if (!TryRentAction(ActionType.Attack, attackContext, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }

    private Queue<IAction> BuildIdleQueue(NPCComponent component, NPCStat stat)
    {
        List<IAction> rented = new List<IAction>();
        ActionContext context = new ActionContext(component, stat);

        if (!TryRentAction(ActionType.Idle, context, rented))
        {
            ReturnAll(rented);
            return new Queue<IAction>();
        }

        return new Queue<IAction>(rented);
    }
}
