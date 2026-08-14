using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Job-neutral NPC decision layer. Chooses exactly one destination and one concrete
/// action for the caller to turn into a queue (or Work for a bounded repeat batch).
/// See PublicMD/NPC_Decision_System_Plan.md for background on the job-neutral design,
/// and PublicMD/DestinationDecider_Refactor_Plan.md for why the previous multi-destination
/// greedy chain and category-only Eat/Drink prediction were replaced with single-step,
/// item-accurate decisions.
/// </summary>
public class DestinationDecider
{
    private const float MinMoveSpeed = 0.01f;

    private DestinationDB _destinationDB;
    private NPCDecisionTuning _tuning;

    private struct NeedSnapshot
    {
        public float Health;
        public float HealthMax;
        public float Fatigue;
        public float FatigueMax;
        public float Hunger;
        public float HungerMax;
        public float Thirst;
        public float ThirstMax;
    }

    private struct Candidate
    {
        public NPCIntent Intent;
        public BuildingType Key;
        public Vector3 Pos;
        public ActionType ActionType;
        public int ItemId;
        public int RepeatCount;
        public float TravelTime;
        public NeedSnapshot After;
        public float Utility;
    }

    private readonly List<InteractionOption> _optionBuffer = new List<InteractionOption>();
    private readonly List<Candidate> _supplyCandidates = new List<Candidate>();

    public void Init(DestinationDB destinationDB, NPCDecisionTuning tuning)
    {
        _destinationDB = destinationDB;
        _tuning = tuning;
    }

    public NPCDecision Decide(IStatView stat, NPCType npcType, Vector3 npcLoc, StatEffect workCost)
    {
        if (!_destinationDB || !_tuning || stat == null)
            return NPCDecision.Idle(npcLoc);

        NeedSnapshot before = ToSnapshot(stat);
        float moveSpeed = stat.GetMoveSpeed;

        _supplyCandidates.Clear();
        BuildSupplyCandidates(before, npcLoc, moveSpeed);

        bool hasWorkCost = workCost != null;
        int safeWorkBefore = 0;
        NeedSnapshot afterWorkBatch = before;
        if (hasWorkCost)
            safeWorkBefore = EstimateWorkCount(before, workCost, out afterWorkBatch);

        ScoreSupplyCandidates(before, workCost, hasWorkCost, safeWorkBefore);

        bool[] criticalMask =
        {
            Normalize(before.Fatigue, before.FatigueMax) > _tuning.CriticalNeedThreshold,
            Normalize(before.Hunger, before.HungerMax) > _tuning.CriticalNeedThreshold,
            Normalize(before.Thirst, before.ThirstMax) > _tuning.CriticalNeedThreshold,
        };
        bool isCritical = criticalMask[0] || criticalMask[1] || criticalMask[2];

        if (isCritical)
        {
            return TryPickCriticalCandidate(before, criticalMask, out Candidate picked)
                ? ToDecision(picked)
                : NPCDecision.Idle(npcLoc);
        }

        Candidate workCandidate = default;
        bool hasWork = hasWorkCost && TryBuildWorkCandidate(
            npcType, npcLoc, moveSpeed, before, afterWorkBatch, safeWorkBefore, workCost, out workCandidate);
        bool hasSupply = TryPickBestSupply(out Candidate bestSupply);

        if (hasWork && hasSupply)
        {
            return bestSupply.Utility >= workCandidate.Utility + _tuning.SwitchMargin
                ? ToDecision(bestSupply)
                : ToDecision(workCandidate);
        }

        if (hasWork)
            return ToDecision(workCandidate);

        if (hasSupply)
            return ToDecision(bestSupply);

        return NPCDecision.Idle(npcLoc);
    }

    // ---- Candidate construction ----

