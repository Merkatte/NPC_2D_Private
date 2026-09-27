using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConstructionPopup : PopBase
{
    [SerializeField] private BuildingDataContext _buildingDataContext;
    [SerializeField] private ConstructionChoiceRow _rowPrefab;
    [SerializeField] private Transform _rowsRoot;
    [SerializeField] private Text _details;
    [SerializeField] private Text _result;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _closeButton;
    private readonly List<ConstructionChoiceRow> _rows = new List<ConstructionChoiceRow>();
    private BuildingPlot _source;
    private ResourceManager _resources;
    private int _selectedId;

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!(source is BuildingPlot plot) || !plot || !plot.TryGetClickPopup(out _)
            || !_rowPrefab || !_rowsRoot || !_details || !_result || !_confirmButton || !_cancelButton || !_closeButton
            || !_buildingDataContext)
            return false;
        Unsubscribe();
        _source = plot;
        _resources = plot.Resources;
        _selectedId = 0;
        Subscribe();
        Refresh();
        return true;
    }
    private void OnEnable()
    {
        if (_confirmButton) _confirmButton.onClick.AddListener(Confirm);
        if (_cancelButton) _cancelButton.onClick.AddListener(CancelConstruction);
        if (_closeButton) _closeButton.onClick.AddListener(RequestClose);
        Subscribe();
        Refresh();
    }
    private void OnDisable()
    {
        if (_confirmButton) _confirmButton.onClick.RemoveListener(Confirm);
        if (_cancelButton) _cancelButton.onClick.RemoveListener(CancelConstruction);
        if (_closeButton) _closeButton.onClick.RemoveListener(RequestClose);
        Unsubscribe();
        _source = null;
        _resources = null;
    }
    protected override void OnBeforeClose() { Unsubscribe(); _source = null; _resources = null; }
    private void Update() { if (!ReferenceEquals(_source, null) && (!_source || !_source.isActiveAndEnabled)) RequestClose(); }
    private void Subscribe()
    {
        Unsubscribe();
        if (_source) _source.StateChanged += Refresh;
        if (_resources) _resources.ResourcesChanged += Refresh;
    }
    private void Unsubscribe()
    {
        if (_source) _source.StateChanged -= Refresh;
        if (_resources) _resources.ResourcesChanged -= Refresh;
    }
    private void Select(int id) { _selectedId = id; _result.text = string.Empty; Refresh(); }
    private void Confirm()
    {
        if (!_source) return;
        if (!_source.TryStartConstruction(_selectedId, out string reason)) _result.text = reason;
        Refresh();
    }
    private void CancelConstruction()
    {
        if (!_source) return;
        if (!_source.TryCancelConstruction(out string reason)) _result.text = reason;
        Refresh();
    }
    private void Refresh()
    {
        if (!_source || !_resources) return;
        if (_source.State == BuildingPlotState.Completed) { RequestClose(); return; }
        bool empty = _source.State == BuildingPlotState.Empty;
        int rowIndex = 0;
        if (empty)
        {
            foreach (BuildingDefinition definition in _source.Definitions)
            {
                if (!_source.IsAllowed(definition.Id)) continue;
                if (rowIndex == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _rowsRoot));
                _buildingDataContext.TryGetAssets(definition.Id, out _, out Sprite icon);
                _rows[rowIndex].gameObject.SetActive(true);
                _rows[rowIndex++].Bind(definition, icon, Select);
            }
        }
        for (int i = rowIndex; i < _rows.Count; ++i) _rows[i].gameObject.SetActive(false);
        var text = new StringBuilder();
        _confirmButton.gameObject.SetActive(empty);
        _cancelButton.gameObject.SetActive(!empty);
        _confirmButton.interactable = false;
        if (empty && _buildingDataContext.TryGetBuildingDefinition(_selectedId, out var selected))
        {
            text.AppendLine(selected.DisplayName);
            bool affordable = true;
            foreach (var entry in selected.Cost)
            {
                _resources.ItemData.TryGetItemInfo(entry.Key, out var item);
                int owned = _resources.GetQuantity(entry.Key);
                text.AppendLine($"{item.ItemName}: {owned} / {entry.Value}");
                affordable &= owned >= entry.Value;
            }
            _confirmButton.interactable = affordable;
        }
        else if (empty) text.Append("건설할 시설을 선택하세요.");
        else
        {
            text.AppendLine($"{_source.Definition.DisplayName}  {_source.Progress:P0}");
            text.AppendLine($"작업 인원 {_source.ReservedWorkers} / {_source.Definition.MaxWorkers}");
            text.AppendLine("취소 시 반환");
            foreach (var entry in _source.GetExpectedRefund())
            {
                _resources.ItemData.TryGetItemInfo(entry.Key, out var item);
                text.AppendLine($"{item.ItemName}: {entry.Value}");
            }
            if (_source.HasCompletionFailure) text.AppendLine("완공을 마무리하지 못했습니다.");
        }
        _details.text = text.ToString();
    }
}
