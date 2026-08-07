using UnityEngine;

/// <summary>
/// Job-neutral NPC decision layer. Decides what an NPC should do next (work, or supply a
/// need first and then work) without knowing about IAction, ActionPool, or action queues.
/// See PublicMD/NPC_Decision_System_Plan.md for the reasoning behind this design.
/// </summary>
public class DestinationDecider
{
    // Destination keys.
    private const string FarmerWorkPlaceKey = "farmerWorkingPlace";
    private const string SleepPlaceKey = "sleepPlace";
    private const string DrinkPlaceKey = "drinkPlace";
    private const string EatPlaceKey = "eatPlace";

    // TODO: need 증가 시스템이 추가되면 이 튜닝 값들을 데이터 에셋으로 옮긴다. (PublicMD/NPC_Decision_System_Plan.md 참고)
    private const float FatiguePerTravelSecond = 0.1f;
    private const float HungerPerTravelSecond = 0.15f;
    private const float ThirstPerTravelSecond = 0.25f;
    private const float FatiguePerWork = 8f;
    private const float HungerPerWork = 5f;
    private const float ThirstPerWork = 7f;
    private const float DangerThreshold = 85f;
    private const float CriticalThreshold = 95f;
    private const float WorkValue = 100f;
    private const float TravelCost = 3f;
    private const float SupplyActionCost = 10f;
    private const float DangerPenaltyMultiplier = 5f;
    private const float MinMoveSpeed = 0.01f;
    private const float MaxNeed = 100f;

    // 이 문턱을 넘는 한계이득이 없으면 보급을 포기하고 바로 일하러 간다.
    private const float MinChainGain = 30f;

    private DestinationDB _destinationDB;

    private struct NeedSnapshot
    {
        public float Fatigue;
        public float Hunger;
        public float Thirst;
    }

    private struct SupplyCandidate
    {
        public NPCIntent Intent;
        public string Key;
        public Vector3 Pos;
    }

    public void Init(DestinationDB destinationDB)
    {
        _destinationDB = destinationDB;
    }

    public NPCDecision Decide(IStatView stat, NPCType npcType, Vector3 npcLoc)
    {
        if (!_destinationDB || stat == null)
            return NPCDecision.None;

        bool hasDrink = _destinationDB.TryGetDestinationPos(DrinkPlaceKey, out Vector3 drinkPos);
        bool hasEat = _destinationDB.TryGetDestinationPos(EatPlaceKey, out Vector3 eatPos);
        bool hasSleep = _destinationDB.TryGetDestinationPos(SleepPlaceKey, out Vector3 sleepPos);

        string workKey = GetWorkPlaceKey(npcType);
        Vector3 workPos = Vector3.zero;
        bool hasWork = !string.IsNullOrEmpty(workKey) && _destinationDB.TryGetDestinationPos(workKey, out workPos);

        SupplyCandidate[] supplies = new SupplyCandidate[3];
        int supplyCount = 0;
        if (hasDrink)
            supplies[supplyCount++] = new SupplyCandidate { Intent = NPCIntent.Drink, Key = DrinkPlaceKey, Pos = drinkPos };
        if (hasEat)
            supplies[supplyCount++] = new SupplyCandidate { Intent = NPCIntent.Eat, Key = EatPlaceKey, Pos = eatPos };
        if (hasSleep)
            supplies[supplyCount++] = new SupplyCandidate { Intent = NPCIntent.Sleep, Key = SleepPlaceKey, Pos = sleepPos };

        NeedSnapshot state = new NeedSnapshot
        {
            Fatigue = stat.CurrentFatiguePercentage,
            Hunger = stat.CurrentHungerPercentage,
            Thirst = stat.CurrentThirstPercentage,
        };
        float moveSpeed = stat.GetMoveSpeed;

        // Critical priority: if the most limiting need would blow past CriticalThreshold,
        // resolve it immediately instead of running the marginal-gain chain.
        NeedSnapshot criticalCheckState = state;
        if (hasWork)
        {
            float travelToWork = GetTravelTime(npcLoc, workPos, moveSpeed);
            criticalCheckState = ApplyWork(ApplyTravel(state, travelToWork));
        }

        NPCIntent criticalIntent = GetLimitingNeedAboveThreshold(criticalCheckState, CriticalThreshold);
        if (criticalIntent != NPCIntent.None && TryFindSupply(supplies, supplyCount, criticalIntent, out SupplyCandidate criticalSupply))
        {
            NPCDecisionStep[] criticalSteps = { new NPCDecisionStep(criticalIntent, criticalSupply.Key, criticalSupply.Pos, 1) };
            return new NPCDecision(criticalSteps, 0, hasWork ? NPCIntent.Work : NPCIntent.None);
        }

        if (!hasWork)
        {
            // 작업장 키를 얻을 수 없는 직업(Farmer 외)은 아직 Work 판단을 지원하지 않는다.
            // TODO: Guard/Cook 등 다른 직업의 작업장 키가 정해지면 위 hasWork 분기에 합류시킨다.
            NPCIntent mostLimiting = GetMostLimitingNeed(state);
            if (TryFindSupply(supplies, supplyCount, mostLimiting, out SupplyCandidate fallbackSupply))
            {
                NPCDecisionStep[] fallbackSteps = { new NPCDecisionStep(mostLimiting, fallbackSupply.Key, fallbackSupply.Pos, 1) };
                return new NPCDecision(fallbackSteps, 0, NPCIntent.None);
            }

            return NPCDecision.None;
        }

        return BuildWorkPlan(state, npcLoc, workKey, workPos, moveSpeed, supplies, supplyCount);
    }

