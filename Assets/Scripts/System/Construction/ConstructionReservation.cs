using UnityEngine;

public sealed class ConstructionReservation : IInteractionReservation
{
    internal BuildingPlot Issuer { get; }
    internal long ConstructionId { get; }
    internal int Slot { get; }
    public Vector3 WorkPosition { get; }
    public ConstructionReservationStatus Status { get; private set; }
    public bool IsValid => Status == ConstructionReservationStatus.Active && Issuer && Issuer.IsReservationValid(this);

    internal ConstructionReservation(BuildingPlot issuer, long constructionId, int slot, Vector3 position)
    {
        Issuer = issuer;
        ConstructionId = constructionId;
        Slot = slot;
        WorkPosition = position;
    }
    internal void End(ConstructionReservationStatus status) { Status = status; }
    public void Dispose()
    {
        if (Status != ConstructionReservationStatus.Active)
            return;
        if (Issuer)
            Issuer.Release(this);
        else
            End(ConstructionReservationStatus.Released);
    }
}
