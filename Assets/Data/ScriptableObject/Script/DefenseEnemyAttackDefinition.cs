using UnityEngine;

[CreateAssetMenu(fileName = "DefenseEnemyAttack", menuName = "Scriptable Objects/Defense/Enemy Attack")]
public sealed class DefenseEnemyAttackDefinition : ScriptableObject
{
    [SerializeField] private DefenseEnemyAttackStyle _style;
    [SerializeField, Min(0.05f)] private float _windupSeconds = 0.45f;
    [SerializeField, Min(0.05f)] private float _recoverySeconds = 0.55f;
    [SerializeField, Range(1, 12)] private int _burstCount = 5;
    [SerializeField, Min(0.02f)] private float _burstIntervalSeconds = 0.12f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 8f;

    public DefenseEnemyAttackStyle Style => (int)_style >= (int)DefenseEnemyAttackStyle.GoblinArrow &&
        (int)_style <= (int)DefenseEnemyAttackStyle.SalamanderFireBurst ? _style : DefenseEnemyAttackStyle.GoblinArrow;
    public float WindupSeconds => ValidPositive(_windupSeconds, 0.05f, 0.45f);
    public float RecoverySeconds => ValidPositive(_recoverySeconds, 0.05f, 0.55f);
    public int ProjectileCount => Style == DefenseEnemyAttackStyle.SalamanderFireBurst ? Mathf.Clamp(_burstCount, 1, 12) : 1;
    public float BurstIntervalSeconds => ValidPositive(_burstIntervalSeconds, 0.02f, 0.12f);
    public float ProjectileSpeed => ValidPositive(_projectileSpeed, 0.1f, 8f);
    public float BurstSeconds => (ProjectileCount - 1) * BurstIntervalSeconds;
    public float CycleSeconds => WindupSeconds + BurstSeconds + RecoverySeconds;

    private void OnValidate()
    {
        _style = Style;
        _windupSeconds = WindupSeconds;
        _recoverySeconds = RecoverySeconds;
        _burstCount = Mathf.Clamp(_burstCount, 1, 12);
        _burstIntervalSeconds = BurstIntervalSeconds;
        _projectileSpeed = ProjectileSpeed;
    }

    private static float ValidPositive(float value, float minimum, float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) || value > float.MaxValue / 32f
            ? fallback : Mathf.Max(minimum, value);
}
