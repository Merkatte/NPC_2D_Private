using System;
using System.Collections.Generic;

public static class NPCThoughtSelector
{
    private static readonly HashSet<LocalizeKey> ValidKeys
        = new HashSet<LocalizeKey>((LocalizeKey[])Enum.GetValues(typeof(LocalizeKey)));

    public static void CollectCandidates(
        IStatView stat, ActionType? actionType,
        IReadOnlyList<NPCThoughtCatalog.ActionThought> actions,
        IReadOnlyList<NPCThoughtCatalog.NeedThought> needs,
        LocalizeKey fallback, List<LocalizeKey> results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));
        results.Clear();

        // Move describes execution, not its destination or purpose.
        if (actionType.HasValue && actionType.Value != ActionType.Move && actions != null)
        {
            for (int i = 0; i < actions.Count; ++i)
            {
                NPCThoughtCatalog.ActionThought thought = actions[i];
                if (thought != null && thought.ActionType == actionType.Value)
                    AddUnique(thought.Keys, results);
            }
        }

        if (stat != null && needs != null)
        {
            for (int i = 0; i < needs.Count; ++i)
            {
                NPCThoughtCatalog.NeedThought thought = needs[i];
                if (thought != null && MatchesNeed(stat, thought))
                    AddUnique(thought.Keys, results);
            }
        }

        if (results.Count == 0 && IsValidKey(fallback))
            results.Add(fallback);
    }

    public static bool TrySelect(IReadOnlyList<LocalizeKey> candidates, LocalizeKey previous,
        IRandomSource random, out LocalizeKey key)
    {
        key = default;
        if (candidates == null || candidates.Count == 0 || random == null)
            return false;

        int eligibleCount = 0;
        for (int i = 0; i < candidates.Count; ++i)
        {
            if (IsValidKey(candidates[i]) && candidates[i] != previous)
                ++eligibleCount;
        }
        bool excludePrevious = eligibleCount > 0;
        if (!excludePrevious)
        {
            for (int i = 0; i < candidates.Count; ++i)
                if (IsValidKey(candidates[i]))
                    ++eligibleCount;
        }
        if (eligibleCount == 0)
            return false;

        int selected = eligibleCount == 1 ? 0 : random.NextInclusive(0, eligibleCount - 1);
        if (selected < 0 || selected >= eligibleCount)
            return false;
        for (int i = 0; i < candidates.Count; ++i)
        {
            LocalizeKey candidate = candidates[i];
            if (!IsValidKey(candidate) || (excludePrevious && candidate == previous))
                continue;
            if (selected-- == 0)
            {
                key = candidate;
                return true;
            }
        }
        return false;
    }

    internal static bool IsValidKey(LocalizeKey key) => ValidKeys.Contains(key);

    private static void AddUnique(IReadOnlyList<LocalizeKey> keys, List<LocalizeKey> results)
    {
        if (keys == null)
            return;
        for (int i = 0; i < keys.Count; ++i)
            if (IsValidKey(keys[i]) && !results.Contains(keys[i]))
                results.Add(keys[i]);
    }

    private static bool MatchesNeed(IStatView stat, NPCThoughtCatalog.NeedThought thought)
    {
        float current;
        float maximum;
        switch (thought.NeedType)
        {
            case NPCThoughtCatalog.NeedType.Hunger:
                current = stat.GetHunger;
                maximum = stat.GetHungerMax;
                break;
            case NPCThoughtCatalog.NeedType.Thirst:
                current = stat.GetThirst;
                maximum = stat.GetThirstMax;
                break;
            case NPCThoughtCatalog.NeedType.Fatigue:
                current = stat.GetFatigue;
                maximum = stat.GetFatigueMax;
                break;
            default:
                return false;
        }
        float threshold = thought.Threshold;
        float ratio = current / maximum;
        return !float.IsNaN(current) && !float.IsInfinity(current)
            && !float.IsNaN(maximum) && !float.IsInfinity(maximum) && maximum > 0f
            && !float.IsNaN(threshold) && threshold >= 0f && threshold <= 1f
            // CompareTo takes the stored Single by reference, rounding a Mono/x87
            // extended-precision quotient to the same precision as the threshold.
            && ratio.CompareTo(threshold) >= 0;
    }
}
