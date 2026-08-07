using UnityEngine;

public interface IStatView
{
    //Health Info
    float GetCurrentHealth { get; }
    float GetMaxHealth { get; }
    
    //Speed Info
    float GetMoveSpeed { get; }
    
    //Natural Info
    float GetFatigue { get; }
    float GetHunger { get; }
    float GetThirst { get; }
    float GetFatigueMax { get; }
    float GetHungerMax { get; }
    float GetThirstMax { get; }
    
    //Percentage Info
    float CurrentFatiguePercentage { get; }
    float CurrentHungerPercentage { get; }
    float CurrentThirstPercentage { get; }
}
