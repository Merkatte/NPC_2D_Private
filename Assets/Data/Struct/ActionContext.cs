using UnityEngine;

public readonly struct ActionContext
{
    public NPCComponent Component { get; }
    public NPCStat Stat { get; }
    public Vector3? Destination { get; }

    public bool HasComponent => Component != null;
    public bool HasStat => Stat != null;
    public bool HasDestination => Destination.HasValue;

    public ActionContext(NPCComponent component, NPCStat stat, Vector3? destination = null)
    {
        Component = component;
        Stat = stat;
        Destination = destination;
    }
}
