using UnityEngine;
using UnityEngine.Serialization;

public class FarmWorkSite : BaseInteractionProvider, IHoverInfoSource
{
    [FormerlySerializedAs("_definition")]
    [SerializeField] private FarmProductionDefinition _startingDefinition;
    [SerializeField] private SeededRandomSource _randomSource;
    [SerializeField] private MonoBehaviour _outputInventorySource;

    private IInventory _outputInventory;

    private FarmProductionDefinition _currentDefinition;
    private FarmWorkPhase _phase = FarmWorkPhase.Growing;
    private float _currentProgress;

    // Set once a yield has been rolled for the current harvest attempt and cleared only after the
    // inventory accepts it in full. Keeps a rejected deposit from silently reshuffling the
    // deterministic random stream (see Farming.md invariants).
    private int _pendingYield = -1;

    public FarmProductionDefinition CurrentDefinition => _currentDefinition;
    public bool HasCrop => _currentDefinition;
    public bool CanSelectCrop => !HasCrop || (_phase == FarmWorkPhase.Growing && _currentProgress <= 0f);

    public FarmWorkPhase Phase => _phase;
    public float CurrentProgress => _currentProgress;
    public float MaxProgress => _currentDefinition ? _currentDefinition.MaxProgress : 0f;
    public float NormalizedProgress => MaxProgress <= 0f ? 0f : Mathf.Clamp01(_currentProgress / MaxProgress);

    public Object Owner => this;
    public HoverType HoverType => HoverType.FarmStatus;

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
        return true;
    }

    protected override bool SupportsCore(ActionType type)
        => type == ActionType.Farming;

    protected override bool CanInteractCore(ActionType type)
        => _currentDefinition && _currentDefinition.IsValid;

    protected override bool TryInitializeCore(out string failureReason)
    {
        _outputInventory = _outputInventorySource as IInventory;

        if (!_randomSource)
        {
            failureReason = "missing SeededRandomSource";
            return false;
        }

        if (_outputInventory == null)
        {
            failureReason = "_outputInventorySource does not implement IInventory";
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
        float workerEfficiency = request.Strength;

        return _phase == FarmWorkPhase.Growing
            ? ApplyGrowingWork(workerEfficiency)
            : ApplyHarvestingWork(workerEfficiency);
    }

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

    private bool ApplyHarvestingWork(float workerEfficiency)
    {
        if (_pendingYield < 0)
            _pendingYield = _randomSource.NextInclusive(_currentDefinition.MinimumYield, _currentDefinition.MaximumYield);

        int yield = _pendingYield;
        bool accepted = _outputInventory.TryAdd(_currentDefinition.OutputItemId, yield, out int acceptedQuantity);

        // A partial accept already banked real quantity in the inventory. Shrink the pending yield
        // by what landed so a retry only asks for the remainder instead of re-requesting the full
        // amount (WarehouseInventory is currently all-or-nothing, but the contract allows partial).
        if (acceptedQuantity > 0 && acceptedQuantity < yield)
        {
            _pendingYield -= acceptedQuantity;
            Debug.LogWarning(
                $"FarmWorkSite '{name}': inventory partially accepted harvest ({acceptedQuantity}/{yield}); " +
                $"retrying remaining {_pendingYield} next attempt.",
                this);
        }

        if (!accepted || acceptedQuantity != yield)
            return false;

        _pendingYield = -1;

        float previousProgress = _currentProgress;
        float delta = _currentDefinition.HarvestProgressPerWork * workerEfficiency;
        _currentProgress = Mathf.Max(0f, _currentProgress - delta);

        if (_currentProgress <= 0f)
            _phase = FarmWorkPhase.Growing;

        Debug.Log(
            $"FarmWorkSite '{name}': harvest gauge {previousProgress:F1} -> {_currentProgress:F1} / {MaxProgress:F1} " +
            $"({NormalizedProgress:P0}), stored item {_currentDefinition.OutputItemId} x{yield}, phase={_phase}.",
            this);

        return true;
    }
}
