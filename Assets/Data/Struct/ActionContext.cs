using UnityEngine;

public readonly struct ActionContext
{
    public NPCComponent Component { get; }
    public NPCStat Stat { get; }
    public Vector3? Destination { get; }
    public DefaultActionCost CostInfo { get; }
    public IInteractionProvider InteractionProvider { get; }
    public InteractRequest? Request { get; }
    public MoveRequest? MoveRequest { get; }

    public bool HasComponent => Component != null;
    public bool HasStat => Stat != null;
    public bool HasDestination => Destination.HasValue;

    public ActionContext(NPCComponent component, NPCStat stat, Vector3? destination = null, DefaultActionCost cost = null, IInteractionProvider provider = null, InteractRequest? request = null, MoveRequest? moveRequest = null)
    {
        Component = component;
        Stat = stat;
        Destination = destination;
        CostInfo = cost;
        InteractionProvider = provider;
        Request = request;
        MoveRequest = moveRequest;
    }
}
