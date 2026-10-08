using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class HousePopup : PopBase
{
    [SerializeField] private HousingDataContext _housingDataContext;
    [SerializeField] private HouseInfoRow _rowPrefab;
    [SerializeField] private Transform _rowsRoot;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Text _title;
    [SerializeField] private Text _result;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _closeButton;
    private readonly List<HouseInfoRow> _rows = new List<HouseInfoRow>();
    private HousePopupSource _source;
    private House _house;
    private BuildingPlot _plot;
    private ResourceManager _resources;
    private int _rowIndex;

    internal override bool TryBindSource(IClickPopupSource source)
    {
        if (!(source is HousePopupSource houseSource) || !houseSource || !houseSource.TryGetClickPopup(out _)
            || !_housingDataContext || !_rowPrefab || !_rowPrefab.IsConfigured || !_rowsRoot || !_scrollRect
            || !_title || !_result || !_upgradeButton || !_cancelButton || !_retryButton || !_closeButton
            || !houseSource.Plot.Resources)
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
        if (_upgradeButton) _upgradeButton.onClick.AddListener(Upgrade);
        if (_cancelButton) _cancelButton.onClick.AddListener(CancelConstruction);
        if (_retryButton) _retryButton.onClick.AddListener(RetryCompletion);
        if (_closeButton) _closeButton.onClick.AddListener(RequestClose);
        Refresh();
    }

    private void OnDisable()
    {
        if (_upgradeButton) _upgradeButton.onClick.RemoveListener(Upgrade);
        if (_cancelButton) _cancelButton.onClick.RemoveListener(CancelConstruction);
        if (_retryButton) _retryButton.onClick.RemoveListener(RetryCompletion);
        if (_closeButton) _closeButton.onClick.RemoveListener(RequestClose);
        ClearSource();
    }

    protected override void OnBeforeClose() => ClearSource();

    private void Update()
    {
        if (!ReferenceEquals(_source, null) && (!_source || !_source.TryGetClickPopup(out _)))
            RequestClose();
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
        ResetScroll();
    }

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
        _title.text = $"주택 {current.Tier}단계  ·  {_house.Residents.Count} / {_house.Capacity}명";
        _rowIndex = 0;
        AddRow("거주민");
        foreach (ResidentHousingState resident in _house.Residents)
            AddRow(resident.Worker ? resident.Worker.name : "주민");
        if (_house.Residents.Count == 0) AddRow("입주한 주민이 없습니다.");
        AddRow("현재 효과");
        foreach (HousingOption option in current.Options)
            AddRow(EffectText(option.EffectType, option.Value));

        bool upgrading = _plot.State == BuildingPlotState.UnderConstruction && _plot.IsUpgrade;
        bool hasNext = _housingDataContext.TryGetNextTier(current.Tier, out var next);
        _upgradeButton.gameObject.SetActive(!upgrading && hasNext);
        _upgradeButton.interactable = false;
        _cancelButton.gameObject.SetActive(upgrading);
        _retryButton.gameObject.SetActive(upgrading && _plot.HasCompletionFailure);
        if (hasNext)
        {
            AddRow($"다음: {next.Tier}단계 · 전체 효과 비교");
            AddComparison(current, next);
        }
        else AddRow("최고 단계입니다.");

        if (upgrading)
        {
            AddRow($"업그레이드 {_plot.Progress:P0} · 작업 {_plot.ReservedWorkers} / {_plot.Definition.MaxWorkers}명");
            AddRow("취소 시 반환");
            foreach (var cost in _plot.GetExpectedRefund()) AddRow($"{ItemName(cost.Key)}: {cost.Value}");
            if (_plot.HasCompletionFailure) AddRow("완공을 마무리하지 못했습니다. 다시 시도할 수 있습니다.");
        }
        else if (_plot.TryGetNextUpgrade(out var definition))
        {
            AddRow("업그레이드 비용 · 보유 / 필요");
            bool affordable = true;
            foreach (var cost in definition.Cost)
            {
                int owned = _resources.GetQuantity(cost.Key);
                bool shortage = owned < cost.Value;
                AddRow($"{ItemName(cost.Key)}: {owned} / {cost.Value}", shortage);
                affordable &= !shortage;
            }
            _upgradeButton.interactable = affordable;
        }
        for (int i = _rowIndex; i < _rows.Count; ++i) _rows[i].gameObject.SetActive(false);
    }

    private void AddComparison(HouseTierDefinition current, HouseTierDefinition next)
    {
        foreach (HousingOption option in next.Options)
        {
            bool hadEffect = TryFindEffect(current, option.EffectType, out float before);
            string change = !hadEffect ? "추가" : before == option.Value ? "유지" : "변경";
            AddRow($"{change}: {EffectText(option.EffectType, option.Value)}"
                + (hadEffect && before != option.Value ? $" (기존 {before:0.##})" : string.Empty));
        }
        foreach (HousingOption option in current.Options)
            if (!TryFindEffect(next, option.EffectType, out _)) AddRow($"제거: {EffectText(option.EffectType, option.Value)}");
    }

    private static bool TryFindEffect(HouseTierDefinition tier, HousingEffectType effect, out float value)
    {
        foreach (HousingOption option in tier.Options)
            if (option.EffectType == effect) { value = option.Value; return true; }
        value = 0f;
        return false;
    }

    private static string EffectText(HousingEffectType effect, float value)
    {
        switch (effect)
        {
            case HousingEffectType.Capacity: return $"정원 {value:0}명";
            case HousingEffectType.DissatisfactionRecovery: return $"불만 회복 보너스 {value:0.##}/초";
            case HousingEffectType.FatigueRecovery: return $"집 피로 회복 보너스 {value:0.##}/초";
            case HousingEffectType.HungerRecovery: return $"집 허기 회복 보너스 {value:0.##}/초";
            case HousingEffectType.ThirstRecovery: return $"집 갈증 회복 보너스 {value:0.##}/초";
            default: return $"{effect}: {value:0.##}";
        }
    }

    private string ItemName(int id)
        => _resources.ItemData && _resources.ItemData.TryGetItemInfo(id, out var item) ? item.ItemName : $"Item {id}";

    private void AddRow(string text, bool shortage = false)
    {
        if (_rowIndex == _rows.Count) _rows.Add(Instantiate(_rowPrefab, _rowsRoot));
        HouseInfoRow row = _rows[_rowIndex++];
        row.gameObject.SetActive(true);
        row.Bind(text, shortage);
    }

    private void ResetScroll()
    {
        if (!_scrollRect) return;
        if (_scrollRect.gameObject.activeInHierarchy) Canvas.ForceUpdateCanvases();
        _scrollRect.StopMovement();
        _scrollRect.verticalNormalizedPosition = 1f;
    }
}
