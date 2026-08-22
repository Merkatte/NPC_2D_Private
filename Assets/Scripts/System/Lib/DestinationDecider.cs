using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Job-neutral NPC decision layer. Chooses exactly one destination and one concrete
/// action for the caller to turn into a queue (or Work for a bounded repeat batch).
/// See PublicMD/NPC_Decision_System_Plan.md for background on the job-neutral design,
/// PublicMD/DestinationDecider_Refactor_Plan.md for why the previous multi-destination
/// greedy chain and category-only Eat/Drink prediction were replaced with single-step,
/// item-accurate decisions, and PublicMD/DestinationDecider_Rational_Utility_Plan.md for
/// the nonlinear risk curve and bounded look-ahead that replaced the one-step utility.
///
/// Every candidate - supply, role activity and the Idle floor - is scored in one comparable
/// model after hard safety filtering. There is no satisfaction/recovery exit threshold: the
/// nonlinear risk curve alone makes further recovery worth less and less.
/// </summary>
public class DestinationDecider
{
    private const float MinMoveSpeed = 0.01f;

    // Scores from different evaluation paths can differ by float noise. Treating anything
    // inside this band as a tie keeps the deterministic tie-break rules in charge. This is
    // an algorithm invariant, not a balance value, so it stays in code rather than tuning.
    private const float ScoreEpsilon = 0.001f;

    // Critical-need bit positions, matching NormalizedNeed's index order.
    private const int CriticalBitFatigue = 1 << 0;
    private const int CriticalBitHunger = 1 << 1;
    private const int CriticalBitThirst = 1 << 2;
    private const int CriticalNeedCount = 3;

    private DestinationDB _destinationDB;
    private NPCDecisionTuning _tuning;

    private enum CandidateKind
    {
        Idle,
        Supply,
        FarmerWork,
        GuardDuty,
    }

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

    /// <summary>
    /// A plain prediction record. Never holds an IAction, a queue or any runtime reference.
    /// </summary>
    private struct Candidate
    {
        public CandidateKind Kind;
        public NPCIntent Intent;
        public BuildingType Key;
        public Vector3 Position;
        public ActionType ActionType;
        public int OptionId;
        public int RepeatCount;
        public float TravelTime;
        public float ActionTime;
        public float ActivityReward;
        public NeedSnapshot After;
        public float Score;
    }

    private readonly List<InteractionOption> _optionBuffer = new List<InteractionOption>();

    // One candidate list per look-ahead level so recursive branches never share mutable
    // candidate state. _optionBuffer stays shared because a level's candidate list is fully
    // built before any recursion into the next level begins.
    private readonly List<Candidate>[] _levelCandidates =
        new List<Candidate>[NPCDecisionTuning.MaxLookAheadDepth];

    private readonly StringBuilder _traceBuilder = new StringBuilder();

    public DestinationDecider()
    {
        for (int i = 0; i < _levelCandidates.Length; ++i)
            _levelCandidates[i] = new List<Candidate>();
    }

    public void Init(DestinationDB destinationDB, NPCDecisionTuning tuning)
    {
        _destinationDB = destinationDB;
        _tuning = tuning;
    }

    /// <summary>
    /// Returns one semantic step: one destination plus one thing to do there (Work may repeat
    /// up to RepeatCount times). Predicted follow-up actions are never returned.
    /// </summary>
    /// <param name="workCost">
    /// Role activity cost, and its unit depends on <paramref name="npcType"/>:
    /// for Farmer it is the authoritative cost of ONE Farming action, while for Guard it is
    /// the cost of one Guard duty EVALUATION slice - an approximation used only to price
    /// "keep standing guard" in the same units as the other candidates. It is not a runtime
    /// replan period and not an authoritative duration; GuardAction keeps running until enemy
    /// detection or GuardActionCost.ShouldInterrupt(...) fires. Pass null when the role has no
    /// activity to offer.
    /// </param>
    public NPCDecision Decide(IStatView stat, NPCType npcType, Vector3 npcLoc, StatEffect workCost)
    {
        if (!_destinationDB || !_tuning || stat == null)
            return NPCDecision.Idle(npcLoc);

        NeedSnapshot before = ToSnapshot(stat);
        float moveSpeed = stat.GetMoveSpeed;
        int depth = Mathf.Clamp(_tuning.LookAheadDepth, 1, NPCDecisionTuning.MaxLookAheadDepth);

        List<Candidate> roots = BuildCandidates(before, npcLoc, moveSpeed, npcType, workCost, 0);
        ScoreCandidates(roots, before, moveSpeed, npcType, workCost, depth, 0);

        if (!TryPickBest(roots, out int bestIndex))
            return NPCDecision.Idle(npcLoc);

        LogDecisionTrace(before, npcLoc, npcType, roots, bestIndex);
        return ToDecision(roots[bestIndex]);
    }

