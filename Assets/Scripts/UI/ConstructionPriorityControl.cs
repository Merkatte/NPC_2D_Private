using UnityEngine.UI;

internal sealed class ConstructionPriorityControl
{
    private BuildingPlot _plot;
    private BuildingPlotRegistry _registry;
    private Button _button;
    private LocalizeText _label;
    private long _shownOrder;
    private bool _showsClear;

    public void Bind(BuildingPlot plot, Button button, LocalizeText label)
    {
        Unbind();
        if (!button) return;
        if (!label || !plot || !plot.Registry)
        {
            button.gameObject.SetActive(false);
            return;
        }
        _plot = plot;
        _registry = plot.Registry;
        _button = button;
        _label = label;
        _button.onClick.AddListener(Apply);
        _registry.PrioritiesChanged += Refresh;
        _plot.StateChanged += Refresh;
        Refresh();
    }

    public void Unbind()
    {
        if (_plot) _plot.StateChanged -= Refresh;
        if (_registry) _registry.PrioritiesChanged -= Refresh;
        if (_button) _button.onClick.RemoveListener(Apply);
        _plot = null;
        _registry = null;
        _button = null;
        _label = null;
    }

    private void Refresh()
    {
        if (!_button) return;
        bool visible = _plot && _registry && _plot.State == BuildingPlotState.UnderConstruction;
        _showsClear = visible && _registry.GetPriorityRank(_plot) > 0;
        visible = visible && (_showsClear || _registry.CanSetPriority(_plot));
        _button.gameObject.SetActive(visible);
        if (!visible) return;
        _shownOrder = _plot.ApplicationOrder;
        LocalizeKey key = _showsClear ? LocalizeKey.ConstructionPriorityClear
            : _registry.Priorities.Count > 0 ? LocalizeKey.ConstructionPriorityReplace
            : LocalizeKey.ConstructionPrioritySet;
        _label.SetKey(key);
    }

    private void Apply()
    {
        if (!_plot || !_registry) return;
        if (_showsClear) _registry.TryClearPriority(_plot, _shownOrder);
        else _registry.TrySetPriority(_plot, _shownOrder);
        Refresh();
    }
}
