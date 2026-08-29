using UnityEngine;

public class TestFarmProductionWindow : MonoBehaviour
{
    private const float WindowWidth = 280f;
    private const float WindowHeight = 220f;
    private const int ApplyManyCount = 10;

    [SerializeField] private FarmWorkSite _farmWorkSite;
    [SerializeField] private WarehouseInventory _warehouse;
    [SerializeField] private int _observedItemId;

    private Rect _windowRect = new Rect(20f, 300f, WindowWidth, WindowHeight);

    private bool _hasLastResult;
    private bool _lastSuccess;
    private FarmWorkPhase _lastPreviousPhase;
    private FarmWorkPhase _lastCurrentPhase;
    private float _lastPreviousProgress;
    private float _lastCurrentProgress;
    private int _lastWarehouseDelta;

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Farm Production");
    }

    private void DrawWindow(int windowId)
    {
        if (!_farmWorkSite || !_warehouse)
        {
            GUILayout.Label("Missing FarmWorkSite or WarehouseInventory reference.");
            GUI.DragWindow();
            return;
        }

        if (GUILayout.Button("Apply Work Once", GUILayout.Height(32f)))
        {
            ApplyWork();
        }

        if (GUILayout.Button($"Apply Work {ApplyManyCount} Times", GUILayout.Height(32f)))
        {
            for (int i = 0; i < ApplyManyCount; ++i)
                ApplyWork();
        }

        GUILayout.Label($"Phase: {_farmWorkSite.Phase}");
        GUILayout.Label($"Progress: {_farmWorkSite.CurrentProgress:F1} / {_farmWorkSite.MaxProgress:F1}");
        GUILayout.Label($"Normalized: {_farmWorkSite.NormalizedProgress:F2}");
        GUILayout.Label($"Warehouse[{_observedItemId}]: {_warehouse.GetQuantity(_observedItemId)}");

        if (_hasLastResult)
        {
            GUILayout.Label($"Last: success={_lastSuccess} phase={_lastPreviousPhase}->{_lastCurrentPhase} " +
                $"progress={_lastPreviousProgress:F1}->{_lastCurrentProgress:F1} warehouseDelta={_lastWarehouseDelta}");
        }

        GUI.DragWindow();
    }

    // Records state directly before/after the call: the common InteractionResult intentionally
    // carries no farm-specific payload (see PublicMD/Archive/Plans/InteractionProvider_Unification_Plan.md
    // section 8.2), so the previous dedicated result type no longer exists.
    private void ApplyWork()
    {
        FarmWorkPhase previousPhase = _farmWorkSite.Phase;
        float previousProgress = _farmWorkSite.CurrentProgress;
        int previousQuantity = _warehouse.GetQuantity(_observedItemId);

        bool success = _farmWorkSite.TryInteract(new InteractionRequest(ActionType.Farming, strength: 1f), out _);

        _lastSuccess = success;
        _lastPreviousPhase = previousPhase;
        _lastCurrentPhase = _farmWorkSite.Phase;
        _lastPreviousProgress = previousProgress;
        _lastCurrentProgress = _farmWorkSite.CurrentProgress;
        _lastWarehouseDelta = _warehouse.GetQuantity(_observedItemId) - previousQuantity;
        _hasLastResult = true;
    }
}
