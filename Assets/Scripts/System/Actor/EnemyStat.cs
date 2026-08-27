using UnityEngine;

public class EnemyStat : NPCStat, IEnemyStatView
{
    private readonly float _attackPower;
    private readonly float _attackSpeed;
    private readonly float _attackRange;
    private readonly AttackStyle _style;
    private readonly float _preferredAttackRangeRatio;

    public EnemyStat(string name, float health, float healthMax, float moveSpeed,
        float fatigue, float hunger, float thirst, float fatigueMax, float hungerMax, float thirstMax,
        float attackPower, float attackSpeed, float attackRange, AttackStyle style, float preferredAttackRangeRatio)
        : base(name, health, healthMax, moveSpeed, fatigue, hunger, thirst, fatigueMax, hungerMax, thirstMax)
    {
        _attackPower = Mathf.Max(0.01f, attackPower);
        _attackSpeed = Mathf.Max(0.01f, attackSpeed);
        _attackRange = Mathf.Max(0.01f, attackRange);
        _style = style;
        _preferredAttackRangeRatio = Mathf.Clamp01(preferredAttackRangeRatio);
    }

    public float AttackPower => _attackPower;
    public float AttackSpeed => _attackSpeed;
    public float AttackRange => _attackRange;
    public AttackStyle Style => _style;
    public float PreferredAttackRangeRatio => _preferredAttackRangeRatio;
}
