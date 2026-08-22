using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [SerializeField] private BaseInteractionProvider[] _interactables;

    private readonly Dictionary<(GameObject Owner, ActionType Type), BaseInteractionProvider> _providerCache
        = new Dictionary<(GameObject, ActionType), BaseInteractionProvider>();

    private bool _initialized;

    void Awake()
    {
        EnsureInitialized();
    }

    public bool TryGetInteractionProvider(GameObject destinationObject, ActionType actionType, out IInteractionProvider provider)
    {
        EnsureInitialized();
        provider = null;

        if (!destinationObject)
            return false;

        if (!_providerCache.TryGetValue((destinationObject, actionType), out var component))
            return false;

        if (!component || !component.CanInteract(actionType))
            return false;

        provider = component;
        return true;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        BuildRegistry();
    }

    private void BuildRegistry()
    {
        _providerCache.Clear();

        if (_interactables == null)
            return;

        foreach (var provider in _interactables)
        {
            if (!provider)
            {
                Debug.LogError("InteractableManager: null or destroyed entry in _interactables.");
                continue;
            }

            if (!provider.TryInitialize(out string failureReason))
                Debug.LogError($"InteractableManager: {provider.name} failed to initialize: {failureReason}");

            RegisterProvider(provider);
        }
    }

    private void RegisterProvider(BaseInteractionProvider provider)
    {
        foreach (ActionType type in Enum.GetValues(typeof(ActionType)))
        {
            if (!provider.Supports(type))
                continue;

            var key = (provider.gameObject, type);
            if (_providerCache.ContainsKey(key))
            {
                Debug.LogError($"InteractableManager: duplicate provider for ({provider.gameObject.name}, {type}); keeping the first registered provider.");
                continue;
            }

            _providerCache[key] = provider;
        }
    }
}
