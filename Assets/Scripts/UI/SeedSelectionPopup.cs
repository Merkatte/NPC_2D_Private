using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a source farm's available seeds and submits one explicit planting request.
/// </summary>
public sealed class SeedSelectionPopup : PopBase
{
    private const int MinimumVisibleSlots = 20;
    private const int Columns = 5;
    private const float SlotSpacingX = 140f;
    private const float SlotSpacingY = 125f;
    private const float FirstSlotX = 100f;
    private const float FirstSlotY = -65f;

    [Serializable]
    private struct SeedIconEntry
    {
        [SerializeField] private int _itemId;
        [SerializeField] private Sprite _icon;

        public int ItemId => _itemId;
        public Sprite Icon => _icon;
    }

    [SerializeField] private ItemSlotView _slotPrefab;
    [SerializeField] private RectTransform _slotParent;
    [SerializeField] private Text _emptyStateText;
    [SerializeField] private SeedIconEntry[] _seedIcons = Array.Empty<SeedIconEntry>();

    [SerializeField] private GameObject _confirmationPanel;
    [SerializeField] private Text _confirmationText;
    [SerializeField] private Text _selectedSeedText;
    [SerializeField] private Button _plantButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _closeButton;

    private readonly List<ItemInfo> _availableSeeds = new List<ItemInfo>();
    private readonly List<ItemSlotView> _slots = new List<ItemSlotView>();
    private readonly List<Button> _slotButtons = new List<Button>();
    private bool _hasLoggedConfigurationFailure;
    private FarmSeedSource _source;
    private int _selectedSeedId = -1;

    internal override bool TryBindSource(IClickPopupSource source)
    {
        FarmSeedSource farmSource = source as FarmSeedSource;
        if (!farmSource || !farmSource.CanPlant || !HasConfiguration())
            return false;

        _source = farmSource;
        ClearSelection();
        if (IsOpen)
            RefreshSlots();
        return true;
    }

    private void OnEnable()
    {
        if (!HasConfiguration())
            return;
        _plantButton.onClick.AddListener(PlantSelectedSeed);
        _cancelButton.onClick.AddListener(CancelSelection);
        _closeButton.onClick.AddListener(RequestClose);
    }

    private void OnDisable()
    {
        if (_plantButton)
            _plantButton.onClick.RemoveListener(PlantSelectedSeed);
        if (_cancelButton)
            _cancelButton.onClick.RemoveListener(CancelSelection);
        if (_closeButton)
            _closeButton.onClick.RemoveListener(RequestClose);
        ClearSelection();
        _source = null;
    }

    private void Update()
    {
        // Closing animation may still be visible after OnBeforeClose releases the source.
        if (ReferenceEquals(_source, null))
            return;
        if (!_source || !_source.IsAvailable)
        {
            RequestClose();
            return;
        }
        if (!_source.CanPlant && _selectedSeedId >= 0)
        {
            _plantButton.interactable = false;
            _confirmationText.text = "이미 작물이 심어진 밭입니다.";
        }
    }

    protected override void OnOpened()
    {
        ClearSelection();
        RefreshSlots();
    }

    protected override void OnBeforeClose()
    {
        ClearSelection();
        _source = null;
    }

    public void CancelSelection()
    {
        ClearSelection();
    }

    private void RefreshSlots()
    {
        _availableSeeds.Clear();

        if (!_source || !_source.IsAvailable || !HasConfiguration())
        {
            ShowEmptyState("씨앗 재고를 불러올 수 없습니다.");
            HideSlots();
            return;
        }

        _source.GetAvailableSeeds(_availableSeeds);

        int visibleCount = Mathf.Max(MinimumVisibleSlots, _availableSeeds.Count);
        for (int i = 0; i < visibleCount; ++i)
        {
            ItemSlotView slot = RentSlot(i);
            Button button = _slotButtons[i];
            button.onClick.RemoveAllListeners();
            slot.gameObject.SetActive(true);

            if (i < _availableSeeds.Count)
            {
                ItemInfo seed = _availableSeeds[i];
                _source.TryGetSeedInfo(seed.ID, out _, out int quantity);
                slot.Bind(seed.ID, seed.ItemName, quantity, GetIcon(seed.ID));
                int itemId = seed.ID;
                button.onClick.AddListener(() => SelectSeed(itemId));
                button.interactable = _source.CanPlant;
            }
            else
            {
                slot.Clear();
                button.interactable = false;
            }
        }

        for (int i = visibleCount; i < _slots.Count; ++i)
        {
            _slotButtons[i].onClick.RemoveAllListeners();
            _slots[i].Clear();
            _slots[i].gameObject.SetActive(false);
        }

        if (_availableSeeds.Count == 0)
            ShowEmptyState("보유한 씨앗이 없습니다.");
        else
            _emptyStateText.gameObject.SetActive(false);
    }

