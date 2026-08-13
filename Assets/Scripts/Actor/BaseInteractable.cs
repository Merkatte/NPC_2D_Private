using System.Collections.Generic;
using UnityEngine;

public abstract class BaseInteractable : MonoBehaviour, IInteractionProvider
{
    protected Dictionary<ItemCategory, List<ItemInfo>> _itemInfos;

    public virtual void Init(Dictionary<ItemCategory, List<ItemInfo>> itemInfos)
    {
        _itemInfos = itemInfos;
    }

    public abstract bool CanInteract(ActionType type);

    public abstract bool TryInteraction(ActionType type, out InteractResult interactResult);
}
