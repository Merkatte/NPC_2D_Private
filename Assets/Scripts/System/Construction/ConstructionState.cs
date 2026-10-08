using System.Collections.Generic;
using System.Collections.ObjectModel;

// Per-plot runtime, never stored in a shared definition or exposed for external mutation.
internal sealed class ConstructionState
{
    internal readonly BuildingDefinition Definition;
    internal readonly IReadOnlyDictionary<int, int> PaidCost;
    internal readonly bool IsUpgrade;
    internal readonly long Id;
    internal readonly long Order;
    internal readonly ConstructionReservation[] Reservations;
    internal float Work;
    internal bool HasWorkStarted;
    internal string CompletionFailure;

    internal ConstructionState(BuildingDefinition definition, long id, long order, bool isUpgrade = false)
    {
        Definition = definition;
        IsUpgrade = isUpgrade;
        Id = id;
        Order = order;
        var copy = new Dictionary<int, int>();
        foreach (var entry in definition.Cost)
            copy.Add(entry.Key, entry.Value);
        PaidCost = new ReadOnlyDictionary<int, int>(copy);
        Reservations = new ConstructionReservation[definition.MaxWorkers];
    }
}