    private ItemSlotView RentSlot(int index)
    {
        while (_slots.Count <= index)
        {
            int slotIndex = _slots.Count;
            ItemSlotView slot = Instantiate(_slotPrefab, _slotParent);
            if (slot.TryGetComponent(out ItemSlotDragHandle dragHandle))
                dragHandle.enabled = false;

            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot.GetComponent<Image>();
            button.interactable = false;

            RectTransform rect = (RectTransform)slot.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                FirstSlotX + slotIndex % Columns * SlotSpacingX,
                FirstSlotY - slotIndex / Columns * SlotSpacingY);

            _slots.Add(slot);
            _slotButtons.Add(button);
        }

        return _slots[index];
    }

    private void SelectSeed(int itemId)
    {
        if (!_source || !_source.CanPlant
            || !_source.TryGetSeedInfo(itemId, out ItemInfo seed, out int quantity) || quantity <= 0)
        {
            ClearSelection();
            RefreshSlots();
            return;
        }

        _selectedSeedId = itemId;
        _confirmationText.text = "이 씨앗을 심을까요? (씨앗 1개 소비)";
        _selectedSeedText.text = seed.ItemName;
        _confirmationPanel.SetActive(true);
        _plantButton.interactable = true;
    }

    private void PlantSelectedSeed()
    {
        if (_selectedSeedId < 0)
            return;

        int seedItemId = _selectedSeedId;
        _selectedSeedId = -1;
        _plantButton.interactable = false;
        SeedPlantResult result = SeedPlantResult.FarmUnavailable;
        if (_source && _source.TryPlantSeed(seedItemId, out result))
        {
            RequestClose();
            return;
        }

        RefreshSlots();
        _confirmationText.text = GetFailureMessage(result);
        _confirmationPanel.SetActive(true);
    }

    private static string GetFailureMessage(SeedPlantResult result)
    {
        switch (result)
        {
            case SeedPlantResult.NotEnoughSeeds: return "씨앗 재고가 부족합니다. 다시 선택해 주세요.";
            case SeedPlantResult.FarmOccupied: return "이미 작물이 심어진 밭입니다.";
            case SeedPlantResult.FarmUnavailable: return "지금은 이 밭에 심을 수 없습니다.";
            default: return "씨앗 정보를 확인할 수 없습니다.";
        }
    }

    private Sprite GetIcon(int itemId)
    {
        if (_seedIcons == null)
            return null;

        for (int i = 0; i < _seedIcons.Length; ++i)
        {
            if (_seedIcons[i].ItemId == itemId)
                return _seedIcons[i].Icon;
        }

        return null;
    }

    private void HideSlots()
    {
        for (int i = 0; i < _slots.Count; ++i)
        {
            _slotButtons[i].onClick.RemoveAllListeners();
            _slots[i].Clear();
            _slots[i].gameObject.SetActive(false);
        }
    }

    private void ShowEmptyState(string message)
    {
        if (!_emptyStateText)
            return;

        _emptyStateText.text = message;
        _emptyStateText.gameObject.SetActive(true);
    }

    private void ClearSelection()
    {
        _selectedSeedId = -1;
        if (_confirmationPanel)
            _confirmationPanel.SetActive(false);

        if (_plantButton)
            _plantButton.interactable = false;
    }

    private bool HasConfiguration()
    {
        if (_slotPrefab && _slotParent && _emptyStateText && _confirmationPanel && _confirmationText
            && _selectedSeedText && _plantButton && _cancelButton && _closeButton)
            return true;
        if (_hasLoggedConfigurationFailure)
            return false;

        Debug.LogError($"SeedSelectionPopup '{name}': missing slots, confirmation text or plant/cancel/close buttons.", this);
        _hasLoggedConfigurationFailure = true;
        return false;
    }
}
