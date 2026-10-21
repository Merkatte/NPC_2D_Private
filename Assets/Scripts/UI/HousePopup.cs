using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HousePopup : PopBase
{
    [Serializable]
    private struct ItemIconEntry
    {
        [SerializeField] private int _itemId;
        [SerializeField] private Sprite _icon;
        public int ItemId => _itemId;
        public Sprite Icon => _icon;
    }

    [SerializeField] private HousingDataContext _housingDataContext;
    [SerializeField] private BuildingDataContext _buildingDataContext;
    [SerializeField] private WarmPixelHousePopup _presentation;
    [SerializeField] private Text _title;
    [SerializeField] private Text _currentTier;
    [SerializeField] private Text _occupancy;
    [SerializeField] private Text _summary;
    [SerializeField] private Image _currentImage;
    [SerializeField] private Image _nextImage;
    [SerializeField] private Text _nextTier;
    [SerializeField] private Text _progress;
    [SerializeField] private Text _result;
    [SerializeField] private Text _emptyResidents;
    [SerializeField] private Text _upgradeLabel;
    [SerializeField] private Text _costHeading;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _collapseButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _comparisonRoot;
    [SerializeField] private HouseEffectRowView _effectPrefab;
    [SerializeField] private HouseEffectRowView _comparisonPrefab;
    [SerializeField] private HouseResidentView _residentPrefab;
    [SerializeField] private ResourceCostRowView _costPrefab;
    [SerializeField] private Transform _effectsRoot;
    [SerializeField] private Transform _comparisonsRoot;
    [SerializeField] private Transform _residentsRoot;
    [SerializeField] private Transform _costsRoot;
    [SerializeField] private ScrollRect[] _scrollRects;
    [SerializeField] private Sprite _residentPortrait;
    [SerializeField] private ItemIconEntry[] _itemIcons = Array.Empty<ItemIconEntry>();
    private readonly List<HouseEffectRowView> _effects = new List<HouseEffectRowView>();
    private readonly List<HouseEffectRowView> _comparisons = new List<HouseEffectRowView>();
    private readonly List<HouseResidentView> _residents = new List<HouseResidentView>();
    private readonly List<ResourceCostRowView> _costs = new List<ResourceCostRowView>();
    private HousePopupSource _source;
    private House _house;
    private BuildingPlot _plot;
    private ResourceManager _resources;
    private bool _wasUpgrading;

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!(source is HousePopupSource houseSource) || !houseSource || !houseSource.TryGetClickPopup(out _)
            || !_housingDataContext || !_buildingDataContext || !_presentation || !_title || !_currentTier
            || !_occupancy || !_summary || !_currentImage || !_nextImage || !_nextTier || !_progress || !_result
            || !_emptyResidents || !_upgradeLabel || !_costHeading || !_upgradeButton || !_startButton
            || !_cancelButton || !_retryButton || !_collapseButton || !_closeButton || !_comparisonRoot
            || !_effectPrefab || !_effectPrefab.IsConfigured || !_comparisonPrefab || !_comparisonPrefab.IsConfigured
            || !_residentPrefab || !_residentPrefab.IsConfigured || !_costPrefab || !_costPrefab.IsConfigured
            || !_effectsRoot || !_comparisonsRoot || !_residentsRoot || !_costsRoot || !houseSource.Plot.Resources)
            return false;
        ClearSource();
        _source = houseSource;
        _house = houseSource.House;
        _plot = houseSource.Plot;
        _resources = _plot.Resources;
        _house.Changed += Refresh;
        _plot.StateChanged += Refresh;
        _resources.ResourcesChanged += Refresh;
        _result.text = string.Empty;
        Refresh();
        ResetScroll();
        return true;
    }

    private void OnEnable()
    {
        if (_upgradeButton) _upgradeButton.onClick.AddListener(ToggleDrawer);
        if (_startButton) _startButton.onClick.AddListener(Upgrade);
        if (_cancelButton) _cancelButton.onClick.AddListener(CancelConstruction);
        if (_retryButton) _retryButton.onClick.AddListener(RetryCompletion);
        if (_collapseButton) _collapseButton.onClick.AddListener(CollapseDrawer);
        if (_closeButton) _closeButton.onClick.AddListener(RequestClose);
        Refresh();
    }

    private void OnDisable()
    {
        if (_upgradeButton) _upgradeButton.onClick.RemoveListener(ToggleDrawer);
        if (_startButton) _startButton.onClick.RemoveListener(Upgrade);
        if (_cancelButton) _cancelButton.onClick.RemoveListener(CancelConstruction);
        if (_retryButton) _retryButton.onClick.RemoveListener(RetryCompletion);
        if (_collapseButton) _collapseButton.onClick.RemoveListener(CollapseDrawer);
        if (_closeButton) _closeButton.onClick.RemoveListener(RequestClose);
        ClearSource();
    }

    protected override void OnBeforeClose() => ClearSource();

    private void Update()
    {
        if (!ReferenceEquals(_source, null) && (!_source || !_source.TryGetClickPopup(out _))) RequestClose();
    }

    private void ClearSource()
    {
        if (_house) _house.Changed -= Refresh;
        if (_plot) _plot.StateChanged -= Refresh;
        if (_resources) _resources.ResourcesChanged -= Refresh;
        _source = null;
        _house = null;
        _plot = null;
        _resources = null;
        _wasUpgrading = false;
        if (_presentation) _presentation.ResetDrawer();
        ResetScroll();
    }

    private void ToggleDrawer()
    {
        if (_plot && _presentation) _presentation.SetDrawerOpen(!_presentation.IsDrawerOpen);
    }
    private void CollapseDrawer() { if (_presentation) _presentation.SetDrawerOpen(false); }

    private void Upgrade()
    {
        if (!_plot) return;
        _result.text = _plot.TryStartUpgrade(out string reason) ? string.Empty : reason;
        Refresh();
    }

    private void CancelConstruction()
    {
        if (!_plot) return;
        _result.text = _plot.TryCancelConstruction(out string reason) ? string.Empty : reason;
        Refresh();
    }

    private void RetryCompletion()
    {
        if (!_plot) return;
        _result.text = _plot.TryRetryCompletion(out _) ? string.Empty : "완공을 마무리하지 못했습니다.";
        Refresh();
    }

    private void Refresh()
    {
        if (!_house || !_plot || !_resources || _house.Tier == null) return;
        HouseTierDefinition current = _house.Tier;
        bool upgrading = _plot.State == BuildingPlotState.UnderConstruction && _plot.IsUpgrade;
        if (_wasUpgrading && !upgrading) _presentation.SetDrawerOpen(false);
        _wasUpgrading = upgrading;
        bool hasNext = _housingDataContext.TryGetNextTier(current.Tier, out var next);
        _title.text = "주택";
        _currentTier.text = $"{current.Tier}단계 주택";
        _occupancy.text = $"{_house.Residents.Count} / {_house.Capacity}명";
        _summary.text = upgrading ? $"업그레이드 진행 중 · {_plot.Progress:P0}" : hasNext ? "주민의 보금자리" : "최고 단계입니다.";
        ShowImage(_currentImage, current.BuildingId);
        RefreshResidents();
        RefreshEffects(current, next);
        _comparisonRoot.SetActive(hasNext && !upgrading);
        _nextImage.gameObject.SetActive(hasNext);
        _nextTier.text = hasNext ? $"{next.Tier}단계 주택" : "최고 단계";
        if (hasNext) ShowImage(_nextImage, next.BuildingId);
        _upgradeLabel.text = upgrading ? "업그레이드 현황" : "업그레이드";
        _upgradeButton.gameObject.SetActive(hasNext || upgrading);
        _upgradeButton.interactable = hasNext || upgrading;
        _startButton.gameObject.SetActive(hasNext && !upgrading);
        _startButton.interactable = false;
        _cancelButton.gameObject.SetActive(upgrading);
        _retryButton.gameObject.SetActive(upgrading && _plot.HasCompletionFailure);
        _progress.gameObject.SetActive(upgrading);
        _progress.text = upgrading ? $"진행 {_plot.Progress:P0} · 작업 {_plot.ReservedWorkers} / {_plot.Definition.MaxWorkers}명"
            + (_plot.HasCompletionFailure ? "\n완공을 마무리하지 못했습니다. 다시 시도할 수 있습니다." : string.Empty) : string.Empty;
        _costHeading.text = upgrading ? "취소 시 예상 환불 · 반환 / 보유" : "업그레이드 비용 · 필요 / 보유";
        if (upgrading) RefreshCosts(_plot.GetExpectedRefund(), false);
        else if (_plot.TryGetNextUpgrade(out var definition)) _startButton.interactable = RefreshCosts(definition.Cost);
        else HideAfter(_costs, 0);
    }

    private void RefreshResidents()
    {
        int index = 0;
        foreach (ResidentHousingState resident in _house.Residents)
            Row(_residents, _residentPrefab, _residentsRoot, index++).Bind(resident.Worker ? resident.Worker.name : "주민", _residentPortrait);
        HideAfter(_residents, index);
        _emptyResidents.gameObject.SetActive(index == 0);
        _emptyResidents.text = "입주한 주민이 없습니다.";
    }

    private void RefreshEffects(HouseTierDefinition current, HouseTierDefinition next)
    {
        int index = 0;
        foreach (HousingOption option in current.Options)
            Row(_effects, _effectPrefab, _effectsRoot, index++).Bind(EffectName(option.EffectType), EffectValue(option.EffectType, option.Value));
        if (_housingDataContext.LifeSettings != null)
        {
            float total = _housingDataContext.LifeSettings.HomeFatigueRecoveryPerSecond + current.GetEffect(HousingEffectType.FatigueRecovery);
            Row(_effects, _effectPrefab, _effectsRoot, index++).Bind("집 체류 시 피로 회복", $"{total:0.##}/초");
        }
        HideAfter(_effects, index);
        index = 0;
        if (next != null)
        {
            foreach (HousingOption option in next.Options)
            {
                bool existed = TryFindEffect(current, option.EffectType, out float before);
                string change = !existed ? "추가" : before == option.Value ? "유지" : "변경";
                string value = (existed ? EffectValue(option.EffectType, before) : "—") + " → " + EffectValue(option.EffectType, option.Value);
                Row(_comparisons, _comparisonPrefab, _comparisonsRoot, index++).Bind(EffectName(option.EffectType), value, change);
            }
            foreach (HousingOption option in current.Options)
                if (!TryFindEffect(next, option.EffectType, out _))
                    Row(_comparisons, _comparisonPrefab, _comparisonsRoot, index++).Bind(EffectName(option.EffectType), EffectValue(option.EffectType, option.Value) + " → —", "제거");
        }
        HideAfter(_comparisons, index);
    }

    private bool RefreshCosts(IEnumerable<KeyValuePair<int, int>> costs, bool isRequirement = true)
    {
        int index = 0;
        bool affordable = true;
        foreach (var cost in costs)
        {
            int owned = _resources.GetQuantity(cost.Key);
            string itemName = _resources.ItemData && _resources.ItemData.TryGetItemInfo(cost.Key, out var item) ? item.ItemName : $"Item {cost.Key}";
            Sprite icon = null;
            foreach (var entry in _itemIcons) if (entry.ItemId == cost.Key) { icon = entry.Icon; break; }
            Row(_costs, _costPrefab, _costsRoot, index++).Bind(itemName, icon, cost.Value, owned, isRequirement);
            affordable &= owned >= cost.Value;
        }
        HideAfter(_costs, index);
        return affordable;
    }

    private void ShowImage(Image image, int buildingId)
    {
        _buildingDataContext.TryGetAssets(buildingId, out _, out Sprite sprite);
        image.sprite = sprite;
        image.enabled = sprite;
    }

    private static bool TryFindEffect(HouseTierDefinition tier, HousingEffectType effect, out float value)
    {
        foreach (HousingOption option in tier.Options)
            if (option.EffectType == effect) { value = option.Value; return true; }
        value = 0f;
        return false;
    }

    private static string EffectName(HousingEffectType effect)
    {
        switch (effect)
        {
            case HousingEffectType.Capacity: return "정원";
            case HousingEffectType.DissatisfactionRecovery: return "불만 회복 보너스";
            case HousingEffectType.FatigueRecovery: return "집 피로 회복 보너스";
            case HousingEffectType.HungerRecovery: return "집 허기 회복 보너스";
            case HousingEffectType.ThirstRecovery: return "집 갈증 회복 보너스";
            default: return effect.ToString();
        }
    }

    private string EffectValue(HousingEffectType effect, float value)
    {
        if (effect == HousingEffectType.Capacity) return $"{value:0}명";
        return $"{value:0.##}/초";
    }

    private static T Row<T>(List<T> rows, T prefab, Transform parent, int index) where T : Component
    {
        if (index == rows.Count) rows.Add(Instantiate(prefab, parent));
        T row = rows[index];
        if (!row.gameObject.activeSelf) row.gameObject.SetActive(true);
        return row;
    }

    private static void HideAfter<T>(List<T> rows, int count) where T : Component
    {
        for (int i = count; i < rows.Count; ++i) rows[i].gameObject.SetActive(false);
    }

    private void ResetScroll()
    {
        if (_scrollRects == null) return;
        foreach (ScrollRect scroll in _scrollRects)
        {
            if (!scroll) continue;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }
    }
}
