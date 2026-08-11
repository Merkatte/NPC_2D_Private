
public interface IDataManager
{
    NPCStat GetStat();
    bool TryGetWorkCostInfo(NPCType npcType, out CostInfo costInfo);
}
