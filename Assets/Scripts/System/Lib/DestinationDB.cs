using System.Collections.Generic;
using UnityEngine;

public class DestinationDB : MonoBehaviour
{
    [SerializeField] private List<DestinationInfo> _destionations;

    Dictionary<string, DestinationInfo> _destinationDB;
    void Awake()
    {
        Convert2Dict();
    }

    public bool TryGetDestinationPos(string destionationName, out Vector3 destination)
    {
        destination = Vector3.zero;

        if (!_destinationDB.TryGetValue(destionationName, out var info))
            return false;

        if (!info.DestinationLoc)
            return false;

        destination = info.DestinationLoc.position;
        return true;
    }

    private void Convert2Dict()
    {
        if(_destinationDB == null) 
            _destinationDB = new Dictionary<string, DestinationInfo>();

        foreach (var info in _destionations)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.DestinationName))
                continue;

            if (!info.DestinationLoc)
                continue;

            _destinationDB[info.DestinationName] = info;
        }
    }
}

[System.Serializable]
public class DestinationInfo
{
    public string DestinationName;
    public Transform DestinationLoc;
    public GameObject DestinationObject;
}
