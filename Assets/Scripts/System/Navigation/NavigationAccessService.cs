using System.Collections.Generic;
using UnityEngine;

public sealed class NavigationAccessService : INavigationService, INavigationRevision
{
    private readonly TilemapNavigation _navigation;
    private readonly NavigationAccess _access;
    public NavigationAccessService(TilemapNavigation navigation, NavigationAccess access)
    { _navigation = navigation; _access = access; }
    public bool IsReady => _navigation && _navigation.IsReady;
    public int Revision => _navigation ? _navigation.Revision : -1;
    public bool TryBuildPath(Vector3 start, Vector3 destination, List<Vector3> output, out NavigationFailure failure)
    {
        failure = NavigationFailure.InvalidConfiguration;
        if (!_navigation) { output?.Clear(); return false; }
        return _navigation.TryBuildPath(start, destination, output, out failure, _access);
    }
}
