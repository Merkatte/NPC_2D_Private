using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DataManager : MonoBehaviour, IDataManager
{
    [SerializeField] private CostInfo[] _costInfos;
    [FormerlySerializedAs("_statInfo")] [SerializeField] private DefaultStatContext _defaultStatContext;
    [SerializeField] private ItemDataContext _itemDataContext;

    Dictionary<ActionType, CostInfo> _costInfoDict = new Dictionary<ActionType, CostInfo>();
    
    public static IDataManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        foreach (var item in _costInfos)
        {
            _costInfoDict.Add(item.actionCost.MyType, item);
        }
    }

    public NPCStat GetStat()
    {
        return _defaultStatContext.CreateStat();
    }

    public Dictionary<ItemCategory, List<ItemInfo>> GetItemInfos()
    {
        return _itemDataContext.ItemInfos();
    }

    public bool TryGetActionCostInfo<T>(ActionType actionType, out T costInfo) where T : DefaultActionCost
    {
        costInfo = null;
        if (!_costInfoDict.TryGetValue(actionType, out var info))
            return false;

        costInfo = info.actionCost as T;
        if (costInfo == null)
        {
            Debug.LogError($"CostInfo for {actionType} is not of type {typeof(T).Name}.");
            return false;
        }

        return true;
    }
}

[Serializable]
public class CostInfo
{
    [SerializeField] public DefaultActionCost actionCost;
}
