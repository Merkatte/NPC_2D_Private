using UnityEngine;

public interface IMoveTarget
{
    bool TryGetPosition(out Vector3 position);
}
