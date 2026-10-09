using UnityEngine;

public readonly struct GuardDutyDecisionContext
{
    public Vector3 Position { get; }
    public bool IsAvailable { get; }

    public GuardDutyDecisionContext(Vector3 position, bool isAvailable)
    {
        Position = position;
        IsAvailable = isAvailable;
    }
}
