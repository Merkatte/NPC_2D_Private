public interface IDataManager
{
    bool TryGetActionCostInfo<T>(ActionType actionType, out T costInfo) where T : DefaultActionCost;

    bool TryGetItemInfo(int itemId, out ItemInfo info);
    bool TryGetBuildingDefinition(int buildingId, out BuildingDefinition definition);
    System.Collections.Generic.IReadOnlyList<BuildingDefinition> BuildingDefinitions { get; }
}
