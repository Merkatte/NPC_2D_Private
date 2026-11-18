// Capture the order and definition while the project still belongs to this plot.
public readonly struct ConstructionProjectInfo
{
    public BuildingPlot Plot { get; }
    public long ApplicationOrder { get; }
    public BuildingDefinition Definition { get; }
    public bool IsUpgrade { get; }

    internal ConstructionProjectInfo(BuildingPlot plot)
    {
        Plot = plot;
        ApplicationOrder = plot.ApplicationOrder;
        Definition = plot.Definition;
        IsUpgrade = plot.IsUpgrade;
    }

    internal bool IsCurrent => Plot && Plot.isActiveAndEnabled &&
        Plot.State == BuildingPlotState.UnderConstruction && Plot.ApplicationOrder == ApplicationOrder;
}
