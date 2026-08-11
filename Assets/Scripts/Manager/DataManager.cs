using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour, IDataManager
{
    [SerializeField] private CostInfo[] _costInfos;
    [SerializeField] private DefaultStatContext _statInfo;
    
    Dictionary<ItemCategory, List<ItemInfo>> _itemInfos = new Dictionary<ItemCategory, List<ItemInfo>>();
    Dictionary<NPCType, CostInfo> _costInfoDict = new Dictionary<NPCType, CostInfo>();
    
    public static IDataManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        foreach (var item in _costInfos)
        {
            _costInfoDict.Add(item.npcType, item);
        }
    }
    
    public NPCStat GetStat()
    {
        return _statInfo.CreateStat();
    }

    public bool TryGetWorkCostInfo(NPCType npcType, out CostInfo costInfo)
    {
        costInfo = null;
        if (!_costInfoDict.TryGetValue(npcType, out var info))
        {
            return false;
        }
        
        costInfo = info;
        return true;
    }
}

[Serializable]
public class CostInfo
{
    [SerializeField] public NPCType npcType;
    [SerializeField] public DefaultActionCost actionCost;
}