    private void BuildSupplyCandidates(NeedSnapshot before, Vector3 npcLoc, float moveSpeed)
    {
        IReadOnlyList<BuildingType> keys = _destinationDB.RegisteredKeys;

        for (int i = 0; i < keys.Count; ++i)
        {
            BuildingType key = keys[i];

            if (!_destinationDB.TryGetDestinationPos(key, out Vector3 pos))
                continue;

            float travelTime = GetTravelTime(npcLoc, pos, moveSpeed);

            if (_destinationDB.TryGetInteractionProvider(key, out IInteractionProvider provider))
            {
                AddItemCandidates(provider, ActionType.Eat, key, pos, travelTime, before);
                AddItemCandidates(provider, ActionType.Drink, key, pos, travelTime, before);
            }

            // TODO: Inn 전용 BaseInteractable이 생기면 SleepAction과 마찬가지로 provider 기반으로 옮긴다.
            if (key == BuildingType.Inn)
                AddSleepCandidate(key, pos, travelTime, before);
        }
    }

    private void AddItemCandidates(IInteractionProvider provider, ActionType type, BuildingType key, Vector3 pos, float travelTime, NeedSnapshot before)
    {
        if (!provider.CanInteract(type))
            return;

        _optionBuffer.Clear();
        provider.AppendOptions(type, _optionBuffer);

        for (int i = 0; i < _optionBuffer.Count; ++i)
        {
            InteractionOption option = _optionBuffer[i];

            _supplyCandidates.Add(new Candidate
            {
                Intent = type == ActionType.Eat ? NPCIntent.Eat : NPCIntent.Drink,
                Key = key,
                Pos = pos,
                ActionType = type,
                ItemId = option.ItemId,
                RepeatCount = 1,
                TravelTime = travelTime,
                After = ApplyEffect(before, option.Effect),
            });
        }
    }

    private void AddSleepCandidate(BuildingType key, Vector3 pos, float travelTime, NeedSnapshot before)
    {
        // Mirrors SleepAction's own full-recovery calculation (stat.ChangeFatigue(-stat.GetFatigue)).
        StatEffect sleepEffect = new StatEffect(fatigueDelta: -before.Fatigue);

        _supplyCandidates.Add(new Candidate
        {
            Intent = NPCIntent.Sleep,
            Key = key,
            Pos = pos,
            ActionType = ActionType.Sleep,
            ItemId = -1,
            RepeatCount = 1,
            TravelTime = travelTime,
            After = ApplyEffect(before, sleepEffect),
        });
    }

    private void ScoreSupplyCandidates(NeedSnapshot before, StatEffect workCost, bool hasWorkCost, int safeWorkBefore)
    {
        for (int i = 0; i < _supplyCandidates.Count; ++i)
        {
            Candidate c = _supplyCandidates[i];
            int safeWorkAfter = hasWorkCost ? EstimateWorkCount(c.After, workCost, out _) : 0;
            c.Utility = ComputeUtility(before, c, safeWorkBefore, safeWorkAfter, 0);
            _supplyCandidates[i] = c;
        }
    }

    private bool TryBuildWorkCandidate(NPCType npcType, Vector3 npcLoc, float moveSpeed, NeedSnapshot before,
        NeedSnapshot afterWorkBatch, int safeWorkBefore, StatEffect workCost, out Candidate candidate)
    {
        candidate = default;

        if (safeWorkBefore < _tuning.MinimumWorkBatch)
            return false;

        BuildingType workKey = GetWorkPlaceKey(npcType);
        if (workKey == BuildingType.None)
            return false;

        if (!_destinationDB.TryGetDestinationPos(workKey, out Vector3 workPos))
            return false;

        float travelTime = GetTravelTime(npcLoc, workPos, moveSpeed);
        int safeWorkAfter = EstimateWorkCount(afterWorkBatch, workCost, out _);

        candidate = new Candidate
        {
            Intent = NPCIntent.Work,
            Key = workKey,
            Pos = workPos,
            ActionType = ActionType.Farming,
            ItemId = -1,
            RepeatCount = safeWorkBefore,
            TravelTime = travelTime,
            After = afterWorkBatch,
        };
        candidate.Utility = ComputeUtility(before, candidate, safeWorkBefore, safeWorkAfter, safeWorkBefore);
        return true;
    }

    // ---- Selection ----

