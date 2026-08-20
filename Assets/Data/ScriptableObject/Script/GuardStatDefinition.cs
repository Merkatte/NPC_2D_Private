using UnityEngine;

[CreateAssetMenu(fileName = "GuardStatDefinition", menuName = "Scriptable Objects/NPCStatDefinition/GuardStatDefinition")]
public class GuardStatDefinition : NPCStatDefinition
{
    [SerializeField] private string _name = "Guard";

    [SerializeField] private float _health = 100f;
    [SerializeField] private float _healthMax = 100f;
    [SerializeField] private float _moveSpeed = 1f;

    [SerializeField] private float _fatigue = 1f;
    [SerializeField] private float _hunger = 50f;
    [SerializeField] private float _thirst = 10f;

    [SerializeField] private float _fatigueMax = 100f;
    [SerializeField] private float _hungerMax = 100f;
    [SerializeField] private float _thirstMax = 100f;

    [SerializeField] private float _attackPower = 1f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _attackRange = 1f;
    [SerializeField] private float _guardRadius = 5f;

    public override NPCStat CreateRuntimeStat()
    {
        return new GuardStat(
            _name,
            _health,
            _healthMax,
            _moveSpeed,
            _fatigue,
            _hunger,
            _thirst,
            _fatigueMax,
            _hungerMax,
            _thirstMax,
            _attackPower,
            _attackSpeed,
            _attackRange,
            _guardRadius);
    }
}
