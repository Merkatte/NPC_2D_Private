using System.Collections.Generic;
using UnityEngine;

public class Pub : BaseInteractionProvider
{
    [SerializeField] private ItemDataContext _itemDataContext;

    private Dictionary<ItemCategory, List<ItemInfo>> _itemInfos;

    protected override bool TryInitializeCore(out string failureReason)
    {
        if (!_itemDataContext)
        {
            failureReason = "missing ItemDataContext";
            return false;
        }

        _itemInfos = _itemDataContext.ItemInfos();
        if (_itemInfos == null)
        {
            failureReason = "ItemDataContext produced no item info mapping";
            return false;
        }

        failureReason = null;
        return true;
    }

    protected override bool SupportsCore(ActionType type)
    {
        return type switch
        {
            ActionType.Drink => true,
            ActionType.Eat => true,
            _ => false
        };
    }

    protected override void AppendOptionsCore(ActionType type, List<InteractionOption> buffer)
    {
        ItemCategory? category = ToCategory(type);
        if (category == null)
            return;

        if (!_itemInfos.TryGetValue(category.Value, out var itemList))
            return;

        foreach (var item in itemList)
        {
            buffer.Add(new InteractionOption(type, item.ID, item.Effect));
        }
    }

    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;

        ItemCategory? category = ToCategory(request.Type);
        if (category == null)
            return false;

        if (!_itemInfos.TryGetValue(category.Value, out var itemList))
            return false;

        foreach (var item in itemList)
        {
            if (item.ID != request.OptionId)
                continue;

            result = new InteractionResult(item.Effect);
            return true;
        }

        return false;
    }

    private static ItemCategory? ToCategory(ActionType type)
    {
        return type switch
        {
            ActionType.Drink => ItemCategory.Drink,
            ActionType.Eat => ItemCategory.Food,
            _ => null
        };
    }
}
