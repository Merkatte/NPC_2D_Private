using System;
using UnityEngine;
using UnityEngine.Serialization;

public class FarmWorkSite : BaseInteractionProvider, IHoverInfoSource
{
    private const int WorkPositionRandomResolution = 10000;

    [FormerlySerializedAs("_definition")]
    [SerializeField] private FarmProductionDefinition _startingDefinition;
    [SerializeField] private SeededRandomSource _randomSource;
    [SerializeField] private BoxCollider2D _workArea;
    // Position draws stay separate from yield draws so routing cannot change production results.
    [SerializeField] private SeededRandomSource _workPositionRandomSource;
    [SerializeField, Min(1)] private int _workGridColumns = 4;
    [SerializeField, Min(1)] private int _workGridRows = 2;
    [SerializeField, Range(0f, 0.45f)] private float _workPositionJitter = 0.2f;

    private FarmProductionDefinition _currentDefinition;
    private FarmWorkPhase _phase = FarmWorkPhase.Growing;
    private float _currentProgress;

    // Set once a yield has been rolled for the current harvest attempt and cleared only after the
    // carrier accepts it in full. Keeps a rejected/partial deposit from silently reshuffling the
    // deterministic random stream (see Farming/Runtime_and_Transactions.md invariants).
    private int _pendingYield = -1;

    private int[] _workCellOrder;
    private int _nextWorkCellIndex;
    private int _lastWorkCell = -1;
    private bool _hasLoggedWorkPositionFallback;
    private bool _hasLoggedMissingHarvestCargo;

    public FarmProductionDefinition CurrentDefinition => _currentDefinition;
    public bool HasCrop => _currentDefinition;
    public bool CanSelectCrop => !HasCrop || (_phase == FarmWorkPhase.Growing && _currentProgress <= 0f);

    public FarmWorkPhase Phase => _phase;
    public float CurrentProgress => _currentProgress;
    public float MaxProgress => _currentDefinition ? _currentDefinition.MaxProgress : 0f;
    public float NormalizedProgress => MaxProgress <= 0f ? 0f : Mathf.Clamp01(_currentProgress / MaxProgress);

    public UnityEngine.Object Owner => this;
    public HoverType HoverType => HoverType.FarmStatus;

    public event Action StateChanged;

    public bool TryGetHoverInfo(out HoverInfo info)
    {
        if (!_currentDefinition || _currentDefinition.MaxProgress <= 0f)
        {
            info = default;
            return false;
        }

        info = new HoverInfo(
            title: null,
            description: null,
            anchorPosition: transform.position,
            hasProgress: true,
            normalizedProgress: NormalizedProgress);
        return true;
    }

    // Empty farms are a normal, expected state (no crop selected yet), not a wiring failure, so
    // this intentionally does not log. TryInitializeCore only validates fixed scene dependencies.
    public bool TrySelectCrop(FarmProductionDefinition definition, out string failureReason)
    {
        if (!definition)
        {
            failureReason = "definition is null";
            return false;
        }

        if (!definition.IsValid)
        {
            failureReason = $"'{definition.name}' has invalid values";
            return false;
        }

        if (!CanSelectCrop)
        {
            failureReason = "farm is not empty (phase/progress)";
            return false;
        }

        _currentDefinition = definition;
        _phase = FarmWorkPhase.Growing;
        _currentProgress = 0f;
        _pendingYield = -1;

        failureReason = null;
        StateChanged?.Invoke();
        return true;
    }

    protected override bool SupportsCore(ActionType type)
        => type == ActionType.Farming || type == ActionType.Harvest;

    // Farming and Harvest are mutually exclusive by phase, so an ordinary CanInteract lookup
    // through DestinationDB/InteractableManager doubles as "is this farm ready to harvest?" —
    // the selector never needs to know about FarmWorkPhase directly.
    protected override bool CanInteractCore(ActionType type)
    {
        if (!_currentDefinition || !_currentDefinition.IsValid)
            return false;

        return type == ActionType.Harvest
            ? _phase == FarmWorkPhase.Harvesting
            : _phase == FarmWorkPhase.Growing;
    }

