
public interface IDataManager
{
    NPCStat GetStat();
    bool TryGetActionCostInfo<T>(ActionType actionType, out T costInfo) where T : DefaultActionCost;
}
