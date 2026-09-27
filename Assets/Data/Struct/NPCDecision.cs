using UnityEngine;

/// <summary>
/// Result of <see cref="DestinationDecider.Decide"/>. One decision is one semantic step:
/// go to one destination and do one thing there (or repeat Work up to RepeatCount times).
/// A selector converts this directly into a queue without re-deriving any decision logic.
/// </summary>
public readonly struct NPCDecision
{
    public NPCIntent Intent { get; }
    public BuildingType DestinationKey { get; }
    public Vector3 DestinationPos { get; }
    public int RepeatCount { get; }
    public InteractionRequest? Request { get; }
    public DestinationInfo Destination { get; }
    public IInteractionProvider Provider { get; }
    public bool HasLiveDestination => Destination != null && Destination.DestinationObject
        && Destination.DestinationObject.activeInHierarchy && Destination.DestinationLoc;

    public NPCDecision(NPCIntent intent, BuildingType destinationKey, Vector3 destinationPos, int repeatCount, InteractionRequest? request = null, DestinationInfo destination = null, IInteractionProvider provider = null)
    {
        Intent = intent;
        DestinationKey = destinationKey;
        DestinationPos = destinationPos;
        RepeatCount = repeatCount;
        Request = request;
        Destination = destination;
        Provider = provider;
    }

    public static NPCDecision Idle(Vector3 pos) => new NPCDecision(NPCIntent.Idle, BuildingType.None, pos, 1);
}
