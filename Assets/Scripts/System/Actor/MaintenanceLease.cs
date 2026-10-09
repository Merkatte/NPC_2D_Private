using UnityEngine;

public sealed class MaintenanceLease : IInteractionReservation
{
    public DefenseMaintenanceSite Site { get; private set; }
    public MaintenanceKind Kind { get; }
    public Vector3 WorkPosition { get; }
    public bool IsValid => Site && Site.IsLeaseValid(this);
    public bool IsCompleted { get; private set; }
    internal MaintenanceLease(DefenseMaintenanceSite site, MaintenanceKind kind, Vector3 position)
    { Site = site; Kind = kind; WorkPosition = position; }
    internal void Complete() { IsCompleted = true; Site = null; }
    public void Dispose()
    {
        DefenseMaintenanceSite site = Site; Site = null;
        if (site) site.Release(this);
    }
}
