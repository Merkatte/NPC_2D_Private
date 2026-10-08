using UnityEngine;

public sealed class HousePopupSource : MonoBehaviour, IClickPopupSource
{
    [SerializeField] private House _house;
    private BuildingPlot _plot;
    public House House => _house;
    public BuildingPlot Plot => _plot;

    public void Configure(House house, BuildingPlot plot)
    {
        _house = house;
        _plot = plot;
    }

    public bool TryGetClickPopup(out PopupType type)
    {
        type = isActiveAndEnabled && _house && _house.isActiveAndEnabled && _house.Tier != null
            && _plot && _plot.isActiveAndEnabled ? PopupType.House : PopupType.None;
        return type != PopupType.None;
    }
}
