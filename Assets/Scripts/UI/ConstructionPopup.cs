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
    [System.Serializable]
    private struct ItemIconEntry
    {
        [SerializeField] private int _itemId;
        [SerializeField] private Sprite _icon;
        public int ItemId => _itemId;
        public Sprite Icon => _icon;
    }
    [System.Serializable]
    private struct RoleDescription
    {
        [SerializeField] private BuildingType _type;
        [SerializeField, TextArea] private string _description;
        public BuildingType Type => _type;
        public string Description => _description;
    }
    [SerializeField] private ResourceCostRowView _costPrefab;
    [SerializeField] private Transform _costsRoot;
    [SerializeField] private ScrollRect _costScrollRect;
    [SerializeField] private ScrollRect _choiceScrollRect;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Text _selectedName;
    [SerializeField] private Text _costHeading;
    [SerializeField] private Text _buildingCount;
    [SerializeField] private ItemIconEntry[] _itemIcons;
    [SerializeField] private RoleDescription[] _roleDescriptions;
    private readonly List<ResourceCostRowView> _costViews = new List<ResourceCostRowView>();
    private readonly List<ConstructionChoiceRow> _rows = new List<ConstructionChoiceRow>();
    private BuildingPlot _source;
    private ResourceManager _resources;
    private int _selectedId;

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!(source is BuildingPlot plot) || !plot || !plot.TryGetClickPopup(out _)
            || !_rowPrefab || !_rowsRoot || !_details || !_result || !_confirmButton || !_cancelButton || !_closeButton
            || !_buildingDataContext || !_costPrefab || !_costPrefab.IsConfigured || !_costsRoot
            || !_selectedImage || !_selectedName || !_costHeading || !_buildingCount || !plot.Resources)
            return false;
        Unsubscribe();
        _source = plot;
        _resources = plot.Resources;
        _selectedId = 0;
        _result.text = string.Empty;
        Subscribe();
        Refresh();
        ResetCostScroll();
        ResetChoiceScroll();
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
        _selectedId = 0;
        ResetCostScroll();
        ResetCostRows();
        ResetChoiceScroll();
    }
    protected override void OnBeforeClose()
    {
        Unsubscribe();
        _source = null;
        _resources = null;
        _selectedId = 0;
        ResetCostScroll();
        ResetCostRows();
        ResetChoiceScroll();
    }
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
    private void Select(int id) { _selectedId = id; _result.text = string.Empty; Refresh(); ResetCostScroll(); }
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
        if (!_source || !_resources) { ResetCostRows(); return; }
        if (!_source.TryGetClickPopup(out _)) { RequestClose(); return; }
        bool empty = _source.State == BuildingPlotState.Empty;
        int rowIndex = 0;
        if (empty)
        {
            foreach (BuildingDefinition definition in _source.Definitions)
            {
                if (!_source.IsAllowed(definition.Id)) continue;
                if (_selectedId == 0) _selectedId = definition.Id;
                if (rowIndex == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _rowsRoot));
                _buildingDataContext.TryGetAssets(definition.Id, out _, out Sprite icon);
                _rows[rowIndex].gameObject.SetActive(true);
                _rows[rowIndex++].Bind(definition, icon, Select);
                _rows[rowIndex - 1].SetSelected(definition.Id == _selectedId);
            }
        }
        for (int i = rowIndex; i < _rows.Count; ++i) _rows[i].gameObject.SetActive(false);
        _buildingCount.text = empty ? $"{rowIndex}개 시설" : "공사 진행 중";
        var text = new StringBuilder();
        _confirmButton.gameObject.SetActive(empty);
        _cancelButton.gameObject.SetActive(!empty);
        _confirmButton.interactable = false;
        if (empty && _buildingDataContext.TryGetBuildingDefinition(_selectedId, out var selected))
        {
            _selectedName.text = selected.DisplayName;
            _buildingDataContext.TryGetAssets(selected.Id, out _, out Sprite selectedIcon);
            _selectedImage.sprite = selectedIcon;
            _selectedImage.enabled = selectedIcon;
            if (_roleDescriptions != null)
                foreach (var role in _roleDescriptions)
                    if (role.Type == selected.BuildingType) { text.AppendLine(role.Description); break; }
            if (selected.ProvidedCapacity > 0) text.AppendLine($"제공 용량 {selected.ProvidedCapacity}");
            _costHeading.text = "건설 비용 · 필요 / 보유";
            _confirmButton.interactable = ShowCosts(selected.Cost);
        }
        else if (empty)
        {
            ResetCostRows();
            _selectedName.text = "건설 가능한 시설 없음";
            _selectedImage.enabled = false;
            _costHeading.text = string.Empty;
            text.Append("이 부지에 건설할 수 있는 시설이 없습니다.");
        }
        else
        {
            _selectedName.text = _source.Definition.DisplayName;
            _buildingDataContext.TryGetAssets(_source.Definition.Id, out _, out Sprite icon);
            _selectedImage.sprite = icon;
            _selectedImage.enabled = icon;
            text.AppendLine($"진행 {_source.Progress:P0}");
            text.AppendLine($"작업 인원 {_source.ReservedWorkers} / {_source.Definition.MaxWorkers}");
            _costHeading.text = "취소 시 예상 환불 · 반환 / 보유";
            ShowCosts(_source.GetExpectedRefund(), false);
            if (_source.HasCompletionFailure) text.AppendLine("완공을 마무리하지 못했습니다.");
        }
        _details.text = text.ToString();
    }

    private string GetItemName(int itemId)
    {
        if (_resources.ItemData && _resources.ItemData.TryGetItemInfo(itemId, out var item)
            && !string.IsNullOrEmpty(item.ItemName))
            return item.ItemName;
        return $"Item {itemId}";
    }
    private bool ShowCosts(IEnumerable<KeyValuePair<int, int>> costs, bool isRequirement = true)
    {
        int index = 0;
        bool affordable = true;
        foreach (var cost in costs)
        {
            if (index == _costViews.Count) _costViews.Add(Instantiate(_costPrefab, _costsRoot));
            ResourceCostRowView row = _costViews[index++];
            if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
            Sprite icon = null;
            if (_itemIcons != null)
                foreach (var entry in _itemIcons) if (entry.ItemId == cost.Key) { icon = entry.Icon; break; }
            int owned = _resources.GetQuantity(cost.Key);
            row.Bind(GetItemName(cost.Key), icon, cost.Value, owned, isRequirement);
            affordable &= owned >= cost.Value;
        }
        for (int i = index; i < _costViews.Count; ++i) _costViews[i].gameObject.SetActive(false);
        return affordable;
    }
    private void ResetCostRows()
    {
        foreach (var row in _costViews) if (row) row.gameObject.SetActive(false);
    }
    private void ResetChoiceScroll()
    {
        if (!_choiceScrollRect) return;
        Canvas.ForceUpdateCanvases();
        _choiceScrollRect.StopMovement();
        _choiceScrollRect.verticalNormalizedPosition = 1f;
    }
    private void ResetCostScroll()
    {
        if (!_costScrollRect) return;
        if (_costScrollRect.gameObject.activeInHierarchy) Canvas.ForceUpdateCanvases();
        _costScrollRect.StopMovement();
        _costScrollRect.verticalNormalizedPosition = 1f;
    }

}
