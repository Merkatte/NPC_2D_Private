using UnityEngine;

public interface IInteractionProvider
{
    bool CanInteract(ActionType type);
    bool TryInteraction(ActionType type, out InteractResult result);
}
