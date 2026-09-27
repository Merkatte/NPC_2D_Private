using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class BuildingDefinition
{
    public int Id { get; }
    public BuildingType BuildingType { get; }
    public string DisplayName { get; }
    public float RequiredWork { get; }
    public int MaxWorkers { get; }
    public int ProvidedCapacity { get; }
    public IReadOnlyDictionary<int, int> Cost { get; }

    public BuildingDefinition(int id, BuildingType buildingType, string displayName, float requiredWork,
        int maxWorkers, int providedCapacity, IReadOnlyDictionary<int, int> cost)
    {
        Id = id;
        BuildingType = buildingType;
        DisplayName = displayName;
        RequiredWork = requiredWork;
        MaxWorkers = maxWorkers;
        ProvidedCapacity = providedCapacity;
        var copy = new Dictionary<int, int>();
        foreach (var entry in cost)
            copy.Add(entry.Key, entry.Value);
        Cost = new ReadOnlyDictionary<int, int>(copy);
    }
}
