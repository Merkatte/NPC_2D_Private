using UnityEngine;

public class TestFarmProductionWindow : MonoBehaviour
{
    private const float WindowWidth = 320f;
    private const float WindowHeight = 320f;
    private const int ApplyManyCount = 10;

    [SerializeField] private FarmWorkSite _farmWorkSite;
    [SerializeField] private WarehouseInventory _warehouse;
    [SerializeField] private CropCatalog _cropCatalog;
    [SerializeField] private ItemDataContext _itemDataContext;

    private Rect _windowRect = new Rect(20f, 300f, WindowWidth, WindowHeight);

    private bool _hasValidatedCatalog;
    private bool _isCatalogValid;
    private string _catalogFailureReason;

    private bool _hasLastResult;
    private bool _lastSuccess;
    private FarmWorkPhase _lastPreviousPhase;
    private FarmWorkPhase _lastCurrentPhase;
    private float _lastPreviousProgress;
    private float _lastCurrentProgress;
    private int _lastWarehouseDelta;

    private bool _hasLastSelection;
    private string _lastSelectionLabel;
    private bool _lastSelectionSuccess;
    private string _lastSelectionFailureReason;

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

        DrawCropSelection();

        GUILayout.Space(6f);

        if (GUILayout.Button("Apply Work Once", GUILayout.Height(32f)))
        {
            ApplyWork();
        }

        if (GUILayout.Button($"Apply Work {ApplyManyCount} Times", GUILayout.Height(32f)))
        {
            for (int i = 0; i < ApplyManyCount; ++i)
                ApplyWork();
        }

        GUILayout.Label($"Crop: {(_farmWorkSite.HasCrop ? _farmWorkSite.CurrentDefinition.DisplayName : "(none)")}");
        GUILayout.Label($"Phase: {_farmWorkSite.Phase}");
        GUILayout.Label($"Progress: {_farmWorkSite.CurrentProgress:F1} / {_farmWorkSite.MaxProgress:F1}");
        GUILayout.Label($"Normalized: {_farmWorkSite.NormalizedProgress:F2}");
        GUILayout.Label($"Can select crop: {_farmWorkSite.CanSelectCrop}");

        DrawWarehouseQuantities();

        if (_hasLastSelection)
        {
            GUILayout.Label(_lastSelectionSuccess
                ? $"Last select: '{_lastSelectionLabel}' OK"
                : $"Last select: '{_lastSelectionLabel}' rejected ({_lastSelectionFailureReason})");
        }

        if (_hasLastResult)
        {
            GUILayout.Label($"Last work: success={_lastSuccess} phase={_lastPreviousPhase}->{_lastCurrentPhase} " +
                $"progress={_lastPreviousProgress:F1}->{_lastCurrentProgress:F1} warehouseDelta={_lastWarehouseDelta}");
        }

        GUI.DragWindow();
    }

    private void DrawCropSelection()
    {
        if (!_cropCatalog || !_itemDataContext)
        {
            GUILayout.Label("Missing CropCatalog or ItemDataContext reference.");
            return;
        }

        EnsureCatalogValidated();

        if (!_isCatalogValid)
        {
            GUILayout.Label($"CropCatalog invalid: {_catalogFailureReason}");
            return;
        }

        var definitions = _cropCatalog.Definitions;
        for (int i = 0; i < definitions.Count; ++i)
        {
            FarmProductionDefinition definition = definitions[i];
            if (!definition)
                continue;

            if (GUILayout.Button($"Select {definition.DisplayName}"))
            {
                SelectCrop(definition);
            }
        }
    }

    // Validated once per window instance: a catalog asset does not change at runtime, so
    // re-running TryValidate every OnGUI call would only repeat the same result every frame.
    private void EnsureCatalogValidated()
    {
        if (_hasValidatedCatalog)
            return;

        _isCatalogValid = _cropCatalog.TryValidate(_itemDataContext, out _catalogFailureReason);
        _hasValidatedCatalog = true;
    }

    private void DrawWarehouseQuantities()
    {
        if (!_cropCatalog)
            return;

        var definitions = _cropCatalog.Definitions;
        for (int i = 0; i < definitions.Count; ++i)
        {
            FarmProductionDefinition definition = definitions[i];
            if (!definition)
                continue;

            GUILayout.Label($"Warehouse[{definition.DisplayName}]: {_warehouse.GetQuantity(definition.OutputItemId)}");
        }
    }

    private void SelectCrop(FarmProductionDefinition definition)
    {
        bool success = _farmWorkSite.TrySelectCrop(definition, out string failureReason);

        _hasLastSelection = true;
        _lastSelectionLabel = definition.DisplayName;
        _lastSelectionSuccess = success;
        _lastSelectionFailureReason = failureReason;
    }

    // Records state directly before/after the call: the common InteractionResult intentionally
    // carries no farm-specific payload (see PublicMD/Archive/Plans/InteractionProvider_Unification_Plan.md
    // section 8.2), so the previous dedicated result type no longer exists.
    private void ApplyWork()
    {
        FarmWorkPhase previousPhase = _farmWorkSite.Phase;
        float previousProgress = _farmWorkSite.CurrentProgress;
        int outputItemId = _farmWorkSite.HasCrop ? _farmWorkSite.CurrentDefinition.OutputItemId : -1;
        int previousQuantity = outputItemId >= 0 ? _warehouse.GetQuantity(outputItemId) : 0;

        bool success = _farmWorkSite.TryInteract(new InteractionRequest(ActionType.Farming, strength: 1f), out _);

        int currentOutputItemId = _farmWorkSite.HasCrop ? _farmWorkSite.CurrentDefinition.OutputItemId : outputItemId;

        _lastSuccess = success;
        _lastPreviousPhase = previousPhase;
        _lastCurrentPhase = _farmWorkSite.Phase;
        _lastPreviousProgress = previousProgress;
        _lastCurrentProgress = _farmWorkSite.CurrentProgress;
        _lastWarehouseDelta = currentOutputItemId >= 0
            ? _warehouse.GetQuantity(currentOutputItemId) - previousQuantity
            : 0;
        _hasLastResult = true;
    }
}
