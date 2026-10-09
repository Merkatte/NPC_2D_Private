using UnityEngine;

public sealed class DefenseProjectile : MonoBehaviour
{
    private readonly CombatTargetHandle _target = new CombatTargetHandle();
    private float _damage;
    private float _speed;
    private bool _launched;
    public void Launch(ICombatTarget target, Component owner, float damage, float speed)
    { _target.Set(target, owner); _damage = Mathf.Max(0f, damage); _speed = Mathf.Max(0.1f, speed); _launched = true; }
    private void Update()
    {
        if (!_launched || Time.deltaTime <= 0f) return;
        if (!_target.TryGetPosition(out Vector3 destination)) { Destroy(gameObject); return; }
        Vector3 offset = destination - transform.position;
        offset.z = 0f;
        float step = _speed * Time.deltaTime;
        if (offset.sqrMagnitude <= step * step)
        {
            _launched = false;
            if (_target.IsValid) _target.Target.ApplyDamage(_damage);
            _target.Clear(); Destroy(gameObject); return;
        }
        transform.position += offset.normalized * step;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
    }
    private void OnDisable() { _target.Clear(); _launched = false; }
}
