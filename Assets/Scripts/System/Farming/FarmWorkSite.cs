using UnityEngine;

public class FarmWorkSite : BaseInteractionProvider, IHoverInfoSource
{
    [SerializeField] private FarmProductionDefinition _definition;
    [SerializeField] private SeededRandomSource _randomSource;
    [SerializeField] private MonoBehaviour _outputInventorySource;

    private IInventory _outputInventory;

    private FarmWorkPhase _phase = FarmWorkPhase.Growing;
    private float _currentProgress;

    public FarmWorkPhase Phase => _phase;
    public float CurrentProgress => _currentProgress;
    public float MaxProgress => _definition ? _definition.MaxProgress : 0f;
    public float NormalizedProgress => MaxProgress <= 0f ? 0f : Mathf.Clamp01(_currentProgress / MaxProgress);

    protected override bool SupportsCore(ActionType type)
        => type == ActionType.Farming;

    protected override bool TryInitializeCore(out string failureReason)
    {
        _outputInventory = _outputInventorySource as IInventory;

        if (!_definition)
        {
            failureReason = "missing FarmProductionDefinition";
            return false;
        }

        if (!_definition.IsValid)
        {
            failureReason = "FarmProductionDefinition has invalid values";
            return false;
        }

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
        float delta = _definition.GrowthPerWork * workerEfficiency;
        _currentProgress = Mathf.Min(_definition.MaxProgress, _currentProgress + delta);

        if (_currentProgress >= _definition.MaxProgress)
            _phase = FarmWorkPhase.Harvesting;

        Debug.Log(
            $"FarmWorkSite '{name}': growth gauge {previousProgress:F1} -> {_currentProgress:F1} / {MaxProgress:F1} " +
            $"({NormalizedProgress:P0}), phase={_phase}.",
            this);

        return true;
    }

    private bool ApplyHarvestingWork(float workerEfficiency)
    {
        int yield = _randomSource.NextInclusive(_definition.MinimumYield, _definition.MaximumYield);
        bool accepted = _outputInventory.TryAdd(_definition.OutputItemId, yield, out int acceptedQuantity);

        if (!accepted || acceptedQuantity != yield)
            return false;

        float previousProgress = _currentProgress;
        float delta = _definition.HarvestProgressPerWork * workerEfficiency;
        _currentProgress = Mathf.Max(0f, _currentProgress - delta);

        if (_currentProgress <= 0f)
            _phase = FarmWorkPhase.Growing;

        Debug.Log(
            $"FarmWorkSite '{name}': harvest gauge {previousProgress:F1} -> {_currentProgress:F1} / {MaxProgress:F1} " +
            $"({NormalizedProgress:P0}), stored item {_definition.OutputItemId} x{yield}, phase={_phase}.",
            this);

        return true;
    }

    public Object Owner => this;

    public bool TryGetHoverInfo(out HoverInfo info)
    {
        if (!_definition || _definition.MaxProgress <= 0f)
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
}
