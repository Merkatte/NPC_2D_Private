using UnityEngine;

/// <summary>
/// One step in an NPC's decided action chain: where to go, what to do there,
/// and how many times to repeat it (used for repeated Work steps).
/// </summary>
public readonly struct NPCDecisionStep
{
    public NPCDecisionStep(NPCIntent intent, string destinationKey, Vector3 destinationPos, int repeatCount)
    {
        Intent = intent;
        DestinationKey = destinationKey;
        DestinationPos = destinationPos;
        RepeatCount = repeatCount;
    }

    public NPCIntent Intent { get; }
    public string DestinationKey { get; }
    public Vector3 DestinationPos { get; }
    public int RepeatCount { get; }
}

/// <summary>
/// Result of <see cref="DestinationDecider.Decide"/>. Carries an ordered chain of steps
/// so a selector can build a queue without re-deriving any decision logic.
/// </summary>
public readonly struct NPCDecision
{
    public NPCDecision(NPCDecisionStep[] steps, int estimatedWorkCount, NPCIntent nextRequiredIntent)
    {
        Steps = steps ?? System.Array.Empty<NPCDecisionStep>();
        EstimatedWorkCount = estimatedWorkCount;
        NextRequiredIntent = nextRequiredIntent;
    }

    public NPCDecisionStep[] Steps { get; }
    public int EstimatedWorkCount { get; }
    public NPCIntent NextRequiredIntent { get; }

    public NPCIntent PrimaryIntent => Steps.Length > 0 ? Steps[0].Intent : NPCIntent.None;

    public static NPCDecision None => new NPCDecision(System.Array.Empty<NPCDecisionStep>(), 0, NPCIntent.None);
}
