using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarehousePopup : PopBase
{
    [SerializeField] private ResourceQuantityRow _rowPrefab;
    [SerializeField] private Transform _rowsRoot;
    [SerializeField] private Text _capacityText;
    [SerializeField] private Button _closeButton;
    private readonly List<ResourceQuantityRow> _rows = new List<ResourceQuantityRow>();
    private WarehouseDepositPoint _source;
    private ResourceManager _resources;
    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!(source is WarehouseDepositPoint warehouse) || !warehouse || !warehouse.Resources
            || !_rowPrefab || !_rowsRoot || !_capacityText || !_closeButton) return false;
        Unsubscribe();
        _source = warehouse;
        _resources = warehouse.Resources;
        _resources.ResourcesChanged += Refresh;
        Refresh();
        return true;
    }
    private void OnEnable() { if (_closeButton) _closeButton.onClick.AddListener(RequestClose); Refresh(); }
    private void OnDisable()
    {
        if (_closeButton) _closeButton.onClick.RemoveListener(RequestClose);
        Unsubscribe(); _source = null; _resources = null;
    }
    protected override void OnBeforeClose() { Unsubscribe(); _source = null; _resources = null; }
    private void Unsubscribe() { if (_resources) _resources.ResourcesChanged -= Refresh; }
    private void Update() { if (!ReferenceEquals(_source, null) && (!_source || !_source.isActiveAndEnabled)) RequestClose(); }
    private void Refresh()
    {
        if (!_resources || !_resources.ItemData) return;
        _capacityText.text = $"공용 창고  {_resources.UsedCapacity} / {_resources.Capacity}";
        int row = 0;
        foreach (var category in _resources.ItemData.ItemInfos().Values)
        foreach (ItemInfo item in category)
        {
            if (!item.ShowInWarehouse) continue;
            if (row == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _rowsRoot));
            _rows[row].gameObject.SetActive(true);
            _rows[row++].Bind(item.ItemName, _resources.GetQuantity(item.ID));
        }
        for (int i = row; i < _rows.Count; ++i) _rows[i].gameObject.SetActive(false);
    }
}
