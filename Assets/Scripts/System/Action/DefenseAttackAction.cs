using UnityEngine;

public sealed class DefenseAttackAction : DefaultAction
{
    private DefenseActor _actor;
    private DefenseWallSegment _wall;
    private ICombatStatView _combat;
    private float _elapsed;
    private bool _hasHit;
    public DefenseAttackAction() : base(ActionType.DefenseAttack) { }
    public void Init(ActionContext context, DefenseActor actor, DefenseWallSegment wall)
    { base.Init(context); _actor = actor; _wall = wall; _combat = context.Stat as ICombatStatView; }
    public override void Start()
    {
        base.Start();
        if (IsFinished) return;
        if (!_actor || _combat == null || _combat.AttackSpeed <= 0f)
        { Fail("Defense attack requires initialized actor and combat stat."); return; }
        BeginSwing();
    }
    private void BeginSwing()
    {
        _elapsed = 0f; _hasHit = false;
        CombatTargetHandle target = actionContext.Component.CombatRuntimeState.TargetHandle;
        if (target.IsValid)
        {
            actionContext.Component.Flip(target.Target.Position.x < _actor.Position.x);
        }
        if (target.IsValid && _actor.Presentation)
            _actor.Presentation.BeginAttack(_actor.Role == NPCType.Archer, target.Target.Position, 1f / _combat.AttackSpeed);
    }
    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || Time.deltaTime <= 0f) return;
        if (!_actor || !_actor.CanAct) { RequestReplan(); return; }
        CombatTargetHandle target = actionContext.Component.CombatRuntimeState.TargetHandle;
        if (!target.IsValid || !CombatLib.IsInRange(_actor.Position, target.Target.Position, _combat.AttackRange))
        { RequestReplan(); return; }
        float interval = 1f / _combat.AttackSpeed;
        _elapsed += Time.deltaTime;
        if (!_hasHit && _elapsed >= interval * _actor.Settings.HitTimeRatio)
        {
            _hasHit = true;
            if (_actor.Role == NPCType.Archer)
            {
                if (!_actor.ProjectilePrefab) { Fail("Archer requires an explicit projectile prefab."); return; }
                DefenseProjectile projectile = Object.Instantiate(_actor.ProjectilePrefab, _actor.Position, Quaternion.identity);
                projectile.Launch(target.Target, target.Owner, _combat.AttackPower, _actor.Settings.ProjectileSpeed);
            }
            else
            {
                if (_wall && ReferenceEquals(target.Target, _wall.Target)) _actor.CommittedWall = _wall;
                target.Target.ApplyDamage(_combat.AttackPower);
            }
        }
        if (_elapsed >= interval) BeginSwing();
    }
    private void Cleanup()
    {
        if (_actor && _actor.Presentation) _actor.Presentation.StopAttack();
    }
    protected override void RequestReplan() { Cleanup(); base.RequestReplan(); }
    protected override void Fail(string reason) { Cleanup(); base.Fail(reason); }
    public override void Stop() { Cleanup(); base.Stop(); }
    public override void Clear()
    { Cleanup(); _actor = null; _wall = null; _combat = null; _elapsed = 0f; _hasHit = false; base.Clear(); }
}
