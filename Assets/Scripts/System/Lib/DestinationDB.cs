using System.Collections.Generic;
using UnityEngine;

public class DestinationDB : MonoBehaviour
{
    [SerializeField] private List<DestinationInfo> _destionations;

    Dictionary<BuildingType, DestinationInfo> _destinationDB;
    void Awake()
    {
        Convert2Dict();
    }

    public bool TryGetDestinationPos(BuildingType destionationName, out Vector3 destination)
    {
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
        provider = null;
        
        if (!_destinationDB.TryGetValue(destinationName, out var info))
            return false;

        if (info.InteractProvider == null)
            return false;
        
        provider = info.InteractProvider;
        return true;
    }

    private void Convert2Dict()
    {
        if(_destinationDB == null) 
            _destinationDB = new Dictionary<BuildingType, DestinationInfo>();

        foreach (var info in _destionations)
        {
            if (info == null)
                continue;

            if (!info.DestinationLoc)
                continue;

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