    // ---- Bounded evaluation ----

    /// <summary>
    /// Value of being in <paramref name="state"/> at <paramref name="pos"/> with
    /// <paramref name="remainingDepth"/> further decisions available. At depth 0 only the
    /// terminal state value remains.
    /// </summary>
    private float EvaluateNode(NeedSnapshot state, Vector3 pos, float moveSpeed, NPCType npcType,
        StatEffect workCost, int remainingDepth, int level)
    {
        if (remainingDepth <= 0 || level >= _levelCandidates.Length)
            return TerminalValue(state);

        List<Candidate> candidates = BuildCandidates(state, pos, moveSpeed, npcType, workCost, level);
        if (candidates.Count == 0)
            return TerminalValue(state);

        ScoreCandidates(candidates, state, moveSpeed, npcType, workCost, remainingDepth, level);

        return TryPickBest(candidates, out int bestIndex)
            ? candidates[bestIndex].Score
            : TerminalValue(state);
    }

    private void ScoreCandidates(List<Candidate> candidates, NeedSnapshot state, float moveSpeed,
        NPCType npcType, StatEffect workCost, int remainingDepth, int level)
    {
        float riskBefore = ComputeRisk(state);

        for (int i = 0; i < candidates.Count; ++i)
        {
            Candidate c = candidates[i];

            float future = EvaluateNode(c.After, c.Position, moveSpeed, npcType, workCost,
                remainingDepth - 1, level + 1);

            c.Score = ComputeImmediateScore(riskBefore, c) + _tuning.FutureDiscount * future;
            candidates[i] = c;
        }
    }

    /// <summary>
    /// Units: ActivityReward is an abstract value score. TravelTime/ActionTime are seconds
    /// converted to opportunity cost by their weights. riskExposure is (risk x seconds), the
    /// cost of spending time in a dangerous state. Every term after the reward is subtracted,
    /// so a higher score always means a better candidate.
    /// </summary>
    private float ComputeImmediateScore(float riskBefore, Candidate c)
    {
        float elapsed = c.TravelTime + c.ActionTime;
        float riskAfter = ComputeRisk(c.After);
        float riskExposure = (riskBefore + riskAfter) * 0.5f * elapsed * _tuning.RiskExposureWeight;

        return c.ActivityReward
             - c.TravelTime * _tuning.TravelWeight
             - c.ActionTime * _tuning.ActionTimeWeight
             - riskExposure;
    }

    // Negative because risk is bad: a safer predicted end state scores higher.
    private float TerminalValue(NeedSnapshot state)
    {
        return -ComputeRisk(state) * _tuning.TerminalRiskWeight;
    }

    // ---- Candidate construction ----

    private List<Candidate> BuildCandidates(NeedSnapshot state, Vector3 pos, float moveSpeed,
        NPCType npcType, StatEffect workCost, int level)
    {
        List<Candidate> candidates = _levelCandidates[level];
        candidates.Clear();

        // Idle floor: always valid, zero reward. Keeps the candidate set non-empty so no
        // caller ever has to special-case "nothing to do".
        candidates.Add(new Candidate
        {
            Kind = CandidateKind.Idle,
            Intent = NPCIntent.Idle,
            Key = BuildingType.None,
            Position = pos,
            ActionType = ActionType.Idle,
            OptionId = -1,
            RepeatCount = 1,
            TravelTime = 0f,
            ActionTime = _tuning.EstimatedIdleSeconds,
            ActivityReward = 0f,
            After = state,
        });

        AddSupplyCandidates(candidates, state, pos, moveSpeed);

        int criticalMask = BuildCriticalMask(state);
        if (criticalMask != 0)
        {
            // Hard safety gate: role activity is invalid while any need is critical, and the
            // remaining supply options are narrowed to the safest available tier.
            ApplyCriticalFilter(candidates, state, criticalMask);
        }
        else
        {
            AddRoleCandidate(candidates, state, pos, moveSpeed, npcType, workCost);
        }

        return candidates;
    }