    private bool TryPickCriticalCandidate(NeedSnapshot before, bool[] criticalMask, out Candidate picked)
    {
        picked = default;
        bool found = false;

        // Tier 1: strictly safe - none of the critical needs worsen, at least one improves.
        for (int i = 0; i < _supplyCandidates.Count; ++i)
        {
            Candidate c = _supplyCandidates[i];
            if (!IsStrictlySafeImprovement(before, c.After, criticalMask))
                continue;

            if (!found || CompareForSelection(c, picked) < 0)
            {
                picked = c;
                found = true;
            }
        }
        if (found)
            return true;

        // Tier 2 fallback: the worst critical need's peak value strictly decreases.
        float beforeMax = MaxCriticalNeed(before, criticalMask);
        for (int i = 0; i < _supplyCandidates.Count; ++i)
        {
            Candidate c = _supplyCandidates[i];
            if (MaxCriticalNeed(c.After, criticalMask) >= beforeMax)
                continue;

            if (!found || CompareForSelection(c, picked) < 0)
            {
                picked = c;
                found = true;
            }
        }
        if (found)
            return true;

        // Tier 3 fallback: peak doesn't get worse, and total critical risk decreases.
        float beforeRiskSum = SumCriticalRisk(before, criticalMask);
        for (int i = 0; i < _supplyCandidates.Count; ++i)
        {
            Candidate c = _supplyCandidates[i];
            if (MaxCriticalNeed(c.After, criticalMask) > beforeMax)
                continue;
            if (SumCriticalRisk(c.After, criticalMask) >= beforeRiskSum)
                continue;

            if (!found || CompareForSelection(c, picked) < 0)
            {
                picked = c;
                found = true;
            }
        }

        return found;
    }

    private bool TryPickBestSupply(out Candidate best)
    {
        best = default;
        bool found = false;

        for (int i = 0; i < _supplyCandidates.Count; ++i)
        {
            Candidate c = _supplyCandidates[i];
            if (!found || CompareForSelection(c, best) < 0)
            {
                best = c;
                found = true;
            }
        }

        return found;
    }

    // Ascending: lower is better. utility desc -> travelTime asc -> ItemId asc -> ActionType asc -> BuildingType asc.
    private static int CompareForSelection(Candidate a, Candidate b)
    {
        int byUtility = b.Utility.CompareTo(a.Utility);
        if (byUtility != 0)
            return byUtility;

        int byTravel = a.TravelTime.CompareTo(b.TravelTime);
        if (byTravel != 0)
            return byTravel;

        int byItem = a.ItemId.CompareTo(b.ItemId);
        if (byItem != 0)
            return byItem;

        int byAction = ((int)a.ActionType).CompareTo((int)b.ActionType);
        if (byAction != 0)
            return byAction;

        return ((int)a.Key).CompareTo((int)b.Key);
    }

    private static NPCDecision ToDecision(Candidate c)
    {
        InteractRequest? request = c.ItemId >= 0 ? new InteractRequest(c.ActionType, c.ItemId) : (InteractRequest?)null;
        return new NPCDecision(c.Intent, c.Key, c.Pos, c.RepeatCount, request);
    }

    // ---- Work batch simulation ----

    private int EstimateWorkCount(NeedSnapshot snapshot, StatEffect workCost, out NeedSnapshot finalState)
    {
        int count = 0;
        NeedSnapshot state = snapshot;

        while (count < _tuning.MaximumWorkBatch)
        {
            NeedSnapshot next = ApplyEffect(state, workCost);
            if (IsAnyNeedAboveCritical(next))
                break;

            state = next;
            count++;
        }

        finalState = state;
        return count;
    }

    private bool IsAnyNeedAboveCritical(NeedSnapshot s)
    {
        return Normalize(s.Fatigue, s.FatigueMax) > _tuning.CriticalNeedThreshold
            || Normalize(s.Hunger, s.HungerMax) > _tuning.CriticalNeedThreshold
            || Normalize(s.Thirst, s.ThirstMax) > _tuning.CriticalNeedThreshold;
    }

    // ---- Need snapshot math ----

    private static NeedSnapshot ToSnapshot(IStatView stat)
    {
        return new NeedSnapshot
        {
            Health = stat.GetCurrentHealth,
            HealthMax = stat.GetMaxHealth,
            Fatigue = stat.GetFatigue,
            FatigueMax = stat.GetFatigueMax,
            Hunger = stat.GetHunger,
            HungerMax = stat.GetHungerMax,
            Thirst = stat.GetThirst,
            ThirstMax = stat.GetThirstMax,
        };
    }

