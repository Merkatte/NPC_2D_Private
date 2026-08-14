using System.Collections.Generic;
using UnityEngine;

public class DestinationDB : MonoBehaviour
{
    [SerializeField] private List<DestinationInfo> _destionations;

    private Dictionary<BuildingType, DestinationInfo> _destinationDB;
    private List<BuildingType> _registeredKeys;

    public IReadOnlyList<BuildingType> RegisteredKeys
    {
        get
        {
            EnsureInitialized();
            return _registeredKeys;
        }
    }

    void Awake()
    {
        EnsureInitialized();
    }

    public bool TryGetDestinationPos(BuildingType destionationName, out Vector3 destination)
    {
        EnsureInitialized();
        destination = Vector3.zero;

        if (!_destinationDB.TryGetValue(destionationName, out var info))
            return false;

        if (!info.DestinationLoc)
            return false;

        destination = info.DestinationLoc.position;
        return true;
    }

    public bool TryGetInteractionProvider(BuildingType destinationName, out IInteractionProvider provider)
    {
        EnsureInitialized();
        provider = null;

        if (!_destinationDB.TryGetValue(destinationName, out var info))
            return false;

        if (info.InteractProvider == null)
            return false;

        provider = info.InteractProvider;
        return true;
    }

    private void EnsureInitialized()
    {
        if (_destinationDB != null)
            return;

        Convert2Dict();
    }

    private void Convert2Dict()
    {
        _destinationDB = new Dictionary<BuildingType, DestinationInfo>();
        _registeredKeys = new List<BuildingType>();

        if (_destionations == null)
            return;

        foreach (var info in _destionations)
        {
            if (info == null)
                continue;

            if (!info.DestinationLoc)
                continue;

            if (!_destinationDB.ContainsKey(info.BuildingType))
                _registeredKeys.Add(info.BuildingType);

            _destinationDB[info.BuildingType] = info;
        }
    }
}

[System.Serializable]
public class DestinationInfo
{
    public BuildingType BuildingType;
    public Transform DestinationLoc;
    public GameObject DestinationObject;
    public BaseInteractable InteractProvider;
}