    private void AddSupplyCandidates(List<Candidate> candidates, NeedSnapshot state, Vector3 pos, float moveSpeed)
    {
        IReadOnlyList<BuildingType> keys = _destinationDB.RegisteredKeys;

        for (int i = 0; i < keys.Count; ++i)
        {
            BuildingType key = keys[i];

            if (!_destinationDB.TryGetDestinationPos(key, out Vector3 destinationPos))
                continue;

            float travelTime = GetTravelTime(pos, destinationPos, moveSpeed);

            AddItemCandidatesForType(candidates, key, ActionType.Eat, destinationPos, travelTime, state);
            AddItemCandidatesForType(candidates, key, ActionType.Drink, destinationPos, travelTime, state);

            // TODO: Inn 전용 provider가 생기면 SleepAction과 마찬가지로 provider 기반으로 옮긴다.
            if (key == BuildingType.Inn)
                AddSleepCandidate(candidates, key, destinationPos, travelTime, state);
        }
    }

    private void AddItemCandidatesForType(List<Candidate> candidates, BuildingType key, ActionType type,
        Vector3 pos, float travelTime, NeedSnapshot state)
    {
        if (!_destinationDB.TryGetInteractionProvider(key, type, out IInteractionProvider provider))
            return;

        _optionBuffer.Clear();
        provider.AppendOptions(type, _optionBuffer);

        float actionTime = type == ActionType.Eat ? _tuning.EstimatedEatSeconds : _tuning.EstimatedDrinkSeconds;

        for (int i = 0; i < _optionBuffer.Count; ++i)
        {
            InteractionOption option = _optionBuffer[i];

            candidates.Add(new Candidate
            {
                Kind = CandidateKind.Supply,
                Intent = type == ActionType.Eat ? NPCIntent.Eat : NPCIntent.Drink,
                Key = key,
                Position = pos,
                ActionType = type,
                OptionId = option.OptionId,
                RepeatCount = 1,
                TravelTime = travelTime,
                ActionTime = actionTime,
                ActivityReward = 0f,
                After = ApplyEffect(state, option.ActorEffect),
            });
        }
    }

    private void AddSleepCandidate(List<Candidate> candidates, BuildingType key, Vector3 pos, float travelTime, NeedSnapshot state)
    {
        // Mirrors SleepAction's own full-recovery calculation (stat.ChangeFatigue(-stat.GetFatigue)).
        StatEffect sleepEffect = new StatEffect(fatigueDelta: -state.Fatigue);

        candidates.Add(new Candidate
        {
            Kind = CandidateKind.Supply,
            Intent = NPCIntent.Sleep,
            Key = key,
            Position = pos,
            ActionType = ActionType.Sleep,
            OptionId = -1,
            RepeatCount = 1,
            TravelTime = travelTime,
            ActionTime = _tuning.EstimatedSleepSeconds,
            ActivityReward = 0f,
            After = ApplyEffect(state, sleepEffect),
        });
    }

    private void AddRoleCandidate(List<Candidate> candidates, NeedSnapshot state, Vector3 pos, float moveSpeed,
        NPCType npcType, StatEffect workCost)
    {
        if (workCost == null)
            return;

        switch (npcType)
        {
            case NPCType.Farmer:
                AddFarmerWorkCandidate(candidates, state, pos, moveSpeed, workCost);
                break;

            case NPCType.Guard:
                AddGuardDutyCandidate(candidates, state, pos, moveSpeed, workCost);
                break;

            default:
                // Cook and any future role without behavior fall back to the Idle floor.
                break;
        }
    }