    protected override bool TryGetActionPositionCore(ActionType type, Vector3 fallbackPosition, out Vector3 position)
    {
        position = fallbackPosition;

        if (!TryGetWorkAreaValues(out Vector2 localMinimum, out Vector2 cellSize, out int cellCount,
                out string failureReason))
        {
            LogWorkPositionFallbackOnce(failureReason);
            return true;
        }

        EnsureWorkCellOrder(cellCount);

        int cellIndex = _workCellOrder[_nextWorkCellIndex++];
        _lastWorkCell = cellIndex;

        int column = cellIndex % _workGridColumns;
        int row = cellIndex / _workGridColumns;
        float jitterRatio = Mathf.Clamp(_workPositionJitter, 0f, 0.45f);
        float localX = localMinimum.x + (column + 0.5f) * cellSize.x
            + NextSignedUnit() * cellSize.x * jitterRatio;
        float localY = localMinimum.y + (row + 0.5f) * cellSize.y
            + NextSignedUnit() * cellSize.y * jitterRatio;

        position = _workArea.transform.TransformPoint(new Vector3(localX, localY, 0f));
        if (IsFinite(position))
            return true;

        position = fallbackPosition;
        LogWorkPositionFallbackOnce("work-area transform produced a non-finite world position");
        return true;
    }

    protected override bool TryInitializeCore(out string failureReason)
    {
        if (!_randomSource)
        {
            failureReason = "missing SeededRandomSource";
            return false;
        }

        if (_startingDefinition && !TrySelectCrop(_startingDefinition, out string selectFailureReason))
        {
            Debug.LogError($"FarmWorkSite '{name}': starting definition rejected ({selectFailureReason}); starting empty.", this);
        }

        failureReason = null;
        return true;
    }

    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;

        bool succeeded = request.Type == ActionType.Harvest
            ? ApplyHarvestingWork(request.Strength, request.Cargo)
            : ApplyGrowingWork(request.Strength);

        if (succeeded)
            StateChanged?.Invoke();