    // Mirrors NPCStat.ApplyStatEffect's order and clamp rules exactly, on a snapshot copy.
    // MoodDelta is intentionally ignored: NPCStat has no mood field yet.
    private static NeedSnapshot ApplyEffect(NeedSnapshot s, StatEffect effect)
    {
        s.Health = Mathf.Clamp(s.Health + effect.HealthDelta, 0f, s.HealthMax);
        s.Hunger = Mathf.Clamp(s.Hunger + effect.HungerDelta, 0f, s.HungerMax);
        s.Thirst = Mathf.Clamp(s.Thirst + effect.ThirstDelta, 0f, s.ThirstMax);
        s.Fatigue = Mathf.Clamp(s.Fatigue + effect.FatigueDelta, 0f, s.FatigueMax);
        return s;
    }

    private static float Normalize(float value, float max)
    {
        return max <= 0f ? 0f : Mathf.Clamp01(value / max);
    }

    private static float NormalizedNeed(NeedSnapshot s, int criticalIndex)
    {
        switch (criticalIndex)
        {
            case 0: return Normalize(s.Fatigue, s.FatigueMax);
            case 1: return Normalize(s.Hunger, s.HungerMax);
            default: return Normalize(s.Thirst, s.ThirstMax);
        }
    }

    private static float MaxCriticalNeed(NeedSnapshot s, bool[] criticalMask)
    {
        float max = 0f;
        for (int i = 0; i < criticalMask.Length; ++i)
        {
            if (!criticalMask[i])
                continue;

            float v = NormalizedNeed(s, i);
            if (v > max)
                max = v;
        }
        return max;
    }

    private float SumCriticalRisk(NeedSnapshot s, bool[] criticalMask)
    {
        float sum = 0f;
        for (int i = 0; i < criticalMask.Length; ++i)
        {
            if (!criticalMask[i])
                continue;

            sum += NeedRisk(NormalizedNeed(s, i));
        }
        return sum;
    }

    private static bool IsStrictlySafeImprovement(NeedSnapshot before, NeedSnapshot after, bool[] criticalMask)
    {
        bool improvedAny = false;
        for (int i = 0; i < criticalMask.Length; ++i)
        {
            if (!criticalMask[i])
                continue;

            float b = NormalizedNeed(before, i);
            float a = NormalizedNeed(after, i);
            if (a > b)
                return false;
            if (a < b)
                improvedAny = true;
        }
        return improvedAny;
    }

    // ---- Risk / utility ----

    private float NeedRisk(float n)
    {
        if (n <= _tuning.DangerThreshold)
            return n;

        float over = n - _tuning.DangerThreshold;
        return n + over * over * _tuning.DangerPenaltyMultiplier;
    }

    private float ComputeRisk(NeedSnapshot s)
    {
        return NeedRisk(Normalize(s.Fatigue, s.FatigueMax))
             + NeedRisk(Normalize(s.Hunger, s.HungerMax))
             + NeedRisk(Normalize(s.Thirst, s.ThirstMax))
             + (1f - Normalize(s.Health, s.HealthMax)) * _tuning.HealthWeight;
    }

    private float ComputeUtility(NeedSnapshot before, Candidate c, int safeWorkBefore, int safeWorkAfter, int producedWorkCount)
    {
        float riskBefore = ComputeRisk(before);
        float riskAfter = ComputeRisk(c.After);

        return (riskBefore - riskAfter) * _tuning.RiskWeight
             + (safeWorkAfter - safeWorkBefore) * _tuning.WorkCapacityWeight
             + producedWorkCount * _tuning.WorkValue
             - c.TravelTime * _tuning.TravelWeight;
    }

    // ---- Misc ----

    private static BuildingType GetWorkPlaceKey(NPCType npcType)
    {
        switch (npcType)
        {
            case NPCType.Farmer:
                return BuildingType.Farm;
            default:
                // TODO: Guard/Cook 등 다른 직업의 작업장 키가 정해지면 매핑을 추가한다.
                return BuildingType.None;
        }
    }

    private static float GetTravelTime(Vector3 from, Vector3 to, float moveSpeed)
    {
        float distance = Vector2.Distance(from, to);
        float speed = Mathf.Max(MinMoveSpeed, moveSpeed);
        return distance / speed;
    }
}