    private void AddFarmerWorkCandidate(List<Candidate> candidates, NeedSnapshot state, Vector3 pos, float moveSpeed, StatEffect workCost)
    {
        int safeRepeats = EstimateWorkCount(state, workCost, out NeedSnapshot afterBatch);
        if (safeRepeats < _tuning.MinimumWorkBatch)
            return;

        if (!_destinationDB.TryGetDestinationPos(BuildingType.Farm, out Vector3 workPos))
            return;

        candidates.Add(new Candidate
        {
            Kind = CandidateKind.FarmerWork,
            Intent = NPCIntent.Work,
            Key = BuildingType.Farm,
            Position = workPos,
            ActionType = ActionType.Farming,
            OptionId = -1,
            RepeatCount = safeRepeats,
            TravelTime = GetTravelTime(pos, workPos, moveSpeed),
            ActionTime = _tuning.EstimatedFarmingSeconds * safeRepeats,
            ActivityReward = _tuning.WorkValue * safeRepeats,
            After = afterBatch,
        });
    }

    /// <summary>
    /// One imagined Guard duty slice. This is a planning approximation used to compare "keep
    /// standing guard" against supply options - the runtime GuardAction does not complete or
    /// replan on this interval.
    /// </summary>
    private void AddGuardDutyCandidate(List<Candidate> candidates, NeedSnapshot state, Vector3 pos, float moveSpeed, StatEffect dutyCost)
    {
        if (!_destinationDB.TryGetDestinationPos(BuildingType.GuardPost, out Vector3 postPos))
            return;

        float seconds = _tuning.GuardDutyEvaluationSeconds;

        candidates.Add(new Candidate
        {
            Kind = CandidateKind.GuardDuty,
            // Exposed as Idle: NPCIntent.Guard is deliberately not added in this pass, and
            // GuardActionSelector treats any non-supply result as "build the Guard queue".
            Intent = NPCIntent.Idle,
            Key = BuildingType.None,
            Position = postPos,
            ActionType = ActionType.Idle,
            OptionId = -1,
            RepeatCount = 1,
            TravelTime = GetTravelTime(pos, postPos, moveSpeed),
            ActionTime = seconds,
            ActivityReward = _tuning.GuardDutyValuePerSecond * seconds,
            After = ApplyEffect(state, dutyCost),
        });
    }

    // ---- Hard safety filtering ----

    private int BuildCriticalMask(NeedSnapshot state)
    {
        int mask = 0;

        if (Normalize(state.Fatigue, state.FatigueMax) > _tuning.CriticalNeedThreshold)
            mask |= CriticalBitFatigue;
        if (Normalize(state.Hunger, state.HungerMax) > _tuning.CriticalNeedThreshold)
            mask |= CriticalBitHunger;
        if (Normalize(state.Thirst, state.ThirstMax) > _tuning.CriticalNeedThreshold)
            mask |= CriticalBitThirst;

        return mask;
    }

    /// <summary>
    /// Narrows the candidate list to the best available safety tier while a need is critical.
    /// Tier 1: no critical need worsens and at least one improves.
    /// Tier 2: the worst critical need's peak strictly decreases.
    /// Tier 3: the peak does not worsen and total critical risk decreases.
    /// When no tier has a member, only the Idle floor survives so the NPC waits safely
    /// instead of being forced into work.
    /// </summary>
    private void ApplyCriticalFilter(List<Candidate> candidates, NeedSnapshot state, int criticalMask)
    {
        float beforeMax = MaxCriticalNeed(state, criticalMask);
        float beforeRiskSum = SumCriticalRisk(state, criticalMask);

        for (int tier = 1; tier <= 3; ++tier)
        {
            bool tierHasMember = false;
            for (int i = 0; i < candidates.Count; ++i)
            {
                if (candidates[i].Kind == CandidateKind.Supply &&
                    MatchesTier(state, candidates[i].After, criticalMask, tier, beforeMax, beforeRiskSum))
                {
                    tierHasMember = true;
                    break;
                }
            }

            if (!tierHasMember)
                continue;

            KeepOnly(candidates, state, criticalMask, tier, beforeMax, beforeRiskSum);
            return;
        }

        KeepOnlyIdle(candidates);
    }

    private void KeepOnly(List<Candidate> candidates, NeedSnapshot state, int criticalMask, int tier,
        float beforeMax, float beforeRiskSum)
    {
        int write = 0;
        for (int read = 0; read < candidates.Count; ++read)
        {
            Candidate c = candidates[read];
            if (c.Kind != CandidateKind.Supply)
                continue;
            if (!MatchesTier(state, c.After, criticalMask, tier, beforeMax, beforeRiskSum))
                continue;

            candidates[write++] = c;
        }

        candidates.RemoveRange(write, candidates.Count - write);
    }

