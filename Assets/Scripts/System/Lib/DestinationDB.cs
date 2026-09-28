using System;
using System.Collections.Generic;
using UnityEngine;

public class DestinationDB : MonoBehaviour
{
    [SerializeField] private List<DestinationInfo> _destionations;
    [SerializeField] private InteractableManager _interactableManager;
    private Dictionary<BuildingType, List<DestinationInfo>> _destinationDB;
    private List<BuildingType> _registeredKeys;
    public IReadOnlyList<BuildingType> RegisteredKeys { get { EnsureInitialized(); return _registeredKeys.AsReadOnly(); } }
    private void Awake() { EnsureInitialized(); }
    private void EnsureInitialized()
    {
        if (_destinationDB != null) return;
        _destinationDB = new Dictionary<BuildingType, List<DestinationInfo>>();
        _registeredKeys = new List<BuildingType>();
        if (_destionations != null)
            foreach (DestinationInfo info in _destionations) Register(info);
    }
    public bool Register(DestinationInfo info)
    {
        EnsureInitialized();
        if (info == null || !info.DestinationLoc || !info.DestinationObject) return false;
        if (!_destinationDB.TryGetValue(info.BuildingType, out var entries))
        {
            _destinationDB.Add(info.BuildingType, entries = new List<DestinationInfo>());
            _registeredKeys.Add(info.BuildingType);
        }
        if (!entries.Contains(info)) entries.Add(info);
        return true;
    }
    public void Unregister(DestinationInfo info)
    {
        EnsureInitialized();
        if (info == null || !_destinationDB.TryGetValue(info.BuildingType, out var entries)) return;
        entries.Remove(info);
        if (entries.Count == 0) { _destinationDB.Remove(info.BuildingType); _registeredKeys.Remove(info.BuildingType); }
    }
    public IReadOnlyList<DestinationInfo> GetCandidates(BuildingType type)
    {
        EnsureInitialized();
        return _destinationDB.TryGetValue(type, out var entries) ? entries.AsReadOnly() : Array.Empty<DestinationInfo>();
    }
    public bool TryGetRegisteredInteractionProvider(DestinationInfo destination, ActionType action,
        out BaseInteractionProvider provider)
    {
        provider = null;
        return destination != null && _interactableManager
            && _interactableManager.TryGetRegisteredInteractionProvider(destination.DestinationObject, action, out provider);
    }
    // Compatibility queries are read-only. Selectors use the instance-bearing nearest result.
    public bool TryGetDestinationPos(BuildingType type, out Vector3 position)
    {
        position = default;
        foreach (DestinationInfo entry in GetCandidates(type))
            if (entry.DestinationLoc && entry.DestinationObject && entry.DestinationObject.activeInHierarchy)
            { position = entry.DestinationLoc.position; return true; }
        return false;
    }
    public bool TryGetInteractionProvider(BuildingType type, ActionType action, out IInteractionProvider provider)
    {
        provider = null;
        foreach (DestinationInfo entry in GetCandidates(type))
            if (_interactableManager && entry.DestinationObject && entry.DestinationObject.activeInHierarchy
                && _interactableManager.TryGetInteractionProvider(entry.DestinationObject, action, out provider)) return true;
        return false;
    }
}

[Serializable]
public class DestinationInfo
{
    public BuildingType BuildingType;
    public Transform DestinationLoc;
    public GameObject DestinationObject;
}
