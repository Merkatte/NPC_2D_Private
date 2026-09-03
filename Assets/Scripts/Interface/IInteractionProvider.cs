using System.Collections.Generic;
using UnityEngine;

public interface IInteractionProvider
{
    bool Supports(ActionType type);
    bool CanInteract(ActionType type);
    bool TryGetActionPosition(ActionType type, Vector3 fallbackPosition, out Vector3 position);
    void AppendOptions(ActionType type, List<InteractionOption> buffer);
    bool TryInteract(InteractionRequest request, out InteractionResult result);
}