    /// <summary>
    /// Greedy marginal-gain chain ("나온 김에" 판단): pick supply stops one at a time,
    /// re-evaluating from the new position each time, until no remaining stop clears
    /// MinChainGain. Then append the Work step. See PublicMD/NPC_Decision_System_Plan.md.
    /// </summary>
    private NPCDecision BuildWorkPlan(NeedSnapshot state, Vector3 npcLoc, string workKey, Vector3 workPos,
        float moveSpeed, SupplyCandidate[] supplies, int supplyCount)
    {
        bool[] used = new bool[supplyCount];
        NeedSnapshot chainState = state;
        Vector3 chainPos = npcLoc;

        NPCDecisionStep[] steps = new NPCDecisionStep[supplyCount + 1];
        int stepCount = 0;

        for (int iteration = 0; iteration < supplyCount; ++iteration)
        {
            float baseScore = ScoreGoWorkNow(chainState, chainPos, workPos, moveSpeed);

            float bestGain = MinChainGain;
            int bestIndex = -1;
            NeedSnapshot bestState = default;

            for (int i = 0; i < supplyCount; ++i)
            {
                if (used[i])
                    continue;

                SupplyCandidate candidate = supplies[i];
                float travel = GetTravelTime(chainPos, candidate.Pos, moveSpeed);
                NeedSnapshot afterMove = ApplyTravel(chainState, travel);
                NeedSnapshot afterSupply = ApplySupply(afterMove, candidate.Intent);
                float score = ScoreGoWorkNow(afterSupply, candidate.Pos, workPos, moveSpeed) - travel * TravelCost - SupplyActionCost;
                float gain = score - baseScore;

                if (gain > bestGain)
                {
                    bestGain = gain;
                    bestIndex = i;
                    bestState = afterSupply;
                }
            }

            if (bestIndex < 0)
                break;

            SupplyCandidate chosen = supplies[bestIndex];
            steps[stepCount++] = new NPCDecisionStep(chosen.Intent, chosen.Key, chosen.Pos, 1);
            used[bestIndex] = true;
            chainState = bestState;
            chainPos = chosen.Pos;
        }

        float finalTravel = GetTravelTime(chainPos, workPos, moveSpeed);
        NeedSnapshot beforeWork = ApplyTravel(chainState, finalTravel);
        int workCount = EstimateWorkCount(beforeWork);
        steps[stepCount++] = new NPCDecisionStep(NPCIntent.Work, workKey, workPos, Mathf.Max(1, workCount));

        if (stepCount < steps.Length)
            System.Array.Resize(ref steps, stepCount);

        NeedSnapshot afterWork = workCount > 0 ? ApplyWork(beforeWork) : beforeWork;
        NPCIntent nextRequired = GetMostLimitingNeed(afterWork);

        return new NPCDecision(steps, workCount, nextRequired);
    }

    /// <summary>
    /// Score of "go to the work place from here and work as many times as possible",
    /// including the travel cost of that leg. Used as the common yardstick every
    /// marginal-gain comparison is measured against.
    /// </summary>
    private static float ScoreGoWorkNow(NeedSnapshot state, Vector3 from, Vector3 workPos, float moveSpeed)
    {
        float travelToWork = GetTravelTime(from, workPos, moveSpeed);
        NeedSnapshot afterTravel = ApplyTravel(state, travelToWork);
        int workCount = EstimateWorkCount(afterTravel);
        NeedSnapshot workState = workCount > 0 ? ApplyWork(afterTravel) : afterTravel;
        return ScorePlan(workState, workCount, travelToWork, 0);
    }

    private static float ScorePlan(NeedSnapshot finalState, int workCount, float travelTime, int supplyActionCount)
    {
        float score = workCount * WorkValue;
        score -= travelTime * TravelCost;
        score -= supplyActionCount * SupplyActionCost;
        score -= GetDangerPenalty(finalState);
        return score;
    }

