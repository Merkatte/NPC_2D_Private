using System.Collections.Generic;

public interface IDataManager
{
    bool TryGetActionCostInfo<T>(ActionType actionType, out T costInfo) where T : DefaultActionCost;
    Dictionary<ItemCategory, List<ItemInfo>> GetItemInfos();
}
