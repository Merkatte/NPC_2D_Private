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
    private FarmWorkResult _lastResult;
    private bool _hasLastResult;

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
            GUILayout.Label($"Last: success={_lastResult.Success} phase={_lastResult.PreviousPhase}->{_lastResult.CurrentPhase} " +
                $"progress={_lastResult.PreviousProgress:F1}->{_lastResult.CurrentProgress:F1} " +
                $"produced={_lastResult.ProducedItemId}x{_lastResult.ProducedQuantity}");
        }

        GUI.DragWindow();
    }

    private void ApplyWork()
    {
        _farmWorkSite.TryApplyWork(1f, out _lastResult);
        _hasLastResult = true;
    }
}
