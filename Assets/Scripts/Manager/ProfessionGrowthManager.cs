using System.Collections.Generic;
using UnityEngine;

public sealed class ProfessionGrowthManager : MonoBehaviour
{
    [SerializeField] private ProfessionGrowthSettings _settings;
    private readonly Dictionary<NPCType, int> _levelCaps = new Dictionary<NPCType, int>();
    public ProfessionGrowthSettings Settings => _settings;

    public int GetLevelCap(NPCType profession)
    {
        if (_levelCaps.TryGetValue(profession, out int cap)) return cap;
        return _settings && _settings.TryGetRule(profession, out var rule) ? rule.BaseLevelCap : 1;
    }

    public bool TrySetLevelCap(NPCType profession, int levelCap)
    {
        if (levelCap < 1 || !_settings || !_settings.TryGetRule(profession, out _)) return false;
        _levelCaps[profession] = levelCap;
        return true;
    }
}
