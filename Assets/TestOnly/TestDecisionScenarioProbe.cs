using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Deterministic decision probe for <see cref="DestinationDecider"/>.
///
/// The project has no assembly definitions, so there is no Unity Test Framework assembly to
/// host EditMode tests, and adding one would restructure the runtime assembly. This probe is
/// the documented alternative: it drives the real decider with fully controlled stats and
/// positions against the real scene DestinationDB, and prints a PASS/FAIL report.
///
/// Deliberately limited: it uses no reflection and adds no test seam to production code, so
/// scenarios that need arbitrary supply options, forced score ties, or alternate tuning values
/// are reported as NOT VERIFIED instead of being faked.
/// </summary>
public class TestDecisionScenarioProbe : MonoBehaviour
{
    private const float WindowWidth = 260f;
    private const float WindowHeight = 80f;

    [SerializeField] private DestinationDB _destinationDB;
    [SerializeField] private NPCDecisionTuning _decisionTuning;
    [SerializeField] private FarmingActionCost _farmingActionCost;
    [SerializeField] private GuardActionCost _guardActionCost;

    // Used to place an NPC far enough from every destination that no trip can pay for itself.
    [SerializeField] private float _farDistance = 200f;

    private Rect _windowRect = new Rect(20f, 170f, WindowWidth, WindowHeight);

    private readonly StringBuilder _report = new StringBuilder();
    private readonly List<string> _firstRunResults = new List<string>();
    private readonly List<string> _currentRunResults = new List<string>();

