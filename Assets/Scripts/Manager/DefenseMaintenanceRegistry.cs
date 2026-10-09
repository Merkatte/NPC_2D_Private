using System.Collections.Generic;
using UnityEngine;

public sealed class DefenseMaintenanceRegistry : MonoBehaviour
{
    private readonly List<DefenseMaintenanceSite> _sites = new List<DefenseMaintenanceSite>();
    private long _nextOrder;
    public long NextOrder() => ++_nextOrder;
    public void Register(DefenseMaintenanceSite site) { if (site && !_sites.Contains(site)) _sites.Add(site); }
    public void Unregister(DefenseMaintenanceSite site) { _sites.Remove(site); }
    public DefenseMaintenanceSite FindAvailable()
    {
        DefenseMaintenanceSite best = null;
        foreach (DefenseMaintenanceSite site in _sites)
        {
            if (!site || !site.CanReserve) continue;
            if (!best || site.Priority < best.Priority || (site.Priority == best.Priority && site.Order < best.Order)) best = site;
        }
        return best;
    }
    public bool HasHigherPriority(DefenseMaintenanceSite current)
    {
        DefenseMaintenanceSite next = FindAvailable();
        return next && (!current || next.Priority < current.Priority);
    }
}
