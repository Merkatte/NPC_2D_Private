using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared stateless combat queries. Distance calculations use 2D positions with Z excluded.
/// Target search does not mutate runtime state; the caller owns target selection and clearing.
/// </summary>
public static class CombatLib
{
    public static bool IsInRange(Vector3 from, Vector3 to, float range)
    {
        Vector3 offset = to - from;
        offset.z = 0f;
        return offset.sqrMagnitude <= range * range;
    }

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
