using UnityEngine;

// Invariant: this struct exposes exactly one interaction path (InteractionProvider +
// Request) for every facility domain. Do not add Cook/Shop/Clinic-specific provider fields;
// route new domains through IInteractionProvider instead.
public readonly struct ActionContext
{
    public NPCComponent Component { get; }
    public NPCStat Stat { get; }
    public Vector3? Destination { get; }
    public DefaultActionCost CostInfo { get; }
    public IInteractionProvider InteractionProvider { get; }
    public InteractionRequest? Request { get; }
    public MoveRequest? MoveRequest { get; }

    public bool HasComponent => Component != null;
    public bool HasDestination => Destination.HasValue;

    public ActionContext(NPCComponent component, NPCStat stat, Vector3? destination = null, DefaultActionCost cost = null, IInteractionProvider provider = null, InteractionRequest? request = null, MoveRequest? moveRequest = null)
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