    private static void KeepOnlyIdle(List<Candidate> candidates)
    {
        int write = 0;
        for (int read = 0; read < candidates.Count; ++read)
        {
            if (candidates[read].Kind != CandidateKind.Idle)
                continue;

            candidates[write++] = candidates[read];
        }

        candidates.RemoveRange(write, candidates.Count - write);
    }

    private bool MatchesTier(NeedSnapshot before, NeedSnapshot after, int criticalMask, int tier,
        float beforeMax, float beforeRiskSum)
    {
        switch (tier)
        {
            case 1:
                return IsStrictlySafeImprovement(before, after, criticalMask);

            case 2:
                return MaxCriticalNeed(after, criticalMask) < beforeMax;

            default:
                return MaxCriticalNeed(after, criticalMask) <= beforeMax
                    && SumCriticalRisk(after, criticalMask) < beforeRiskSum;
        }
    }

    // ---- Selection ----

    private static bool TryPickBest(List<Candidate> candidates, out int bestIndex)
    {
        bestIndex = -1;

        for (int i = 0; i < candidates.Count; ++i)
        {
            if (!IsFinite(candidates[i].Score))
                continue;

            if (bestIndex < 0 || CompareForSelection(candidates[i], candidates[bestIndex]) < 0)
                bestIndex = i;
        }

        return bestIndex >= 0;
    }

    // Ascending: lower is better. score desc -> travelTime asc -> OptionId asc -> ActionType asc
    // -> BuildingType asc -> Kind asc. Scores within ScoreEpsilon count as a tie so float noise
    // never changes the chosen action.
    private static int CompareForSelection(Candidate a, Candidate b)
    {
        float scoreDiff = a.Score - b.Score;
        if (scoreDiff > ScoreEpsilon)
            return -1;
        if (scoreDiff < -ScoreEpsilon)
            return 1;

        int byTravel = a.TravelTime.CompareTo(b.TravelTime);
        if (byTravel != 0)
            return byTravel;

        int byItem = a.OptionId.CompareTo(b.OptionId);
        if (byItem != 0)
            return byItem;

        int byAction = ((int)a.ActionType).CompareTo((int)b.ActionType);
        if (byAction != 0)
            return byAction;

        int byKey = ((int)a.Key).CompareTo((int)b.Key);
        if (byKey != 0)
            return byKey;

        return ((int)a.Kind).CompareTo((int)b.Kind);
    }

