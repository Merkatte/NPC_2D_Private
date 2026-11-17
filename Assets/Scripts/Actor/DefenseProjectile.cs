using UnityEngine;

public sealed class DefenseProjectile : MonoBehaviour
{
    private readonly CombatTargetHandle _target = new CombatTargetHandle();
    private CombatTarget _targetLifetimeOwner;
    private uint _targetLifetimeVersion;
    private WorkerNPC _source;
    private uint _sourceLifetimeVersion;
    private NPCProfessionGrowth _sourceGrowth;
    private NPCType _sourceProfession;
    private float _damage;
    private float _speed;
    private bool _launched;

    public void Launch(ICombatTarget target, Component owner, float damage, float speed, WorkerNPC source = null)
    {
        _target.Set(target, owner);
        _targetLifetimeOwner = target as CombatTarget;
        _targetLifetimeVersion = _targetLifetimeOwner ? _targetLifetimeOwner.LifetimeVersion : 0;
        _source = source;
        _sourceLifetimeVersion = source ? source.LifetimeVersion : 0;
        _sourceGrowth = source ? source.ProfessionGrowth : null;
        _sourceProfession = source ? source.Profession : NPCType.Enemy;
        _damage = Mathf.Max(0f, damage);
        _speed = Mathf.Max(0.1f, speed);
        _launched = true;
    }
    private void Update()
    {
        if (!_launched || Time.deltaTime <= 0f) return;
        if ((_targetLifetimeOwner && _targetLifetimeOwner.LifetimeVersion != _targetLifetimeVersion) ||
            !_target.TryGetPosition(out Vector3 destination)) { Destroy(gameObject); return; }
        Vector3 offset = destination - transform.position;
        offset.z = 0f;
        float step = _speed * Time.deltaTime;
        if (offset.sqrMagnitude <= step * step)
        {
            _launched = false;
            float damage = _target.IsValid ? _target.Target.ApplyDamage(_damage) : 0f;
            // A late projectile can still hit after its archer is downed, but never credits a new resident.
            if (_source && _source.isActiveAndEnabled && _source.LifetimeVersion == _sourceLifetimeVersion &&
                ReferenceEquals(_source.ProfessionGrowth, _sourceGrowth) && _sourceProfession == NPCType.Archer)
                _sourceGrowth?.Award(_sourceProfession, WorkContributionKind.Damage, damage);
            _target.Clear();
            Destroy(gameObject);
            return;
        }
        transform.position += offset.normalized * step;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
    }
    private void OnDisable()
    {
        _target.Clear();
        _targetLifetimeOwner = null;
        _targetLifetimeVersion = 0;
        _source = null;
        _sourceGrowth = null;
        _sourceLifetimeVersion = 0;
        _sourceProfession = NPCType.Enemy;
        _damage = 0f;
        _speed = 0f;
        _launched = false;
    }
}
