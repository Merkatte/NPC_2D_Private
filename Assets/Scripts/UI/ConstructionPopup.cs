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
    [SerializeField] private CostRowBinding[] _costRows;
    [SerializeField] private ScrollRect _costScrollRect;
    [SerializeField] private ScrollRect _choiceScrollRect;
    [SerializeField] private Color _costNormalColor = new Color(0.25f, 0.15f, 0.08f, 1f);
    [SerializeField] private Color _costShortageColor = new Color(0.75f, 0.12f, 0.08f, 1f);
    private bool _hasValidatedCostRows;
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
        ValidateCostRows();
        Unsubscribe();
        _source = plot;
        _resources = plot.Resources;
        _selectedId = 0;
        Subscribe();
        Refresh();
        ResetCostScroll();
        ResetChoiceScroll();
        return true;
    }
    private void OnEnable()
    {
        ValidateCostRows();
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
    }
    protected override void OnBeforeClose()
    {
        Unsubscribe();
        _source = null;
        _resources = null;
        _selectedId = 0;
        ResetCostScroll();
        ResetCostRows();
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
            bool showCostRows = CanShowCostRows(selected);
            ResetCostRows(showCostRows ? selected : null);
            if (showCostRows) text.AppendLine("보유 / 필요");
            bool affordable = true;
            foreach (var entry in selected.Cost)
            {
                int owned = _resources.GetQuantity(entry.Key);
                if (showCostRows)
                    FindCostRow(entry.Key).Show(owned, entry.Value, _costNormalColor, _costShortageColor);
                else
                    text.AppendLine($"{GetItemName(entry.Key)}: {owned} / {entry.Value}");
                affordable &= owned >= entry.Value;
            }
            _confirmButton.interactable = affordable;
        }
        else if (empty)
        {
            ResetCostRows();
            text.Append("건설할 시설을 선택하세요.");
        }
        else
        {
            ResetCostRows();
            text.AppendLine($"{_source.Definition.DisplayName}  {_source.Progress:P0}");
            text.AppendLine($"작업 인원 {_source.ReservedWorkers} / {_source.Definition.MaxWorkers}");
            text.AppendLine("취소 시 반환");
            foreach (var entry in _source.GetExpectedRefund())
            {
                text.AppendLine($"{GetItemName(entry.Key)}: {entry.Value}");
            }
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
    private void ValidateCostRows()
    {
        if (_hasValidatedCostRows) return;
        _hasValidatedCostRows = true;
        if (_costRows == null) return;
        var itemIds = new HashSet<int>();
        foreach (CostRowBinding row in _costRows)
        {
            if (row == null)
            {
                Debug.LogError($"ConstructionPopup '{name}': missing _costRows binding; using text costs.", this);
                continue;
            }
            row.Validate(itemIds, this);
        }
    }
    private bool CanShowCostRows(BuildingDefinition definition)
    {
        if (definition.Cost.Count == 0) return false;
        foreach (var entry in definition.Cost)
            if (FindCostRow(entry.Key) == null) return false;
        return true;
    }
    private CostRowBinding FindCostRow(int itemId)
    {
        if (_costRows == null) return null;
        foreach (CostRowBinding row in _costRows)
            if (row != null && row.ItemId == itemId && row.IsConfigured) return row;
        return null;
    }
    private void ResetCostRows(BuildingDefinition visibleDefinition = null)
    {
        bool showScroll = visibleDefinition != null;
        if (_costScrollRect && _costScrollRect.gameObject.activeSelf != showScroll)
            _costScrollRect.gameObject.SetActive(showScroll);
        if (_costRows == null) return;
        foreach (CostRowBinding row in _costRows)
            if (row != null && (visibleDefinition == null || !visibleDefinition.Cost.ContainsKey(row.ItemId)
                || FindCostRow(row.ItemId) != row))
                row.Reset(_costNormalColor);
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

    [System.Serializable]
    private sealed class CostRowBinding
    {
        [SerializeField] private int _itemId;
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _quantity;
        private bool _isValid;
        public int ItemId => _itemId;
        public bool IsConfigured => _isValid && _root && _icon && _icon.sprite && _quantity;

        public void Validate(HashSet<int> itemIds, ConstructionPopup owner)
        {
            _isValid = _itemId > 0 && _root && _icon && _icon.sprite && _quantity && itemIds.Add(_itemId);
            if (!_isValid)
                Debug.LogError($"ConstructionPopup '{owner.name}': invalid _costRows item {_itemId}; require unique item ID, root, icon sprite and quantity Text. Using text costs.", owner);
        }
        public void Show(int owned, int required, Color normalColor, Color shortageColor)
        {
            _quantity.text = $"{owned} / {required}";
            _quantity.color = owned < required ? shortageColor : normalColor;
            if (!_root.activeSelf) _root.SetActive(true);
        }
        public void Reset(Color normalColor)
        {
            if (_root && _root.activeSelf) _root.SetActive(false);
            if (_quantity)
            {
                _quantity.text = string.Empty;
                _quantity.color = normalColor;
            }
        }
    }
}
