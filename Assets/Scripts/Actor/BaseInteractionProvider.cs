using System.Collections.Generic;
using UnityEngine;

public abstract class BaseInteractionProvider : MonoBehaviour, IInteractionProvider
{
    private bool _initializationAttempted;
    private bool _isOperational;
    private string _initializationFailureReason;

    public bool TryInitialize(out string failureReason)
    {
        if (_initializationAttempted)
        {
            failureReason = _initializationFailureReason;
            return _isOperational;
        }

        _initializationAttempted = true;
        _isOperational = TryInitializeCore(out _initializationFailureReason);
        failureReason = _initializationFailureReason;
        return _isOperational;
    }

    public bool Supports(ActionType type)
        => SupportsCore(type);

    public bool CanInteract(ActionType type)
    {
        EnsureInitialized();
        return SupportsCore(type) && _isOperational && CanInteractCore(type);
    }

    public bool TryGetActionPosition(ActionType type, Vector3 fallbackPosition, out Vector3 position)
    {
        position = fallbackPosition;

        if (!CanInteract(type))
            return false;

        if (TryGetActionPositionCore(type, fallbackPosition, out position))
            return true;

        position = fallbackPosition;
        return false;
    }

    public void AppendOptions(ActionType type, List<InteractionOption> buffer)
    {
        if (buffer == null || !CanInteract(type))
            return;

        AppendOptionsCore(type, buffer);
    }

    public bool TryInteract(InteractionRequest request, out InteractionResult result)
    {
        result = default;

        if (!request.HasValidStrength || !CanInteract(request.Type))
            return false;

        return TryInteractCore(request, out result);
    }

    protected abstract bool SupportsCore(ActionType type);

    protected virtual bool CanInteractCore(ActionType type)
        => true;

    protected virtual bool TryGetActionPositionCore(ActionType type, Vector3 fallbackPosition, out Vector3 position)
    {
        position = fallbackPosition;
        return true;
    }

    protected abstract bool TryInitializeCore(out string failureReason);

    protected virtual void AppendOptionsCore(ActionType type, List<InteractionOption> buffer)
    {
    }

    protected abstract bool TryInteractCore(InteractionRequest request, out InteractionResult result);

    private void EnsureInitialized()
    {
        if (!_initializationAttempted)
            TryInitialize(out _);
    }
}
