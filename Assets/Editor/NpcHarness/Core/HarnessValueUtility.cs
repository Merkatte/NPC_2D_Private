using UnityEngine;

internal static class HarnessValueUtility
{
    private const float Tolerance = 0.0001f;

    public static Vector3 ToVector3(HarnessVector3 value)
    {
        return new Vector3(value.x, value.y, value.z);
    }

    public static Color ToColor(HarnessColor value)
    {
        return new Color(value.r, value.g, value.b, value.a);
    }

    public static Color ToGradientStorageColor(Color value)
    {
        return (Color)(Color32)value;
    }

    public static bool Approximately(Vector3 left, Vector3 right)
    {
        return (left - right).sqrMagnitude <= Tolerance * Tolerance;
    }

    public static bool Approximately(Color left, Color right)
    {
        return Mathf.Abs(left.r - right.r) <= Tolerance &&
               Mathf.Abs(left.g - right.g) <= Tolerance &&
               Mathf.Abs(left.b - right.b) <= Tolerance &&
               Mathf.Abs(left.a - right.a) <= Tolerance;
    }

    public static bool Approximately(float left, float right)
    {
        return Mathf.Abs(left - right) <= Tolerance;
    }

    public static HarnessToolResult RequireOverwrite(
        HarnessToolContext context,
        Object target,
        bool isDifferent,
        string description)
    {
        if (!isDifferent)
        {
            return HarnessToolResult.NoChange($"{description} already matches.");
        }

        if (context.WasCreated(target) || context.Options.AllowOverwrite)
        {
            return HarnessToolResult.Success($"{description} may be changed.");
        }

        return HarnessToolResult.Failure(
            $"{description} differs from the requested value. Explicit overwrite approval is required.");
    }
}
