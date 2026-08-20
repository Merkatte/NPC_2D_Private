using UnityEngine;

public class GuardStat : NPCStat, IGuardStatView
{
    private readonly float _attackPower;
    private readonly float _attackSpeed;
    private readonly float _attackRange;
    private readonly float _guardRadius;

    public GuardStat(string name, float health, float healthMax, float moveSpeed,
        float fatigue, float hunger, float thirst, float fatigueMax, float hungerMax, float thirstMax,
        float attackPower, float attackSpeed, float attackRange, float guardRadius)
        : base(name, health, healthMax, moveSpeed, fatigue, hunger, thirst, fatigueMax, hungerMax, thirstMax)
    {
        _attackPower = Mathf.Max(0.01f, attackPower);
        _attackSpeed = Mathf.Max(0.01f, attackSpeed);
        _attackRange = Mathf.Max(0.01f, attackRange);
        _guardRadius = Mathf.Max(0f, guardRadius);
    }

    public float AttackPower => _attackPower;
    public float AttackSpeed => _attackSpeed;
    public float AttackRange => _attackRange;
    public float GuardRadius => _guardRadius;
}
