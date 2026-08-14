using System.Collections.Generic;

public interface IInteractionProvider
{
    bool CanInteract(ActionType type);
    void AppendOptions(ActionType type, List<InteractionOption> buffer);
    bool TryInteraction(InteractRequest request, out InteractResult result);
}
