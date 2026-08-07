using UnityEngine;

public class NPCStat : IStatView
{
    public NPCStat(string name, float health, float healthMax, float moveSpeed)
    {
        _name = name;
        _health = health;
        _healthMax = healthMax;
        _moveSpeed = moveSpeed;
    }
    
    private string _name;
    
    private float _health;
    private float _healthMax;

    private float _moveSpeed;

    private float _fatigue = 1f;
    private float _hunger = 50f;
    private float _thirst = 10f;

    private float _fatigueMax = 100f;
    private float _hungerMax = 100f;
    private float _thirstMax = 100f;
    
    public float GetCurrentHealth => _health;
    public float GetMaxHealth => _healthMax;
    public float GetMoveSpeed => _moveSpeed;
    
    public float GetFatigue => _fatigue;
    public float GetHunger => _hunger;
    public float GetThirst => _thirst;
    
    public float GetFatigueMax => _fatigueMax;
    public float GetHungerMax => _hungerMax;
    public float GetThirstMax => _thirstMax;
    
    public float CurrentFatiguePercentage => GetPercentage(_fatigue, _fatigueMax);
    public float CurrentHungerPercentage => GetPercentage(_hunger, _hungerMax);
    public float CurrentThirstPercentage => GetPercentage(_thirst, _thirstMax);

    private float GetPercentage(float current, float max)
    {
        if (max <= 0f)
            return 0f;

        return Mathf.Clamp01(current / max) * 100f;
    }

    /// <summary>
    /// Change health by the given amount.
    /// </summary>
    /// <param name="val">Positive heals, negative damages.</param>
    /// <returns>Changed health</returns>
    public float ChangeHealth(float val)
    {
        _health = Mathf.Clamp(_health + val, 0, _healthMax);
        return _health;
    }

    /// <summary>
    /// Change movespeed by the given amount.
    /// </summary>
    /// <param name="val">Positive increase, negative decrease.</param>
    /// <returns>Changed health</returns>
    public float ChangeMoveSpeed(float val)
    {
        _moveSpeed = Mathf.Clamp(_moveSpeed + val, 0, _moveSpeed);
        return _moveSpeed;
    }

    public float ChangeHunger(float val)
    {
        _hunger = Mathf.Clamp(_hunger + val, 0, _hungerMax);
        return _hunger;
    }

    public float ChangeFatigue(float val)
    {
        _fatigue = Mathf.Clamp(_fatigue + val, 0, _fatigueMax);
        return _fatigue;
    }

    public float ChangeThirst(float val)
    {
        _thirst = Mathf.Clamp(_thirst + val, 0, _thirstMax);
        return _thirst;
    }
}
