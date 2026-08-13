using System.Collections.Generic;
using UnityEngine;

public class Pub : BaseInteractable
{
    public override bool CanInteract(ActionType type)
    {
        return type switch
        {
            ActionType.Drink => true,
            ActionType.Eat => true,
            _ => false
        };
    }

    public override bool TryInteraction(ActionType type, out InteractResult interactResult)
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
}