    private static bool TryFindSupply(SupplyCandidate[] supplies, int count, NPCIntent intent, out SupplyCandidate found)
    {
        for (int i = 0; i < count; ++i)
        {
            if (supplies[i].Intent == intent)
            {
                found = supplies[i];
                return true;
            }
        }

        found = default;
        return false;
    }

    private static string GetWorkPlaceKey(NPCType npcType)
    {
        switch (npcType)
        {
            case NPCType.Farmer:
                return FarmerWorkPlaceKey;
            default:
                // TODO: Guard/Cook 등 다른 직업의 작업장 키가 정해지면 매핑을 추가한다.
                return string.Empty;
        }
    }

    private static float GetTravelTime(Vector3 from, Vector3 to, float moveSpeed)
    {
        float distance = Vector2.Distance(from, to);
        float speed = Mathf.Max(MinMoveSpeed, moveSpeed);
        return distance / speed;
    }

    private static NeedSnapshot ApplyTravel(NeedSnapshot state, float travelTime)
    {
        return new NeedSnapshot
        {
            Fatigue = Mathf.Min(MaxNeed, state.Fatigue + FatiguePerTravelSecond * travelTime),
            Hunger = Mathf.Min(MaxNeed, state.Hunger + HungerPerTravelSecond * travelTime),
            Thirst = Mathf.Min(MaxNeed, state.Thirst + ThirstPerTravelSecond * travelTime),
        };
    }

    private static NeedSnapshot ApplyWork(NeedSnapshot state)
    {
        return new NeedSnapshot
        {
            Fatigue = Mathf.Min(MaxNeed, state.Fatigue + FatiguePerWork),
            Hunger = Mathf.Min(MaxNeed, state.Hunger + HungerPerWork),
            Thirst = Mathf.Min(MaxNeed, state.Thirst + ThirstPerWork),
        };
    }

    private static NeedSnapshot ApplySupply(NeedSnapshot state, NPCIntent supply)
    {
        switch (supply)
        {
            case NPCIntent.Drink:
                return ApplyDrink(state);
            case NPCIntent.Eat:
                return ApplyEat(state);
            case NPCIntent.Sleep:
                return ApplySleep(state);
            default:
                return state;
        }
    }

    private static NeedSnapshot ApplyDrink(NeedSnapshot state)
    {
        state.Thirst = 0f;
        return state;
    }

    private static NeedSnapshot ApplyEat(NeedSnapshot state)
    {
        state.Hunger = 0f;
        return state;
    }

    private static NeedSnapshot ApplySleep(NeedSnapshot state)
    {
        state.Fatigue = 0f;
        return state;
    }

    private static int EstimateWorkCount(NeedSnapshot state)
    {
        int byFatigue = Mathf.FloorToInt((MaxNeed - state.Fatigue) / FatiguePerWork);
        int byHunger = Mathf.FloorToInt((MaxNeed - state.Hunger) / HungerPerWork);
        int byThirst = Mathf.FloorToInt((MaxNeed - state.Thirst) / ThirstPerWork);

        return Mathf.Max(0, Mathf.Min(byFatigue, Mathf.Min(byHunger, byThirst)));
    }

    private static NPCIntent GetMostLimitingNeed(NeedSnapshot state)
    {
        NPCIntent result = NPCIntent.Sleep;
        float maxValue = state.Fatigue;

        if (state.Hunger > maxValue)
        {
            maxValue = state.Hunger;
            result = NPCIntent.Eat;
        }

        if (state.Thirst > maxValue)
        {
            result = NPCIntent.Drink;
        }

        return result;
    }

    private static NPCIntent GetLimitingNeedAboveThreshold(NeedSnapshot state, float threshold)
    {
        NPCIntent limiting = GetMostLimitingNeed(state);
        float value = GetNeedValue(state, limiting);
        return value > threshold ? limiting : NPCIntent.None;
    }

    private static float GetNeedValue(NeedSnapshot state, NPCIntent intent)
    {
        switch (intent)
        {
            case NPCIntent.Sleep:
                return state.Fatigue;
            case NPCIntent.Eat:
                return state.Hunger;
            case NPCIntent.Drink:
                return state.Thirst;
            default:
                return 0f;
        }
    }

    private static float GetDangerPenalty(NeedSnapshot state)
    {
        return GetSingleDangerPenalty(state.Fatigue)
            + GetSingleDangerPenalty(state.Hunger)
            + GetSingleDangerPenalty(state.Thirst);
    }

    private static float GetSingleDangerPenalty(float value)
    {
        if (value <= DangerThreshold)
            return 0f;

        float over = value - DangerThreshold;
        return over * over * DangerPenaltyMultiplier;
    }
}
