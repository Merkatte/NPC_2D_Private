using System.Collections.Generic;

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

    public override void AppendOptions(ActionType type, List<InteractionOption> buffer)
    {
        if (_itemInfos == null)
            return;

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

    public override bool TryInteraction(InteractRequest request, out InteractResult interactResult)
    {
        interactResult = default;

        if (_itemInfos == null)
            return false;

        ItemCategory? category = ToCategory(request.Type);
        if (category == null)
            return false;

        if (!_itemInfos.TryGetValue(category.Value, out var itemList))
            return false;

        foreach (var item in itemList)
        {
            if (item.ID != request.ItemId)
                continue;

            interactResult = new InteractResult(true, item.Effect);
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
