using UnityEngine;

// Weapon and pose presentation only: attack actions own hit timing and damage.
public sealed class DefenseCombatPresentation : MonoBehaviour
{
    [SerializeField] private Transform _body;
    [SerializeField] private Transform _weapon;
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private Sprite _sword;
    [SerializeField] private Sprite _bow;
    [SerializeField] private Transform _chargeVisual;
    [SerializeField] private ParticleSystem _windupParticles;
    private Quaternion _bodyRotation;
    private Vector3 _bodyPosition;
    private Vector3 _bodyScale;
    private Vector3 _chargeScale;
    private Quaternion _weaponRotation;
    private Vector3 _weaponScale;
    private float _elapsed;
    private float _duration;
    private bool _isRanged;
    private bool _isAttacking;
    private bool _isDowned;
    private float _windupParticleElapsed = -1f;
    private void Awake()
    {
        if (_body)
        {
            _bodyRotation = _body.localRotation;
            _bodyPosition = _body.localPosition;
            _bodyScale = _body.localScale;
        }
        if (_chargeVisual)
        {
            _chargeScale = _chargeVisual.localScale;
            _chargeVisual.gameObject.SetActive(false);
        }
        if (_weapon) { _weaponRotation = _weapon.localRotation; _weaponScale = _weapon.localScale; }
        ClearWindupParticles();
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
    public void PresentEnemyAttack(DefenseEnemyAttackDefinition definition, float elapsed)
    {
        if (!definition || _isDowned) return;
        // The action supplies time; this adapter never releases a projectile or advances a cycle.
        _isAttacking = false;
        float windup = Mathf.Clamp01(elapsed / definition.WindupSeconds);
        bool isWindingUp = elapsed < definition.WindupSeconds;
        Vector3 scale = Vector3.one;
        float lift = 0f;
        switch (definition.Style)
        {
            case DefenseEnemyAttackStyle.GoblinArrow:
                if (isWindingUp)
                {
                    if (windup < 0.8f)
                        scale = Vector3.Lerp(Vector3.one, new Vector3(1.18f, 0.7f, 1f), windup / 0.8f);
                    else
                    {
                        float pop = (windup - 0.8f) / 0.2f;
                        scale = Vector3.Lerp(new Vector3(1.18f, 0.7f, 1f), new Vector3(0.88f, 1.18f, 1f), pop);
                        lift = 0.18f * pop;
                    }
                }
                else
                {
                    float settle = Mathf.Clamp01((elapsed - definition.WindupSeconds) / definition.RecoverySeconds);
                    scale = Vector3.Lerp(new Vector3(0.88f, 1.18f, 1f), Vector3.one, settle);
                    lift = 0.18f * (1f - settle);
                }
                break;
            case DefenseEnemyAttackStyle.ShamanEnergyBall:
                if (isWindingUp)
                {
                    float pulse = Mathf.Sin(windup * Mathf.PI * 8f);
                    scale = new Vector3(1f - pulse * 0.08f, 1f + pulse * 0.18f, 1f);
                }
                break;
            case DefenseEnemyAttackStyle.SalamanderFireBurst:
                float inhale = isWindingUp ? Mathf.SmoothStep(0f, 1f, windup) :
                    1f - Mathf.Clamp01((elapsed - definition.WindupSeconds) /
                        Mathf.Max(0.12f, definition.BurstSeconds));
                scale = new Vector3(1f - inhale * 0.12f, 1f + inhale * 0.32f, 1f);
                break;
        }
        if (_body)
        {
            _body.localScale = Vector3.Scale(_bodyScale, scale);
            _body.localPosition = _bodyPosition + Vector3.up * lift;
        }
        if (_chargeVisual)
        {
            bool showCharge = definition.Style == DefenseEnemyAttackStyle.ShamanEnergyBall && isWindingUp;
            _chargeVisual.gameObject.SetActive(showCharge);
            _chargeVisual.localScale = _chargeScale * (showCharge ? Mathf.Lerp(0.12f, 1f, windup) : 1f);
        }
        PresentWindupParticles(isWindingUp && (definition.Style == DefenseEnemyAttackStyle.ShamanEnergyBall ||
            definition.Style == DefenseEnemyAttackStyle.SalamanderFireBurst), elapsed);
    }
    private void PresentWindupParticles(bool isWindingUp, float elapsed)
    {
        if (!_windupParticles) return;
        if (!isWindingUp || elapsed < 0f || float.IsNaN(elapsed) || float.IsInfinity(elapsed))
        {
            if (_windupParticleElapsed >= 0f) ClearWindupParticles();
            return;
        }
        bool restart = _windupParticleElapsed < 0f || elapsed < _windupParticleElapsed;
        float step = restart ? elapsed : elapsed - _windupParticleElapsed;
        // Simulate pauses the system afterwards. Without a new action sample, even
        // action-only Pause (timeScale still 1) cannot advance these visual particles.
        if (restart || step > 0f) _windupParticles.Simulate(step, false, restart, false);
        _windupParticleElapsed = elapsed;
    }
    private void ClearWindupParticles()
    {
        if (_windupParticles) _windupParticles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        _windupParticleElapsed = -1f;
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
        _elapsed = 0f;
        ClearWindupParticles();
        if (_body) { _body.localPosition = _bodyPosition; _body.localScale = _bodyScale; }
        if (_chargeVisual) { _chargeVisual.localScale = _chargeScale; _chargeVisual.gameObject.SetActive(false); }
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
    private void OnDisable() { ResetPose(); }
}
