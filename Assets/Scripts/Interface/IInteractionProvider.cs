using System.Collections.Generic;

public interface IInteractionProvider
{
    bool Supports(ActionType type);
    bool CanInteract(ActionType type);
    void AppendOptions(ActionType type, List<InteractionOption> buffer);
    bool TryInteract(InteractionRequest request, out InteractionResult result);
}
