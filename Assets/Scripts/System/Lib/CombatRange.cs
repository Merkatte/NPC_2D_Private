using UnityEngine;

public static class CombatRange
{
    public static bool IsInRange(Vector3 from, Vector3 to, float range)
    {
        Vector3 offset = to - from;
        offset.z = 0f;
        return offset.sqrMagnitude <= range * range;
    }
}
