using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour, IDataManager
{
    [SerializeField] private CostInfo[] _costInfos;
    [SerializeField] private ItemDataContext _itemDataContext;

    Dictionary<ActionType, CostInfo> _costInfoDict = new Dictionary<ActionType, CostInfo>();

    public static IDataManager instance;

    void Awake()
    {
        instance = this;

        foreach (var item in _costInfos)
        {
            _costInfoDict.Add(item.actionCost.MyType, item);
        }
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

    // Delegates to ItemDataContext rather than duplicating item storage: DataManager is the
    // query facade for immutable data, ItemDataContext stays the owner of the CSV-backed table.
    public bool TryGetItemInfo(int itemId, out ItemInfo info)
    {
        if (!_itemDataContext)
        {
            info = default;
            Debug.LogError("DataManager: _itemDataContext is not assigned.");
            return false;
        }

        return _itemDataContext.TryGetItemInfo(itemId, out info);
    }
}

[Serializable]
public class CostInfo
{
    [SerializeField] public DefaultActionCost actionCost;
}
