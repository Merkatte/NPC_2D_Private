using System.Collections.Generic;
using UnityEngine;

public sealed class HousingAssignmentPolicy
{
    private readonly List<Vector3> _path = new List<Vector3>();

    public static int CompareResidents(ResidentHousingState left, ResidentHousingState right)
    {
        int dissatisfaction = right.Stat.CurrentDissatisfaction.CompareTo(left.Stat.CurrentDissatisfaction);
        return dissatisfaction != 0 ? dissatisfaction : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
    }

    public House SelectNearest(ResidentHousingState resident, IReadOnlyList<House> houses, INavigationService navigation)
    {
        if (!resident.Component || navigation == null || !navigation.IsReady) return null;
        House selected = null;
        float nearest = float.PositiveInfinity;
        foreach (House house in houses)
        {
            if (!house || !house.isActiveAndEnabled || house.Residents.Count >= house.Capacity) continue;
            float distance = (resident.Component.Position - house.EntrancePosition).sqrMagnitude;
            if (distance >= nearest || !navigation.TryBuildPath(resident.Component.Position, house.EntrancePosition, _path, out _)) continue;
            selected = house;
            nearest = distance;
        }
        _path.Clear();
        return selected;
    }
}
