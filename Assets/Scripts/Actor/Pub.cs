using System;
using System.Collections.Generic;
using UnityEngine;

public class Pub : MonoBehaviour, IInteractionProvider
{
    private Dictionary<ItemCategory, List<ItemInfo>> _itemInfos;

    public void Init(Dictionary<ItemCategory, List<ItemInfo>> itemInfos)
    {
        _itemInfos = itemInfos;
    }

    public bool CanInteract(ActionType type)
    {
        return type switch
        {
            ActionType.Drink => true,
            ActionType.Eat => true,
            _ => false
        };
    }

    public bool TryInteraction(ActionType type, out InteractResult interactResult)
    {
        List<ItemInfo> itemList;
        switch (type)
        {
            case ActionType.Drink:
                itemList = _itemInfos[ItemCategory.Drink];
                break;
            case ActionType.Eat:
                itemList = _itemInfos[ItemCategory.Food];
                break;
            default:
                interactResult = default;
                return false;
        }
        
        int randomIndex = UnityEngine.Random.Range(0, itemList.Count);
        StatEffect statEffect = itemList[randomIndex].Effect;
        interactResult = new InteractResult(true, statEffect);
        return true;
    }

    /// <summary>
    /// Current Method is for temp only!
    /// </summary>
    /// <returns></returns>
    private InteractResult ProvideBeer()
    {
        var effect = new StatEffect(
            thirstDelta: -20f,
            moodDelta: 10f,
            healthDelta: -2f
        );

        return new InteractResult(true, effect);
    }
}
