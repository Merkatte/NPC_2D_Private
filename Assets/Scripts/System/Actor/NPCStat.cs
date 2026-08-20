using UnityEngine;

public class NPCStat : IStatView
{
    public NPCStat(string name, float health, float healthMax, float moveSpeed,
        float fatigue, float hunger, float thirst, float fatigueMax, float hungerMax, float thirstMax)
    {
        _name = name;
        _healthMax = Mathf.Max(0f, healthMax);
        _health = Mathf.Clamp(health, 0f, _healthMax);
        _moveSpeed = Mathf.Max(0f, moveSpeed);
        _fatigueMax = Mathf.Max(0f, fatigueMax);
        _hungerMax = Mathf.Max(0f, hungerMax);
        _thirstMax = Mathf.Max(0f, thirstMax);
        _fatigue = Mathf.Clamp(fatigue, 0f, _fatigueMax);
        _hunger = Mathf.Clamp(hunger, 0f, _hungerMax);
        _thirst = Mathf.Clamp(thirst, 0f, _thirstMax);
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

    public void ApplyStatEffect(StatEffect effect)
    {
        ChangeHealth(effect.HealthDelta);
        ChangeHunger(effect.HungerDelta);
        ChangeThirst(effect.ThirstDelta);
        ChangeFatigue(effect.FatigueDelta);
        Debug.Log("Current Health = " + _health);
        Debug.Log("Current Hunger = " + _hunger);
        Debug.Log("Current Thirst = " + _thirst);
        Debug.Log("Current Fatigue = " + _fatigue);
    }
}