        return succeeded;
    }

    private void OnValidate()
    {
        _workGridColumns = Mathf.Max(1, _workGridColumns);
        _workGridRows = Mathf.Max(1, _workGridRows);
        _workPositionJitter = Mathf.Clamp(_workPositionJitter, 0f, 0.45f);
    }

    private bool TryGetWorkAreaValues(out Vector2 localMinimum, out Vector2 cellSize, out int cellCount,
        out string failureReason)
    {
        localMinimum = default;
        cellSize = default;
        cellCount = 0;

        if (!_workArea)
        {
            failureReason = "missing work-area BoxCollider2D";
            return false;
        }

        if (!_workPositionRandomSource)
        {
            failureReason = "missing work-position SeededRandomSource";
            return false;
        }

        if (float.IsNaN(_workPositionJitter) || float.IsInfinity(_workPositionJitter))
        {
            failureReason = $"invalid work-position jitter {_workPositionJitter}";
            return false;
        }

        if (_workGridColumns <= 0 || _workGridRows <= 0 || _workGridRows > int.MaxValue / _workGridColumns)
        {
            failureReason = $"invalid work grid {_workGridColumns}x{_workGridRows}";
            return false;
        }

        Vector2 areaSize = _workArea.size;
        Vector2 areaOffset = _workArea.offset;
        if (!IsFinite(areaSize) || !IsFinite(areaOffset) || areaSize.x <= 0f || areaSize.y <= 0f)
        {
            failureReason = $"invalid work-area geometry (offset={areaOffset}, size={areaSize})";
            return false;
        }

        cellCount = _workGridColumns * _workGridRows;
        cellSize = new Vector2(areaSize.x / _workGridColumns, areaSize.y / _workGridRows);
        localMinimum = areaOffset - areaSize * 0.5f;
        failureReason = null;
        return true;
    }

    private void EnsureWorkCellOrder(int cellCount)
    {
        if (_workCellOrder == null || _workCellOrder.Length != cellCount)
        {
            _workCellOrder = new int[cellCount];
            _nextWorkCellIndex = cellCount;
            _lastWorkCell = -1;
        }

        if (_nextWorkCellIndex < cellCount)
            return;

        for (int i = 0; i < cellCount; ++i)
            _workCellOrder[i] = i;

        for (int i = cellCount - 1; i > 0; --i)
        {
            int swapIndex = _workPositionRandomSource.NextInclusive(0, i);
            (_workCellOrder[i], _workCellOrder[swapIndex]) = (_workCellOrder[swapIndex], _workCellOrder[i]);
        }

        if (cellCount > 1 && _workCellOrder[0] == _lastWorkCell)
            (_workCellOrder[0], _workCellOrder[1]) = (_workCellOrder[1], _workCellOrder[0]);

        _nextWorkCellIndex = 0;
    }

    private float NextSignedUnit()
    {
        return _workPositionRandomSource.NextInclusive(-WorkPositionRandomResolution, WorkPositionRandomResolution)
            / (float)WorkPositionRandomResolution;
    }

    private void LogWorkPositionFallbackOnce(string failureReason)
    {
        if (_hasLoggedWorkPositionFallback)
            return;

        Debug.LogWarning(
            $"FarmWorkSite '{name}': cannot provide a distributed Farming position ({failureReason}); " +
            "using the registered farm destination instead.",
            this);
        _hasLoggedWorkPositionFallback = true;
    }

    private static bool IsFinite(Vector2 value)
        => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y);

    private static bool IsFinite(Vector3 value)
        => IsFinite(new Vector2(value.x, value.y))
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

    private bool ApplyGrowingWork(float workerEfficiency)
    {
        float previousProgress = _currentProgress;
        float delta = _currentDefinition.GrowthPerWork * workerEfficiency;
        _currentProgress = Mathf.Min(_currentDefinition.MaxProgress, _currentProgress + delta);

        if (_currentProgress >= _currentDefinition.MaxProgress)
            _phase = FarmWorkPhase.Harvesting;

        Debug.Log(
            $"FarmWorkSite '{name}': growth gauge {previousProgress:F1} -> {_currentProgress:F1} / {MaxProgress:F1} " +
            $"({NormalizedProgress:P0}), phase={_phase}.",
            this);

        return true;
    }

    private bool ApplyHarvestingWork(float workerEfficiency, ICarriedInventory destination)
    {
        if (destination == null)
        {
            LogMissingHarvestCargoOnce();
            return false;
        }

        if (_pendingYield < 0)
        {
            _pendingYield = _randomSource.NextInclusive(_currentDefinition.MinimumYield, _currentDefinition.MaximumYield);
            Debug.Log($"FarmWorkSite '{name}': rolled harvest yield {_pendingYield}.", this);
        }

        FarmProductionDefinition harvestedDefinition = _currentDefinition;
        int outputItemId = harvestedDefinition.OutputItemId;
        float maxProgress = harvestedDefinition.MaxProgress;

        bool accepted = destination.TryAdd(outputItemId, _pendingYield, out int acceptedQuantity);

        // Nothing moved (cargo already full, or holding a different item type) — a pure no-op.
        // No internal state changed; the caller should RequestReplan, not Fail.
        if (!accepted || acceptedQuantity <= 0)
            return false;

        _pendingYield -= acceptedQuantity;

        // Any accepted quantity is a real, successful work outcome. Progress only moves once the
        // full pending yield has left the farm — this preserves the roll-once invariant (no
        // re-roll on a later attempt) while still letting a partial carry count as success.
        if (_pendingYield > 0)
        {
            Debug.Log(
                $"FarmWorkSite '{name}': cargo accepted {acceptedQuantity}, {_pendingYield} left to collect.",
                this);
            return true;
        }

        _pendingYield = -1;

        float previousProgress = _currentProgress;
        float delta = harvestedDefinition.HarvestProgressPerWork * workerEfficiency;
        _currentProgress = Mathf.Max(0f, _currentProgress - delta);
        float normalizedProgress = maxProgress <= 0f ? 0f : Mathf.Clamp01(_currentProgress / maxProgress);

        if (_currentProgress <= 0f)
        {
            _phase = FarmWorkPhase.Growing;
            _currentDefinition = null;
        }

        Debug.Log(
            $"FarmWorkSite '{name}': harvest gauge {previousProgress:F1} -> {_currentProgress:F1} / {maxProgress:F1} " +
            $"({normalizedProgress:P0}), stored item {outputItemId} x{acceptedQuantity}, phase={_phase}, hasCrop={HasCrop}.",
            this);

        return true;
    }

    private void LogMissingHarvestCargoOnce()
    {
        if (_hasLoggedMissingHarvestCargo)
            return;

        Debug.LogError($"FarmWorkSite '{name}': Harvest request arrived without carried cargo.", this);
        _hasLoggedMissingHarvestCargo = true;
    }
}
