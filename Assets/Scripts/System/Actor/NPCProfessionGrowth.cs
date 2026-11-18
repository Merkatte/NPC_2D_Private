using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NPCProfessionGrowth
{
    private sealed class Record
    {
        public int Level = 1;
        public double Experience;
    }
    private readonly Dictionary<NPCType, Record> _records = new Dictionary<NPCType, Record>();
    private readonly ProfessionGrowthSettings _settings;
    private readonly ProfessionGrowthManager _manager;

    public NPCProfessionGrowth(ProfessionGrowthSettings settings = null, ProfessionGrowthManager manager = null)
    {
        _settings = settings;
        _manager = manager;
    }

    public bool TryGetProgress(NPCType profession, out ProfessionProgress progress)
    {
        progress = default;
        if (!_settings || !_settings.TryGetRule(profession, out var rule)) return false;
        int cap = GetLevelCap(profession, rule);
        if (_records.TryGetValue(profession, out Record record))
            progress = new ProfessionProgress(record.Level, (float)record.Experience, cap, rule.RequiredExperience(record.Level));
        else
            progress = new ProfessionProgress(1, 0f, cap, rule.RequiredExperience(1));
        return true;
    }

    public void Award(NPCType profession, WorkContributionKind kind, float amount)
    {
        if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) ||
            !_settings || !_settings.TryGetRule(profession, out var rule)) return;
        float coefficient = rule.ExperiencePerUnit(kind);
        if (coefficient <= 0f) return;
        AddExperience(profession, rule, (double)amount * coefficient, GetLevelCap(profession, rule));
    }

    // Training caps this one payment. Damage and other contributions retain the scene's cap.
    // The return value indicates whether a subsequent award can still advance this record.
    internal bool AwardTrainingExperience(NPCType profession, double experience, int trainingCap)
    {
        if (experience < 0d || double.IsNaN(experience) || double.IsInfinity(experience) ||
            !_settings || !_settings.TryGetRule(profession, out var rule)) return false;
        int cap = Math.Min(trainingCap, GetLevelCap(profession, rule));
        return AddExperience(profession, rule, experience, cap);
    }

    private bool AddExperience(NPCType profession, ProfessionGrowthSettings.ProfessionRule rule,
        double experience, int cap)
    {
        if (!_records.TryGetValue(profession, out Record record))
        { record = new Record(); _records.Add(profession, record); }
        // A lowered cap freezes the existing record without discarding its stored remainder.
        if (record.Level >= cap) return false;
        double remaining = record.Experience + experience;
        while (record.Level < cap)
        {
            float required = rule.RequiredExperience(record.Level);
            if (remaining < required) break;
            remaining -= required;
            record.Level++;
        }
        record.Experience = record.Level >= cap ? 0d : remaining;
        return record.Level < cap;
    }

    private int GetLevelCap(NPCType profession, ProfessionGrowthSettings.ProfessionRule rule)
        => _manager && _manager.Settings == _settings ? _manager.GetLevelCap(profession) : rule.BaseLevelCap;
}
