using System;
using System.Collections.Generic;

/// <summary>Pure rule checks. These do not claim Unity lifecycle, physics or rendering coverage.</summary>
public static class NPCMessageTests
{
    private const LocalizeKey First = LocalizeKey.NPC_Thought_Farming_1;
    private const LocalizeKey Second = LocalizeKey.NPC_Thought_Farming_2;
    private const LocalizeKey Hungry = LocalizeKey.NPC_Thought_Hungry;
    private const LocalizeKey Fallback = LocalizeKey.NPC_Thought_Default;

    public static int RunPure()
    {
        int passed = 0;
        var stat = new TestStat();
        var candidates = new List<LocalizeKey>();
        var actions = new[]
        {
            new NPCThoughtCatalog.ActionThought(ActionType.Farming, First, Second, First),
            new NPCThoughtCatalog.ActionThought(ActionType.Harvest, LocalizeKey.NPC_Thought_Harvest_1),
            new NPCThoughtCatalog.ActionThought(ActionType.Guard, LocalizeKey.NPC_Thought_Guard_1)
        };
        var needs = new[]
        {
            new NPCThoughtCatalog.NeedThought(NPCThoughtCatalog.NeedType.Hunger, 0.6f, Hungry, First),
            new NPCThoughtCatalog.NeedThought(NPCThoughtCatalog.NeedType.Thirst, 0.6f, LocalizeKey.NPC_Thought_Thirsty_1),
            new NPCThoughtCatalog.NeedThought(NPCThoughtCatalog.NeedType.Fatigue, 0.6f, LocalizeKey.NPC_Thought_Tired_1)
        };
        NPCThoughtSelector.CollectCandidates(stat, ActionType.Farming, actions, needs, Fallback, candidates);
        RequireSet(candidates, First, Second);
        passed++;
        stat.Hunger = 60f;
        NPCThoughtSelector.CollectCandidates(stat, ActionType.Farming, actions, needs, Fallback, candidates);
        RequireSet(candidates, First, Second, Hungry);
        passed++;
        stat.Hunger = 59.99f;
        NPCThoughtSelector.CollectCandidates(stat, ActionType.Move, actions, needs, Fallback, candidates);
        RequireSet(candidates, Fallback);
        passed++;
        stat.Hunger = 60f;
        stat.Thirst = 60f;
        stat.Fatigue = 60f;
        NPCThoughtSelector.CollectCandidates(stat, ActionType.Move, actions, needs, Fallback, candidates);
        RequireSet(candidates, Hungry, First, LocalizeKey.NPC_Thought_Thirsty_1, LocalizeKey.NPC_Thought_Tired_1);
        passed++;
        stat.Maximum = 0f;
        NPCThoughtSelector.CollectCandidates(stat, null, actions, needs, Fallback, candidates);
        RequireSet(candidates, Fallback);
        passed++;
        stat.Maximum = 100f;
        stat.Hunger = stat.Thirst = stat.Fatigue = 0f;
        NPCThoughtSelector.CollectCandidates(stat, ActionType.Guard, actions, needs, Fallback, candidates);
        RequireSet(candidates, LocalizeKey.NPC_Thought_Guard_1);
        Require(stat.Hunger == 0f && stat.Thirst == 0f && stat.Fatigue == 0f && stat.Maximum == 100f,
            "Selection must leave gameplay stat unchanged.");
        passed++;

        var random = new TestRandom();
        Require(NPCThoughtSelector.TrySelect(new[] { First, Second }, First, random, out LocalizeKey key) && key == Second,
            "Multiple candidates must avoid the last key.");
        passed++;
        Require(NPCThoughtSelector.TrySelect(new[] { First }, First, random, out key) && key == First,
            "Single candidate must remain selectable.");
        passed++;
        var selected = new HashSet<LocalizeKey>();
        for (int index = 0; index < 3; index++)
        {
            random.NextIndex = index;
            Require(NPCThoughtSelector.TrySelect(new[] { First, Second, Hungry }, default, random, out key), "Valid candidates.");
            selected.Add(key);
        }
        Require(selected.SetEquals(new[] { First, Second, Hungry }) && random.LastMinimum == 0 && random.LastMaximum == 2,
            "Every unique candidate must occupy one equally likely random slot.");
        passed++;
        Require(!NPCThoughtSelector.TrySelect(Array.Empty<LocalizeKey>(), default, random, out key) && key == default,
            "Empty candidates fail with default out key.");
        passed++;

        random.NextIndex = 0;
        var state = new NPCMessageState();
        state.Synchronize(stat);
        var pair = new[] { First, Second };
        Require(state.TryGetMessage(pair, 4f, 10d, random, out key), "First query selects.");
        LocalizeKey initial = key;
        int calls = random.Calls;
        Require(state.TryGetMessage(pair, 4f, 13.999d, random, out key) && key == initial && random.Calls == calls,
            "Quick hover reentry retains selection until the original real-time deadline.");
        passed++;
        Require(state.TryGetMessage(pair, 4f, 14d, random, out key) && key != initial,
            "At the four-second deadline select a different key when available.");
        passed++;
        Require(state.TryGetMessage(new[] { Hungry }, 4f, 14.01d, random, out key) && key == Hungry,
            "A disappeared condition invalidates the cached key immediately at the next query.");
        passed++;
        Require(state.TryGetMessage(new[] { Fallback }, 4f, 14.02d, random, out key) && key == Fallback,
            "No eligible condition uses fallback.");
        Require(state.TryGetMessage(pair, 4f, 14.03d, random, out key) && key != Fallback,
            "Fallback is replaced as soon as eligible thoughts appear.");
        passed++;
        calls = random.Calls;
        Require(state.TrySetFixedMessage(LocalizeKey.Tutorial_Welcome, out uint firstToken) && firstToken != 0,
            "Fixed message produces nonzero ownership token.");
        Require(state.TryGetMessage(pair, 4f, 100d, random, out key) && key == LocalizeKey.Tutorial_Welcome && random.Calls == calls,
            "Fixed message overrides thoughts indefinitely without drawing randomness.");
        passed++;
        Require(state.TrySetFixedMessage(LocalizeKey.UI_Confirm, out uint secondToken) && secondToken != firstToken,
            "Replacement issues a fresh token.");
        Require(!state.TryClearFixedMessage(firstToken) && !state.TryClearFixedMessage(0), "Stale and zero tokens rejected.");
        Require(state.TryGetMessage(pair, 4f, 101d, random, out key) && key == LocalizeKey.UI_Confirm,
            "Rejected clear preserves the replacement.");
        passed++;
        Require(state.TryClearFixedMessage(secondToken), "Current owner may clear.");
        Require(state.TryGetMessage(new[] { Hungry }, 4f, 101.01d, random, out key) && key == Hungry,
            "Clearing fixed text selects from current conditions.");
        Require(!state.TryClearFixedMessage(secondToken), "Clearing twice fails.");
        passed++;
        Require(!state.TrySetFixedMessage(default, out uint invalidToken) && invalidToken == 0,
            "Undefined localization key rejected atomically.");
        passed++;

        state.TrySetFixedMessage(LocalizeKey.Tutorial_Welcome, out firstToken);
        state.Reset();
        state.Synchronize(stat);
        state.TrySetFixedMessage(LocalizeKey.UI_Confirm, out secondToken);
        Require(firstToken != secondToken && !state.TryClearFixedMessage(firstToken),
            "Pool reset cannot let an old request clear a new lifetime's fixed message.");
        passed++;
        state.Synchronize(new TestStat());
        Require(state.TryGetMessage(new[] { Hungry }, 4f, 102d, random, out key) && key == Hungry,
            "Runtime stat replacement clears prior fixed text and cached thought.");
        Require(!state.TryClearFixedMessage(secondToken), "Stat replacement invalidates old token.");
        passed++;
        var second = new NPCMessageState();
        var otherRandom = new TestRandom();
        second.Synchronize(new TestStat());
        second.TrySetFixedMessage(LocalizeKey.Tutorial_Welcome, out uint otherToken);
        state.Reset();
        Require(second.TryGetMessage(pair, 4f, 1000d, otherRandom, out key) && key == LocalizeKey.Tutorial_Welcome &&
            second.TryClearFixedMessage(otherToken), "NPC states and reset operations are independent.");
        passed++;
        Require(state.TryGetMessage(pair, 4f, 1000d, random, out key), "Query after reset.");
        calls = random.Calls;
        Require(state.TryGetMessage(pair, 4f, 1003.9d, random, out _) && calls == random.Calls,
            "Reset starts a fresh hold interval.");
        passed++;
        Console.WriteLine("PASS: " + passed + " NPC message pure rule cases (no Unity lifecycle/physics/render claims).");
        return passed;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RequireSet(List<LocalizeKey> actual, params LocalizeKey[] expected)
    {
        Require(actual.Count == expected.Length && new HashSet<LocalizeKey>(actual).SetEquals(expected),
            "Candidate set differs or contains duplicates. Expected: " + string.Join(",", expected) +
            "; actual: " + string.Join(",", actual));
    }

    private sealed class TestRandom : IRandomSource
    {
        public int Calls { get; private set; }
        public int NextIndex { get; set; }
        public int LastMinimum { get; private set; }
        public int LastMaximum { get; private set; }

        public int NextInclusive(int minimum, int maximum)
        {
            Calls++;
            LastMinimum = minimum;
            LastMaximum = maximum;
            Require(minimum <= maximum, "Random bounds must be valid.");
            return Math.Min(maximum, Math.Max(minimum, NextIndex));
        }
    }

    private sealed class TestStat : IStatView
    {
        public float Hunger { get; set; }
        public float Thirst { get; set; }
        public float Fatigue { get; set; }
        public float Maximum { get; set; } = 100f;
        public float GetCurrentHealth => 100f;
        public float GetMaxHealth => 100f;
        public float GetMoveSpeed => 1f;
        public float GetFatigue => Fatigue;
        public float GetHunger => Hunger;
        public float GetThirst => Thirst;
        public float GetFatigueMax => Maximum;
        public float GetHungerMax => Maximum;
        public float GetThirstMax => Maximum;
    }
}