    private static NPCDecision ToDecision(Candidate c)
    {
        InteractionRequest? request = c.OptionId >= 0 ? new InteractionRequest(c.ActionType, c.OptionId) : (InteractionRequest?)null;
        return new NPCDecision(c.Intent, c.Key, c.Position, c.RepeatCount, request);
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
        return BuildCriticalMask(s) != 0;
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

    private static float MaxCriticalNeed(NeedSnapshot s, int criticalMask)
    {
        float max = 0f;
        for (int i = 0; i < CriticalNeedCount; ++i)
        {
            if ((criticalMask & (1 << i)) == 0)
                continue;

            float v = NormalizedNeed(s, i);
            if (v > max)
                max = v;
        }
        return max;
    }

    private float SumCriticalRisk(NeedSnapshot s, int criticalMask)
    {
        float sum = 0f;
        for (int i = 0; i < CriticalNeedCount; ++i)
        {
            if ((criticalMask & (1 << i)) == 0)
                continue;

            sum += NeedRisk(NormalizedNeed(s, i));
        }
        return sum;
    }

    private static bool IsStrictlySafeImprovement(NeedSnapshot before, NeedSnapshot after, int criticalMask)
    {
        bool improvedAny = false;
        for (int i = 0; i < CriticalNeedCount; ++i)
        {
            if ((criticalMask & (1 << i)) == 0)
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

    // ---- Risk ----

    /// <summary>
    /// Smooth nonlinear need risk. The exponent makes reducing an already-low need nearly
    /// worthless and reducing a near-maximum need very valuable, which is what removes the
    /// need for any fixed satisfaction threshold. The danger term adds a continuous extra
    /// penalty once the need passes DangerThreshold.
    /// </summary>
    private float NeedRisk(float n)
    {
        float clamped = Mathf.Clamp01(n);
        float baseRisk = Mathf.Pow(clamped, _tuning.NeedRiskExponent);

        float over = clamped - _tuning.DangerThreshold;
        if (over <= 0f)
            return baseRisk;

        return baseRisk + over * over * _tuning.DangerPenaltyMultiplier;
    }

    private float ComputeRisk(NeedSnapshot s)
    {
        float healthDeficit = Mathf.Clamp01(1f - Normalize(s.Health, s.HealthMax));

        return NeedRisk(Normalize(s.Fatigue, s.FatigueMax))
             + NeedRisk(Normalize(s.Hunger, s.HungerMax))
             + NeedRisk(Normalize(s.Thirst, s.ThirstMax))
             + Mathf.Pow(healthDeficit, _tuning.HealthRiskExponent) * _tuning.HealthWeight;
    }

    // ---- Misc ----

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float GetTravelTime(Vector3 from, Vector3 to, float moveSpeed)
    {
        float distance = Vector2.Distance(from, to);
        float speed = Mathf.Max(MinMoveSpeed, moveSpeed);
        return distance / speed;
    }

    // ---- Development trace ----

    private void LogDecisionTrace(NeedSnapshot state, Vector3 pos, NPCType npcType, List<Candidate> roots, int bestIndex)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!_tuning.LogDecisionTrace)
            return;

        float riskBefore = ComputeRisk(state);

        _traceBuilder.Clear();
        _traceBuilder.Append("[Decision] ").Append(npcType).Append(" at ").Append(pos.ToString("F2"))
            .Append(" needs(f/h/t)=")
            .Append(Normalize(state.Fatigue, state.FatigueMax).ToString("F2")).Append('/')
            .Append(Normalize(state.Hunger, state.HungerMax).ToString("F2")).Append('/')
            .Append(Normalize(state.Thirst, state.ThirstMax).ToString("F2"))
            .Append(" risk=").Append(riskBefore.ToString("F3"))
            .AppendLine();

        for (int i = 0; i < roots.Count; ++i)
        {
            Candidate c = roots[i];
            float immediate = ComputeImmediateScore(riskBefore, c);
            float elapsed = c.TravelTime + c.ActionTime;
            float riskExposure = (riskBefore + ComputeRisk(c.After)) * 0.5f * elapsed * _tuning.RiskExposureWeight;

            _traceBuilder.Append(i == bestIndex ? "  * " : "    ")
                .Append(c.Kind).Append('/').Append(c.Intent).Append(" key=").Append(c.Key)
                .Append(" item=").Append(c.OptionId).Append(" xN=").Append(c.RepeatCount)
                .Append(" reward=").Append(c.ActivityReward.ToString("F1"))
                .Append(" travel=-").Append((c.TravelTime * _tuning.TravelWeight).ToString("F1"))
                .Append(" actionTime=-").Append((c.ActionTime * _tuning.ActionTimeWeight).ToString("F1"))
                .Append(" exposure=-").Append(riskExposure.ToString("F1"))
                .Append(" future=").Append((c.Score - immediate).ToString("F1"))
                .Append(" total=").Append(c.Score.ToString("F1"))
                .AppendLine();
        }

        _traceBuilder.Append("  decided by: ").Append(DescribeTieBreak(roots, bestIndex));
        Debug.Log(_traceBuilder.ToString());
#endif
    }

    private static string DescribeTieBreak(List<Candidate> roots, int bestIndex)
    {
        int runnerUp = -1;
        for (int i = 0; i < roots.Count; ++i)
        {
            if (i == bestIndex || !IsFinite(roots[i].Score))
                continue;

            if (runnerUp < 0 || CompareForSelection(roots[i], roots[runnerUp]) < 0)
                runnerUp = i;
        }

        if (runnerUp < 0)
            return "only valid candidate";

        Candidate best = roots[bestIndex];
        Candidate second = roots[runnerUp];

        if (Mathf.Abs(best.Score - second.Score) > ScoreEpsilon)
            return "score";
        if (best.TravelTime != second.TravelTime)
            return "tie-break: travelTime";
        if (best.OptionId != second.OptionId)
            return "tie-break: itemId";
        if (best.ActionType != second.ActionType)
            return "tie-break: actionType";
        if (best.Key != second.Key)
            return "tie-break: buildingType";

        return "tie-break: candidateKind";
    }
}
