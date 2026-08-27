using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure nearest-target search shared by any selector that owns a CombatPerception/CombatRuntimeState
/// pair. This does not mutate runtime state - the caller decides whether to call
/// CombatRuntimeState.SetTarget(...) (and whether to gate the call behind an existing sticky
/// target, as GuardActionSelector does) and whether to clear it when no candidate is found.
/// Distance is 2D (Z excluded), matching CombatRange.IsInRange.
/// </summary>
public static class CombatTargeting
{
    public static bool TryFindNearestTarget(
        CombatPerception perception, Vector3 fromPosition,
        List<(ICombatTarget Target, Component Owner)> scratchBuffer,
        float? maxRange, out ICombatTarget target, out Component owner)
    {
        target = null;
        owner = null;

        if (!perception || !perception.HasCandidate)
        {
            return false;
        }

        perception.CopyCandidatesTo(scratchBuffer);

        float bestSqrDistance = float.PositiveInfinity;
        float maxSqrRange = maxRange.HasValue ? maxRange.Value * maxRange.Value : float.PositiveInfinity;

        for (int i = 0; i < scratchBuffer.Count; ++i)
        {
            (ICombatTarget candidateTarget, Component candidateOwner) = scratchBuffer[i];
            if (!CombatTargetHandle.IsValidPair(candidateTarget, candidateOwner))
            {
                continue;
            }

            Vector3 offset = candidateTarget.Position - fromPosition;
            offset.z = 0f;
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance > maxSqrRange)
            {
                continue;
            }

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                target = candidateTarget;
                owner = candidateOwner;
            }
        }

        return target != null;
    }
}
