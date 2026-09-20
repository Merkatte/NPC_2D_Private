using System.Collections.Generic;
using UnityEngine;

public interface INavigationService
{
    bool IsReady { get; }

    // The caller owns output. Failure leaves it empty, never a partial route.
    bool TryBuildPath(Vector3 start, Vector3 destination, List<Vector3> output,
        out NavigationFailure failure);
}
