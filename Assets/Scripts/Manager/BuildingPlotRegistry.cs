using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingPlotRegistry : MonoBehaviour
{
    [SerializeField] private BuildingPlot[] _plots = Array.Empty<BuildingPlot>();
    private IReadOnlyList<BuildingPlot> _view;
    private long _nextOrder;
    public IReadOnlyList<BuildingPlot> Plots => _view ?? (_view = Array.AsReadOnly(_plots));
    internal long NextApplicationOrder() => ++_nextOrder;
}
