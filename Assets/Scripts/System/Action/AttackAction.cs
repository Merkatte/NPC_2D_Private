using UnityEngine;

public class AttackAction : DefaultAction
{
    // Caps hits processed in a single Tick so a huge frame delta (or an absurd AttackSpeed)
    // can't apply unbounded damage in one frame.
    private const int MaxHitsPerTick = 5;

    private float _timer;
    private ICombatStatView _combatStat;

    public AttackAction() : base(ActionType.Attack)
    {
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
        {
            return;
        }

        CombatRuntimeState runtimeState = actionContext.Component.CombatRuntimeState;
        _combatStat = actionContext.Stat as ICombatStatView;

        if (!runtimeState.HasValidTarget)
        {
            Fail("AttackAction started without a valid target");
            return;
        }

        if (_combatStat == null || _combatStat.AttackSpeed <= 0f)
        {
            Fail("AttackAction started with an invalid attack speed");
            return;
        }

        _timer = 0f;
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
        {
            return;
        }

        var component = actionContext.Component;

        if (!component || _combatStat == null)
        {
            Fail("AttackAction lost its required references mid-tick");
            return;
        }

        CombatRuntimeState runtimeState = component.CombatRuntimeState;
        float interval = 1f / _combatStat.AttackSpeed;
        _timer += Time.deltaTime;

        int hits = 0;
        bool targetDied = false;
        bool outOfRange = false;

        while (_timer >= interval && hits < MaxHitsPerTick)
        {
            CombatTargetHandle handle = runtimeState.TargetHandle;
            if (!handle.IsValid)
            {
                targetDied = true;
                break;
            }

            if (!CombatLib.IsInRange(component.Position, handle.Target.Position, _combatStat.AttackRange))
            {
                outOfRange = true;
                break;
            }

            _timer -= interval;
            handle.Target.ApplyDamage(_combatStat.AttackPower);
            hits++;

            if (!handle.IsValid)
            {
                targetDied = true;
                break;
            }
        }

        if (hits >= MaxHitsPerTick)
        {
            _timer = Mathf.Min(_timer, interval * MaxHitsPerTick);
        }

        if (targetDied)
        {
            runtimeState.ClearTarget();
            RequestReplan();
            return;
        }

        if (outOfRange)
        {
            RequestReplan();
        }
    }

    public override void Clear()
    {
        _timer = 0f;
        _combatStat = null;
        base.Clear();
    }
}
