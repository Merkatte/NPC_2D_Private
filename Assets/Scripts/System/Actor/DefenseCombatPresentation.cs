using UnityEngine;

// Weapon and pose presentation only: attack actions own hit timing and damage.
public sealed class DefenseCombatPresentation : MonoBehaviour
{
    [SerializeField] private Transform _body;
    [SerializeField] private Transform _weapon;
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private Sprite _sword;
    [SerializeField] private Sprite _bow;
    private Quaternion _bodyRotation;
    private Quaternion _weaponRotation;
    private Vector3 _weaponScale;
    private float _elapsed;
    private float _duration;
    private bool _isRanged;
    private bool _isAttacking;
    private bool _isDowned;
    private void Awake()
    {
        if (_body) _bodyRotation = _body.localRotation;
        if (_weapon) { _weaponRotation = _weapon.localRotation; _weaponScale = _weapon.localScale; }
    }
    public void SetRole(NPCType role)
    {
        if (_weaponRenderer && (role == NPCType.Guard || role == NPCType.Archer))
        { _weaponRenderer.sprite = role == NPCType.Archer ? _bow : _sword; _weaponRenderer.gameObject.SetActive(true); }
    }
    public void BeginAttack(bool ranged, Vector3 target, float duration)
    {
        if (_weaponRenderer && !_isDowned) _weaponRenderer.gameObject.SetActive(true);
        _isRanged = ranged; _duration = Mathf.Max(0.1f, duration); _elapsed = 0f; _isAttacking = true;
    }
    private void LateUpdate()
    {
        if (_isDowned || !_isAttacking || !_weapon || Time.deltaTime <= 0f) return;
        _elapsed += Time.deltaTime;
        float phase = Mathf.Clamp01(_elapsed / _duration);
        float swing = Mathf.Sin(phase * Mathf.PI * 2f);
        _weapon.localRotation = _weaponRotation * Quaternion.Euler(0f, 0f, _isRanged ? swing * 12f : swing * 65f);
        _weapon.localScale = _isRanged ? Vector3.Scale(_weaponScale, new Vector3(1f - Mathf.Sin(phase * Mathf.PI) * 0.15f, 1f, 1f)) : _weaponScale;
    }
    public void StopAttack()
    {
        // Ending an action resets its pose, but an active soldier keeps carrying its weapon.
        _isAttacking = false;
        if (_weapon) { _weapon.localRotation = _weaponRotation; _weapon.localScale = _weaponScale; }
    }
    public void ShowDowned()
    {
        StopAttack();
        _isDowned = true;
        if (_weaponRenderer) _weaponRenderer.gameObject.SetActive(false);
        if (_body) _body.localRotation = _bodyRotation * Quaternion.Euler(0f, 0f, 85f);
    }
    public void ResetPose()
    { _isDowned = false; StopAttack(); if (_body) _body.localRotation = _bodyRotation; }
}
