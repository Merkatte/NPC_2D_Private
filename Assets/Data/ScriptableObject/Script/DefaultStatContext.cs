using UnityEngine;

[CreateAssetMenu(fileName = "DefaultStatContext", menuName = "Scriptable Objects/DefaultStatContext")]
public class DefaultStatContext : ScriptableObject
{
    [SerializeField] private string _name = "something";

    [SerializeField] private float _health = 100f;
    [SerializeField] private float _healthMax = 100f;
    [SerializeField] private float _moveSpeed = 1f;

    [SerializeField] private float _fatigue = 1f;
    [SerializeField] private float _hunger = 50f;
    [SerializeField] private float _thirst = 10f;

    [SerializeField] private float _fatigueMax = 100f;
    [SerializeField] private float _hungerMax = 100f;
    [SerializeField] private float _thirstMax = 100f;
    
    public string Name => _name;
    public float Health => _health;
    public float HealthMax => _healthMax;
    public float MoveSpeed => _moveSpeed;
    public float Fatigue => _fatigue;
    public float Hunger => _hunger;
    public float Thirst => _thirst;
    public float FatigueMax => _fatigueMax;
    public float HungerMax => _hungerMax;
    public float ThirstMax => _thirstMax;

    public NPCStat CreateStat()
    {
        return new NPCStat(
            _name,
            _health,
            _healthMax,
            _moveSpeed,
            _fatigue,
            _hunger,
            _thirst,
            _fatigueMax,
            _hungerMax,
            _thirstMax);
    }
}
