using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Deterministic probe for MerchantTradeSite's batch trade transaction and
/// WarehouseInventory.TryRemoveBatch.
///
/// The project has no assembly definitions, so there is no Unity Test Framework assembly to host
/// EditMode tests (see TestDecisionScenarioProbe for the established alternative this follows).
/// Unlike a pure decision probe, every case here actually mutates real WarehouseInventory/
/// GoldManager state, so each case that changes something restores it in a try/finally before the
/// next case runs — the probe is safe to run repeatedly in the same Play session.
/// </summary>
public class TestMerchantBatchTradeProbe : MonoBehaviour
{
    private const float WindowWidth = 300f;
    private const float WindowHeight = 80f;
    private const int UnknownItemId = 987654321;
    private const int NonCatalogItemId = 123456789;
    private const int SuccessCaseQuantity = 3;

    [SerializeField] private WarehouseInventory _warehouse;
    [SerializeField] private MerchantTradeSite _tradeSite;
    [SerializeField] private GoldManager _goldManager;
    [SerializeField] private CropCatalog _cropCatalog;
    [SerializeField] private MonoBehaviour _dataManagerSource;

    private IDataManager _dataManager;
    private Rect _windowRect = new Rect(300f, 170f, WindowWidth, WindowHeight);
    private readonly StringBuilder _report = new StringBuilder();

    private int _passCount;
    private int _failCount;
    private int _skipCount;

