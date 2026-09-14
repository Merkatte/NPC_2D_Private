using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows seed items currently held by the warehouse in inventory slots. Planting is not connected.
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

    [SerializeField] private WarehouseInventory _warehouse;
    [SerializeField] private ItemDataContext _itemDataContext;
    [SerializeField] private ItemSlotView _slotPrefab;
    [SerializeField] private RectTransform _slotParent;
    [SerializeField] private Text _emptyStateText;
    [SerializeField] private SeedIconEntry[] _seedIcons = Array.Empty<SeedIconEntry>();

    [SerializeField] private GameObject _confirmationPanel;
    [SerializeField] private Text _confirmationText;
    [SerializeField] private Text _selectedSeedText;
    [SerializeField] private Button _plantButton;

    private readonly List<ItemInfo> _availableSeeds = new List<ItemInfo>();
    private readonly List<ItemSlotView> _slots = new List<ItemSlotView>();
    private readonly List<Button> _slotButtons = new List<Button>();
    private bool _hasLoggedConfigurationFailure;

    protected override void OnOpened()
    {
        ClearSelection();
        RefreshSlots();
    }

    protected override void OnBeforeClose()
    {
        ClearSelection();
    }

    public void CancelSelection()
    {
        ClearSelection();
    }

    private void RefreshSlots()
    {
        _availableSeeds.Clear();

        if (!_warehouse || !_itemDataContext || !_slotPrefab || !_slotParent || !_emptyStateText)
        {
            ReportConfigurationFailure();
            ShowEmptyState("씨앗 재고를 불러올 수 없습니다.");
            HideSlots();
            return;
        }

        Dictionary<ItemCategory, List<ItemInfo>> itemsByCategory = _itemDataContext.ItemInfos();
        if (itemsByCategory.TryGetValue(ItemCategory.Seed, out List<ItemInfo> seedItems))
        {
            for (int i = 0; i < seedItems.Count; ++i)
            {
                ItemInfo seed = seedItems[i];
                if (_warehouse.GetQuantity(seed.ID) > 0)
                    _availableSeeds.Add(seed);
            }
        }

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
                slot.Bind(seed.ID, seed.ItemName, _warehouse.GetQuantity(seed.ID), GetIcon(seed.ID));
                int itemId = seed.ID;
                button.onClick.AddListener(() => SelectSeed(itemId));
                button.interactable = true;
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
        if (!_warehouse || !_itemDataContext || !_confirmationPanel ||
            !_confirmationText || !_selectedSeedText || _warehouse.GetQuantity(itemId) <= 0 ||
            !_itemDataContext.TryGetItemInfo(itemId, out ItemInfo seed) ||
            seed.Category != ItemCategory.Seed)
        {
            RefreshSlots();
            return;
        }

        _confirmationText.text = "이 씨앗을 심을까요?";
        _selectedSeedText.text = seed.ItemName;
        _confirmationPanel.SetActive(true);
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
        if (_confirmationPanel)
            _confirmationPanel.SetActive(false);

        if (_plantButton)
            _plantButton.interactable = false;
    }

    private void ReportConfigurationFailure()
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"SeedSelectionPopup '{name}': missing warehouse, item data, slot prefab, slot parent or empty-state text.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