    private int _passCount;
    private int _failCount;
    private int _skipCount;
    private bool _recordOnly;

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Decision Probe");
    }

    private void DrawWindow(int windowId)
    {
        if (GUILayout.Button("Run Decision Scenarios", GUILayout.Height(32f)))
        {
            RunAll();
        }

        GUI.DragWindow();
    }

    private void RunAll()
    {
        if (!_destinationDB || !_decisionTuning || !_farmingActionCost || !_guardActionCost)
        {
            Debug.LogError("TestDecisionScenarioProbe is missing one of its serialized references.");
            return;
        }

        _report.Clear();
        _passCount = 0;
        _failCount = 0;
        _skipCount = 0;

        _report.AppendLine("=== DestinationDecider scenario probe ===");
        _report.Append("LookAheadDepth=").Append(_decisionTuning.LookAheadDepth)
            .Append("  MinimumWorkBatch=").Append(_decisionTuning.MinimumWorkBatch)
            .Append("  CriticalNeedThreshold=").Append(_decisionTuning.CriticalNeedThreshold)
            .AppendLine();

        // First pass records every decision so the second pass can prove determinism.
        _firstRunResults.Clear();
        _currentRunResults.Clear();
        _recordOnly = false;
        RunScenarios();
        _firstRunResults.AddRange(_currentRunResults);

        // Second pass: same inputs, results must match exactly.
        _currentRunResults.Clear();
        _recordOnly = true;
        RunScenarios();
        _recordOnly = false;
        CheckDeterminism();

        AppendNotVerifiedSection();

        _report.Append("Result: ").Append(_passCount).Append(" passed, ")
            .Append(_failCount).Append(" failed, ")
            .Append(_skipCount).Append(" skipped.");

        if (_failCount > 0)
            Debug.LogError(_report.ToString());
        else
            Debug.Log(_report.ToString());
    }

    private void RunScenarios()
    {
        RunFarmerScenarios();
        RunGuardScenarios();
        RunGeneralScenarios();
    }

    // ---- Farmer ----

    private void RunFarmerScenarios()
    {
        if (!TryGetPos(BuildingType.Farm, out Vector3 farmPos))
        {
            Skip("Farmer 1-6", "Farm destination is not registered in DestinationDB");
            return;
        }

        bool hasPub = TryGetPos(BuildingType.Pub, out Vector3 pubPos);
        DestinationDecider decider = CreateDecider();
        StatEffect workCost = CreateFarmingCost();

        NPCDecision d = Decide(decider, NewStat(20f, 20f, 20f), NPCType.Farmer, farmPos, workCost);
        Check("Farmer 1", "healthy Farmer at Farm chooses a bounded Work batch",
            d.Intent == NPCIntent.Work && d.RepeatCount >= _decisionTuning.MinimumWorkBatch, d);

        d = Decide(decider, NewStat(20f, 99f, 20f), NPCType.Farmer, farmPos, workCost);
        Check("Farmer 2", "critical Hunger rejects Work and chooses a Food option",
            d.Intent == NPCIntent.Eat, d);

        if (hasPub)
        {
            d = Decide(decider, NewStat(50f, 75f, 50f), NPCType.Farmer, pubPos, workCost);
            Check("Farmer 3", "Farmer at Pub after one bread stays for another supply action",
                IsSupply(d.Intent), d);

            d = Decide(decider, NewStat(20f, 20f, 20f), NPCType.Farmer, pubPos, workCost);
            Check("Farmer 4", "recovered Farmer at Pub returns to Work with no satisfaction threshold",
                d.Intent == NPCIntent.Work, d);
        }
        else
        {
            Skip("Farmer 3-4", "Pub destination is not registered in DestinationDB");
        }

        // Same stats, different position: at the Farm the work trip is free, far away it is not.
        ProbeStat stat = NewStat(20f, 20f, 20f);
        NPCDecision atFarm = Decide(decider, stat, NPCType.Farmer, farmPos, workCost);
        NPCDecision farAway = Decide(decider, stat, NPCType.Farmer, FarFrom(farmPos), workCost);
        Check("Farmer 5", "identical stats decide differently at Farm and far from Farm",
            atFarm.Intent == NPCIntent.Work && farAway.Intent != NPCIntent.Work, farAway);

        d = Decide(decider, NewStat(20f, 90f, 20f), NPCType.Farmer, farmPos, workCost);
        Check("Farmer 6", "unsafe work count below MinimumWorkBatch never forces one Farming action",
            d.Intent != NPCIntent.Work, d);
    }

    // ---- Guard ----

    private void RunGuardScenarios()
    {
        if (!TryGetPos(BuildingType.GuardPost, out Vector3 postPos))
        {
            Skip("Guard 1-6", "GuardPost destination is not registered in DestinationDB");
            return;
        }

        bool hasPub = TryGetPos(BuildingType.Pub, out Vector3 pubPos);
        DestinationDecider decider = CreateDecider();
        StatEffect dutyCost = CreateGuardDutyCost();

        NPCDecision d = Decide(decider, NewStat(20f, 20f, 20f), NPCType.Guard, postPos, dutyCost);
        Check("Guard 1", "healthy Guard at GuardPost chooses duty over supply",
            !IsSupply(d.Intent), d);

        d = Decide(decider, NewStat(20f, 99f, 20f), NPCType.Guard, postPos, dutyCost);
        Check("Guard 2", "critical need interrupts duty and chooses a matching supply action",
            d.Intent == NPCIntent.Eat, d);

        if (hasPub)
        {
            // Below GuardActionCost's 0.97 interrupt threshold, so this only happens because the
            // selector now consults the decider on every replan.
            d = Decide(decider, NewStat(90f, 75f, 90f), NPCType.Guard, pubPos, dutyCost);
            Check("Guard 3", "Guard below its interrupt threshold still gets a supply decision at Pub",
                IsSupply(d.Intent), d);

            d = Decide(decider, NewStat(75f, 75f, 75f), NPCType.Guard, pubPos, dutyCost);
            Check("Guard 4", "Guard performs another supply action while already at the facility",
                IsSupply(d.Intent), d);

            // "Eventually returns" is the acceptance criterion, so drive the real replan loop:
            // decide, apply that one action's effect, then decide again from the new state and
            // the new position - exactly what WorkerNPC does between queues.
            RunGuardConvergenceScenario(decider, dutyCost, pubPos);
        }
        else
        {
            Skip("Guard 3-5", "Pub destination is not registered in DestinationDB");
        }

        // Travel time is distance/moveSpeed, so lowering move speed is exactly equivalent to
        // moving every facility farther away while duty stays at zero travel.
        ProbeStat slow = NewStat(60f, 60f, 60f);
        slow.MoveSpeed = 0.2f;
        NPCDecision farCase = Decide(decider, slow, NPCType.Guard, postPos, dutyCost);

        ProbeStat fast = NewStat(60f, 60f, 60f);
        fast.MoveSpeed = 20f;
        NPCDecision nearCase = Decide(decider, fast, NPCType.Guard, postPos, dutyCost);

        Check("Guard 6", "moderate need: Guard stays on duty when the round trip is expensive, leaves when it is cheap",
            !IsSupply(farCase.Intent) && IsSupply(nearCase.Intent), farCase);

        Skip("Guard 7", "combat priority is a GuardActionSelector concern, not a decider one - Play Mode check");
    }

    private const int MaxConvergenceSteps = 30;

    /// <summary>
    /// A Guard that starts at a facility with moderate needs may rationally take several supply
    /// actions before leaving. What must not happen is consuming forever, so this walks the loop
    /// and fails only if it never stops choosing supply.
    /// </summary>
    private void RunGuardConvergenceScenario(DestinationDecider decider, StatEffect dutyCost, Vector3 startPos)
    {
        ProbeStat stat = NewStat(75f, 75f, 75f);
        Vector3 pos = startPos;
        int supplyActions = 0;
        NPCDecision d = default;

        while (supplyActions < MaxConvergenceSteps)
        {
            d = Decide(decider, stat, NPCType.Guard, pos, dutyCost);
            if (!IsSupply(d.Intent))
                break;

            if (!TryApplyChosenSupply(d, stat))
            {
                Skip("Guard 5", "could not resolve the chosen supply option's effect to continue the loop");
                return;
            }

            pos = d.DestinationPos;
            supplyActions++;
        }

        Check("Guard 5", $"Guard stops consuming and returns to duty after {supplyActions} supply action(s)",
            supplyActions < MaxConvergenceSteps, d);
    }

    /// <summary>
    /// Applies the effect the chosen supply action would produce, using the same clamp order as
    /// NPCStat.ApplyStatEffect. Reads options through AppendOptions only - it never calls
    /// TryInteract, so scene inventory is not consumed by the probe.
    /// </summary>
    private bool TryApplyChosenSupply(NPCDecision decision, ProbeStat stat)
    {
        if (decision.Intent == NPCIntent.Sleep)
        {
            stat.Fatigue = 0f;
            return true;
        }

        if (!decision.Request.HasValue)
            return false;

        InteractionRequest request = decision.Request.Value;

        if (!_destinationDB.TryGetInteractionProvider(decision.DestinationKey, request.Type, out IInteractionProvider provider))
            return false;

        List<InteractionOption> buffer = new List<InteractionOption>();
        provider.AppendOptions(request.Type, buffer);

        for (int i = 0; i < buffer.Count; ++i)
        {
            if (buffer[i].OptionId != request.OptionId || buffer[i].ActorEffect == null)
                continue;

            StatEffect e = buffer[i].ActorEffect;
            stat.Health = Mathf.Clamp(stat.Health + e.HealthDelta, 0f, stat.HealthMax);
            stat.Hunger = Mathf.Clamp(stat.Hunger + e.HungerDelta, 0f, stat.HungerMax);
            stat.Thirst = Mathf.Clamp(stat.Thirst + e.ThirstDelta, 0f, stat.ThirstMax);
            stat.Fatigue = Mathf.Clamp(stat.Fatigue + e.FatigueDelta, 0f, stat.FatigueMax);
            return true;
        }

        return false;
    }

    // ---- General ----

    private void RunGeneralScenarios()
    {
        DestinationDecider decider = CreateDecider();
        StatEffect workCost = CreateFarmingCost();

        // Zero max values must not produce NaN/Infinity or an exception.
        ProbeStat zeroed = NewStat(0f, 0f, 0f);
        zeroed.HealthMax = 0f;
        zeroed.HungerMax = 0f;
        zeroed.ThirstMax = 0f;
        zeroed.FatigueMax = 0f;
        NPCDecision zeroDecision = Decide(decider, zeroed, NPCType.Farmer, Vector3.zero, workCost);
        Check("General 5", "zero max stats still produce a finite, valid decision",
            IsFinite(zeroDecision.DestinationPos), zeroDecision);

        // No destination/provider at all: safe Idle, no exception and no spin.
        DestinationDecider emptyDecider = new DestinationDecider();
        emptyDecider.Init(null, _decisionTuning);
        Vector3 probePos = new Vector3(5f, 7f, 0f);
        NPCDecision emptyDecision = emptyDecider.Decide(NewStat(50f, 50f, 50f), NPCType.Farmer, probePos, workCost);
        Check("General 7", "missing destination database falls back to a safe Idle",
            emptyDecision.Intent == NPCIntent.Idle
            && emptyDecision.DestinationKey == BuildingType.None
            && Approximately(emptyDecision.DestinationPos, probePos), emptyDecision);

        RunHealthPenaltyScenario(decider, workCost);
        RunEqualEffectObservation(decider, workCost);
    }

    /// <summary>
    /// Only asserts when the scene data actually contains both a health-damaging drink and a
    /// non-damaging alternative. Otherwise it reports the gap instead of claiming a pass.
    /// </summary>
    private void RunHealthPenaltyScenario(DestinationDecider decider, StatEffect workCost)
    {
        if (!TryFindDamagingDrink(out BuildingType key, out int damagingItemId, out bool hasSafeAlternative))
        {
            Skip("General 4", "scene item data has no health-damaging drink to test against");
            return;
        }

        if (!hasSafeAlternative)
        {
            Skip("General 4", "scene item data has a damaging drink but no non-damaging alternative to prefer");
            return;
        }

        if (!TryGetPos(key, out Vector3 pos))
        {
            Skip("General 4", "the damaging drink's building has no registered destination");
            return;
        }

        // Thirsty but at full health: the health penalty must outweigh the extra thirst relief.
        NPCDecision d = Decide(decider, NewStat(20f, 20f, 99f), NPCType.Farmer, pos, workCost);
        bool pickedDamaging = d.Request.HasValue && d.Request.Value.OptionId == damagingItemId;
        Check("General 4", "health-damaging supply is rejected while a safe alternative exists",
            !pickedDamaging, d);
    }

    /// <summary>
    /// Observation only. The probe cannot construct two candidates with a forced identical
    /// score, so this reports what the existing data happens to produce rather than passing.
    /// </summary>
    private void RunEqualEffectObservation(DestinationDecider decider, StatEffect workCost)
    {
        if (!TryFindEqualEffectDrinkPair(out BuildingType key, out int lowerId, out int higherId))
        {
            _report.AppendLine("  OBS  General 1 - scene item data has no two identical drink effects to observe");
            return;
        }

        if (!TryGetPos(key, out Vector3 pos))
            return;

        NPCDecision d = Decide(decider, NewStat(20f, 20f, 90f), NPCType.Farmer, pos, workCost);
        int chosen = d.Request.HasValue ? d.Request.Value.OptionId : -1;

        _report.Append("  OBS  General 1 - identical drink effects ").Append(lowerId).Append(" and ").Append(higherId)
            .Append(" at ").Append(key).Append("; decider chose item ").Append(chosen)
            .AppendLine(" (observation only, not a forced score tie)");
    }

    // ---- Scene data inspection ----

    private bool TryFindDamagingDrink(out BuildingType key, out int itemId, out bool hasSafeAlternative)
    {
        key = BuildingType.None;
        itemId = -1;
        hasSafeAlternative = false;

        List<InteractionOption> buffer = new List<InteractionOption>();
        IReadOnlyList<BuildingType> keys = _destinationDB.RegisteredKeys;

        for (int i = 0; i < keys.Count; ++i)
        {
            if (!_destinationDB.TryGetInteractionProvider(keys[i], ActionType.Drink, out IInteractionProvider provider))
                continue;

            buffer.Clear();
            provider.AppendOptions(ActionType.Drink, buffer);

            for (int o = 0; o < buffer.Count; ++o)
            {
                if (buffer[o].ActorEffect == null)
                    continue;

                if (buffer[o].ActorEffect.HealthDelta < 0f && itemId < 0)
                {
                    key = keys[i];
                    itemId = buffer[o].OptionId;
                }
                else if (buffer[o].ActorEffect.HealthDelta >= 0f && buffer[o].ActorEffect.ThirstDelta < 0f)
                {
                    hasSafeAlternative = true;
                }
            }
        }

        return itemId >= 0;
    }

    private bool TryFindEqualEffectDrinkPair(out BuildingType key, out int lowerId, out int higherId)
    {
        key = BuildingType.None;
        lowerId = -1;
        higherId = -1;

        List<InteractionOption> buffer = new List<InteractionOption>();
        IReadOnlyList<BuildingType> keys = _destinationDB.RegisteredKeys;

        for (int i = 0; i < keys.Count; ++i)
        {
            if (!_destinationDB.TryGetInteractionProvider(keys[i], ActionType.Drink, out IInteractionProvider provider))
                continue;

            buffer.Clear();
            provider.AppendOptions(ActionType.Drink, buffer);

            for (int a = 0; a < buffer.Count; ++a)
            {
                for (int b = a + 1; b < buffer.Count; ++b)
                {
                    if (!SameEffect(buffer[a].ActorEffect, buffer[b].ActorEffect))
                        continue;

                    key = keys[i];
                    lowerId = Mathf.Min(buffer[a].OptionId, buffer[b].OptionId);
                    higherId = Mathf.Max(buffer[a].OptionId, buffer[b].OptionId);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool SameEffect(StatEffect a, StatEffect b)
    {
        if (a == null || b == null)
            return false;

        return Mathf.Approximately(a.HealthDelta, b.HealthDelta)
            && Mathf.Approximately(a.HungerDelta, b.HungerDelta)
            && Mathf.Approximately(a.ThirstDelta, b.ThirstDelta)
            && Mathf.Approximately(a.FatigueDelta, b.FatigueDelta);
    }

    // ---- Reporting ----

    private void Check(string id, string description, bool passed, NPCDecision decision)
    {
        _currentRunResults.Add(Describe(id, decision));

        if (_recordOnly)
            return;

        if (passed)
            _passCount++;
        else
            _failCount++;

        _report.Append(passed ? "  PASS " : "  FAIL ").Append(id).Append(" - ").Append(description)
            .Append("  -> ").AppendLine(Describe(decision));
    }

    private void Skip(string id, string reason)
    {
        if (_recordOnly)
            return;

        _skipCount++;
        _report.Append("  SKIP ").Append(id).Append(" - ").AppendLine(reason);
    }

    private void CheckDeterminism()
    {
        bool identical = _firstRunResults.Count == _currentRunResults.Count;

        if (identical)
        {
            for (int i = 0; i < _firstRunResults.Count; ++i)
            {
                if (_firstRunResults[i] == _currentRunResults[i])
                    continue;

                identical = false;
                _report.Append("  FAIL determinism - run 1: ").Append(_firstRunResults[i])
                    .Append(" | run 2: ").AppendLine(_currentRunResults[i]);
                break;
            }
        }

        if (identical)
        {
            _passCount++;
            _report.Append("  PASS Determinism - ").Append(_firstRunResults.Count)
                .AppendLine(" decisions identical across two identical runs");
        }
        else
        {
            _failCount++;
        }
    }

    private void AppendNotVerifiedSection()
    {
        _report.AppendLine("  --- NOT VERIFIED by this probe (do not report as passing) ---");
        _report.AppendLine("  General 1 - a forced identical-score candidate pair needs an arbitrary provider");
        _report.AppendLine("  General 2 - a farther high-effect vs nearer adequate item pair needs arbitrary item placement");
        _report.AppendLine("  General 3 - isolating every StatEffect field needs arbitrary supply effects");
        _report.AppendLine("  General 6 - LookAheadDepth 1 vs 2 and extreme tuning: set the value on");
        _report.AppendLine("             NPCDecisionTuning.asset in the Inspector and re-run this probe by hand");
        _report.AppendLine("             (runtime ScriptableObject mutation is forbidden by ARCHITECTURE.md 6.4)");
    }

    private static string Describe(string id, NPCDecision d)
    {
        return $"{id}:{d.Intent}/{d.DestinationKey}/{d.RepeatCount}/{(d.Request.HasValue ? d.Request.Value.OptionId : -1)}/{d.DestinationPos:F3}";
    }

    private static string Describe(NPCDecision d)
    {
        return $"{d.Intent} key={d.DestinationKey} xN={d.RepeatCount} item={(d.Request.HasValue ? d.Request.Value.OptionId : -1)}";
    }

    // ---- Helpers ----

    private DestinationDecider CreateDecider()
    {
        DestinationDecider decider = new DestinationDecider();
        decider.Init(_destinationDB, _decisionTuning);
        return decider;
    }

    private StatEffect CreateFarmingCost()
    {
        return new StatEffect(
            hungerDelta: _farmingActionCost.FarmingActionPerHunger,
            thirstDelta: _farmingActionCost.FarmingActionPerThirst,
            fatigueDelta: _farmingActionCost.FarmingActionPerFatigue);
    }

    private StatEffect CreateGuardDutyCost()
    {
        float seconds = _decisionTuning.GuardDutyEvaluationSeconds;

        return new StatEffect(
            hungerDelta: _guardActionCost.HungerPerSecond * seconds,
            thirstDelta: _guardActionCost.ThirstPerSecond * seconds,
            fatigueDelta: _guardActionCost.FatiguePerSecond * seconds);
    }

    private bool TryGetPos(BuildingType key, out Vector3 pos)
    {
        return _destinationDB.TryGetDestinationPos(key, out pos);
    }

    private Vector3 FarFrom(Vector3 origin)
    {
        return origin + new Vector3(_farDistance, _farDistance, 0f);
    }

    private static NPCDecision Decide(DestinationDecider decider, ProbeStat stat, NPCType npcType, Vector3 pos, StatEffect workCost)
    {
        return decider.Decide(stat, npcType, pos, workCost);
    }

    private static ProbeStat NewStat(float fatigue, float hunger, float thirst)
    {
        return new ProbeStat
        {
            Fatigue = fatigue,
            Hunger = hunger,
            Thirst = thirst,
        };
    }

    private static bool IsSupply(NPCIntent intent)
    {
        return intent == NPCIntent.Eat || intent == NPCIntent.Drink || intent == NPCIntent.Sleep;
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude <= 0.0001f;
    }

    private static bool IsFinite(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsInfinity(v.x)
            && !float.IsNaN(v.y) && !float.IsInfinity(v.y)
            && !float.IsNaN(v.z) && !float.IsInfinity(v.z);
    }

    /// <summary>
    /// Fully controlled stat view. Plain C# so scenarios can set any value, including the
    /// zero-max edge cases a real NPCStat would clamp away.
    /// </summary>
    private sealed class ProbeStat : IStatView
    {
        public float Health = 100f;
        public float HealthMax = 100f;
        public float MoveSpeed = 3f;
        public float Fatigue;
        public float Hunger;
        public float Thirst;
        public float FatigueMax = 100f;
        public float HungerMax = 100f;
        public float ThirstMax = 100f;

        public float GetCurrentHealth => Health;
        public float GetMaxHealth => HealthMax;
        public float GetMoveSpeed => MoveSpeed;
        public float GetFatigue => Fatigue;
        public float GetHunger => Hunger;
        public float GetThirst => Thirst;
        public float GetFatigueMax => FatigueMax;
        public float GetHungerMax => HungerMax;
        public float GetThirstMax => ThirstMax;
    }
}
