using System;

// Reservation exists before recruitment spends resources; a downed occupant retains it.
public sealed class DefenseArcherSlotLease : IDisposable
{
    public DefenseBattlefield Battlefield { get; private set; }
    public DefenseWallSegment Wall { get; }
    public DefenseActor Occupant { get; private set; }
    public bool IsValid => Battlefield && Wall;
    internal DefenseArcherSlotLease(DefenseBattlefield battlefield, DefenseWallSegment wall)
    { Battlefield = battlefield; Wall = wall; }
    public bool TryOccupy(DefenseActor actor)
    {
        if (!IsValid || !actor || Occupant) return false;
        Occupant = actor;
        actor.AssignArcherSlot(this);
        return true;
    }
    public void Dispose()
    {
        DefenseBattlefield battlefield = Battlefield;
        Battlefield = null;
        Occupant = null;
        if (battlefield) battlefield.ReleaseArcherSlot(this);
    }
}