    private void Awake()
    {
        _dataManager = _dataManagerSource as IDataManager;
    }

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Merchant Batch Trade Probe");
    }

    private void DrawWindow(int windowId)
    {
        if (GUILayout.Button("Run Batch Trade Scenarios", GUILayout.Height(32f)))
            RunAll();

        GUI.DragWindow();
    }

    private void RunAll()
    {
        if (!_warehouse || !_tradeSite || !_goldManager || !_cropCatalog || _dataManager == null)
        {
            Debug.LogError("TestMerchantBatchTradeProbe is missing one of its serialized references, " +
                "or _dataManagerSource does not implement IDataManager.");
            return;
        }

        _report.Clear();
        _passCount = 0;
        _failCount = 0;
        _skipCount = 0;
        _report.AppendLine("=== Merchant batch trade probe ===");

        if (!TryGetSellableItemId(out int itemId, out int sellPrice))
        {
            Skip("All cases", "No sellable item (a CropCatalog output with a registered positive SellPrice) is available.");
        }
        else
        {
            CaseEmptyBatchRejected();
            CaseUnknownItemRejected();
            CaseInsufficientStockRejected(itemId);
            CasePriceOverflowRejected(itemId, sellPrice);
            CaseGoldCapRejected(itemId);
            CaseSuccessfulTradeAppliesExactly(itemId, sellPrice);
            CaseNonCatalogItemRejected();
        }

        _report.Append("Result: ").Append(_passCount).Append(" passed, ")
            .Append(_failCount).Append(" failed, ")
            .Append(_skipCount).Append(" skipped.");

        if (_failCount > 0)
            Debug.LogError(_report.ToString());
        else
            Debug.Log(_report.ToString());
    }

    // ---- cases ----

    private void CaseEmptyBatchRejected()
    {
        int goldBefore = _goldManager.CurrentGold;

        TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int>());

        Check("Case 1", "an empty batch is rejected and gold is untouched",
            result == TradeResult.InvalidRequest && _goldManager.CurrentGold == goldBefore);
    }

    /// <summary>
    /// Not registered anywhere (not even a valid ItemInfo row), so this exercises the
    /// IsSellableItem check before ItemInfo is ever looked up.
    /// </summary>
    private void CaseUnknownItemRejected()
    {
        int stockBefore = _warehouse.GetQuantity(UnknownItemId);
        int goldBefore = _goldManager.CurrentGold;

        TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int> { [UnknownItemId] = 1 });

        Check("Case 2", "an unknown item id is rejected and nothing changes",
            result == TradeResult.InvalidRequest &&
            _warehouse.GetQuantity(UnknownItemId) == stockBefore &&
            _goldManager.CurrentGold == goldBefore);
    }

    private void CaseInsufficientStockRejected(int itemId)
    {
        int stockBefore = _warehouse.GetQuantity(itemId);
        int goldBefore = _goldManager.CurrentGold;
        int requestedQuantity = stockBefore + 1000;

        TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int> { [itemId] = requestedQuantity });

        Check("Case 3", "requesting more than warehouse stock is rejected and no stock is removed",
            result == TradeResult.OutOfStock &&
            _warehouse.GetQuantity(itemId) == stockBefore &&
            _goldManager.CurrentGold == goldBefore);
    }

    /// <summary>
    /// Skips instead of asserting if sellPrice is so low that no int-representable quantity can
    /// overflow the total — the case is about the overflow guard itself, not about forcing one at
    /// any cost.
    /// </summary>
    private void CasePriceOverflowRejected(int itemId, int sellPrice)
    {
        long overflowQuantityLong = (long)int.MaxValue / sellPrice + 2;
        if (overflowQuantityLong > int.MaxValue)
        {
            Skip("Case 4", $"sell price {sellPrice} is too low to construct an int-quantity overflow case");
            return;
        }

        int stockBefore = _warehouse.GetQuantity(itemId);
        int goldBefore = _goldManager.CurrentGold;
        int overflowQuantity = (int)overflowQuantityLong;

        TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int> { [itemId] = overflowQuantity });

        Check("Case 4", "a price total that would overflow int is rejected before touching stock",
            result == TradeResult.InvalidRequest &&
            _warehouse.GetQuantity(itemId) == stockBefore &&
            _goldManager.CurrentGold == goldBefore);
    }

    private void CaseGoldCapRejected(int itemId)
    {
        int stockBefore = _warehouse.GetQuantity(itemId);
        int goldBefore = _goldManager.CurrentGold;

        _goldManager.Add(int.MaxValue); // Add saturates, so this always lands exactly on int.MaxValue.

        try
        {
            TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int> { [itemId] = 1 });

            Check("Case 5", "a sale that would overflow the gold balance is rejected before touching stock",
                result == TradeResult.InvalidRequest &&
                _warehouse.GetQuantity(itemId) == stockBefore &&
                _goldManager.CurrentGold == int.MaxValue);
        }
        finally
        {
            int overflowAmount = int.MaxValue - goldBefore;
            if (overflowAmount > 0)
                _goldManager.TrySpend(overflowAmount);
        }
    }

    private void CaseSuccessfulTradeAppliesExactly(int itemId, int sellPrice)
    {
        _warehouse.TryAdd(itemId, SuccessCaseQuantity, out _);

        int stockBefore = _warehouse.GetQuantity(itemId);
        int goldBefore = _goldManager.CurrentGold;

        try
        {
            TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int> { [itemId] = SuccessCaseQuantity });

            Check("Case 6", "a valid batch succeeds, removing exactly the requested stock and crediting the exact total",
                result == TradeResult.Success &&
                _warehouse.GetQuantity(itemId) == stockBefore - SuccessCaseQuantity &&
                _goldManager.CurrentGold == goldBefore + sellPrice * SuccessCaseQuantity);
        }
        finally
        {
            int consumed = stockBefore - _warehouse.GetQuantity(itemId);
            if (consumed > 0)
                _warehouse.TryAdd(itemId, consumed, out _);

            int goldGained = _goldManager.CurrentGold - goldBefore;
            if (goldGained > 0)
                _goldManager.TrySpend(goldGained);
        }
    }

    /// <summary>
    /// A well-formed, positive itemId that simply is not one of CropCatalog's outputs. In the
    /// current implementation this is rejected by the same IsSellableItem check as Case 2 (an
    /// unregistered id) — both prove TryTrade enforces its own sellability filter rather than
    /// trusting the caller.
    /// </summary>
    private void CaseNonCatalogItemRejected()
    {
        int goldBefore = _goldManager.CurrentGold;

        TradeResult result = _tradeSite.TryTrade(new Dictionary<int, int> { [NonCatalogItemId] = 1 });

        Check("Case 7", "an id outside CropCatalog.Definitions is rejected even with a well-formed quantity",
            result == TradeResult.InvalidRequest && _goldManager.CurrentGold == goldBefore);
    }

    // ---- helpers ----

    private bool TryGetSellableItemId(out int itemId, out int sellPrice)
    {
        IReadOnlyList<FarmProductionDefinition> definitions = _cropCatalog.Definitions;
        for (int i = 0; i < definitions.Count; ++i)
        {
            FarmProductionDefinition definition = definitions[i];
            if (!definition)
                continue;

            int candidateId = definition.OutputItemId;
            if (_dataManager.TryGetItemInfo(candidateId, out ItemInfo info) && info.SellPrice > 0)
            {
                itemId = candidateId;
                sellPrice = info.SellPrice;
                return true;
            }
        }

        itemId = -1;
        sellPrice = 0;
        return false;
    }

    private void Check(string id, string description, bool passed)
    {
        if (passed)
            _passCount++;
        else
            _failCount++;

        _report.Append(passed ? "  PASS " : "  FAIL ").Append(id).Append(" - ").AppendLine(description);
    }

    private void Skip(string id, string reason)
    {
        _skipCount++;
        _report.Append("  SKIP ").Append(id).Append(" - ").AppendLine(reason);
    }
}
