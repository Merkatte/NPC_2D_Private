using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStatDefinition", menuName = "Scriptable Objects/NPCStatDefinition/EnemyStatDefinition")]
public class EnemyStatDefinition : NPCStatDefinition
{
    [SerializeField] private string _name = "Enemy";

    [SerializeField] private float _health = 10f;
    [SerializeField] private float _healthMax = 10f;
    [SerializeField] private float _moveSpeed = 1f;

    [SerializeField] private float _attackPower = 1f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 1f;
    [SerializeField] private AttackStyle _style = AttackStyle.Melee;
    [SerializeField, Range(0f, 1f)] private float _preferredAttackRangeRatio = 0.9f;

    // Enemy has no instinct: hunger/thirst/fatigue are never read by EnemyActionSelector, so they
    // are not exposed here (unlike GuardStatDefinition/FarmerStatDefinition). NPCStat still carries
    // the fields for base-class compatibility; they stay pinned at 0 and are simply never used.
    public override NPCStat CreateRuntimeStat()
    {
        return new EnemyStat(
            _name,
            _health,
            _healthMax,
            _moveSpeed,
            fatigue: 0f,
            hunger: 0f,
            thirst: 0f,
            fatigueMax: 0f,
            hungerMax: 0f,
            thirstMax: 0f,
            _attackPower,
            _attackSpeed,
            _attackRange,
            _style,
            _preferredAttackRangeRatio);
    }

    private void OnValidate()
    {
        _health = Mathf.Max(0f, _health);
        _healthMax = Mathf.Max(0f, _healthMax);
        _moveSpeed = Mathf.Max(0f, _moveSpeed);
        _attackPower = Mathf.Max(0.01f, _attackPower);
        _attackSpeed = Mathf.Max(0.01f, _attackSpeed);
        _attackRange = Mathf.Max(0.01f, _attackRange);
        _preferredAttackRangeRatio = Mathf.Clamp01(_preferredAttackRangeRatio);
    }
}
