using UnityEngine;

public class FarmWorkSite : MonoBehaviour, IFarmWorkProvider
{
    [SerializeField] private FarmProductionDefinition _definition;
    [SerializeField] private SeededRandomSource _randomSource;
    [SerializeField] private MonoBehaviour _outputInventorySource;

    private IInventory _outputInventory;
    private bool _isOperational;

    private FarmWorkPhase _phase = FarmWorkPhase.Growing;
    private float _currentProgress;

    public FarmWorkPhase Phase => _phase;
    public float CurrentProgress => _currentProgress;
    public float MaxProgress => _definition ? _definition.MaxProgress : 0f;
    public float NormalizedProgress => MaxProgress <= 0f ? 0f : Mathf.Clamp01(_currentProgress / MaxProgress);

    public bool CanApplyWork => _isOperational;

    private void Awake()
    {
        _outputInventory = _outputInventorySource as IInventory;
        _isOperational = ResolveOperational(out string failureReason);

        if (!_isOperational)
            Debug.LogError($"FarmWorkSite on {name} is not operational: {failureReason}");
    }

    public bool TryApplyWork(float workerEfficiency, out FarmWorkResult result)
    {
        if (workerEfficiency <= 0f || !CanApplyWork)
        {
            result = FarmWorkResult.Failed(_phase, _currentProgress);
            return false;
        }

        return _phase == FarmWorkPhase.Growing
            ? ApplyGrowingWork(workerEfficiency, out result)
            : ApplyHarvestingWork(workerEfficiency, out result);
    }

    private bool ApplyGrowingWork(float workerEfficiency, out FarmWorkResult result)
    {
        FarmWorkPhase previousPhase = _phase;
        float previousProgress = _currentProgress;

        float delta = _definition.GrowthPerWork * workerEfficiency;
        _currentProgress = Mathf.Min(_definition.MaxProgress, _currentProgress + delta);

        if (_currentProgress >= _definition.MaxProgress)
            _phase = FarmWorkPhase.Harvesting;

        result = new FarmWorkResult(true, previousPhase, _phase, previousProgress, _currentProgress, -1, 0);
        return true;
    }

    private bool ApplyHarvestingWork(float workerEfficiency, out FarmWorkResult result)
    {
        FarmWorkPhase previousPhase = _phase;
        float previousProgress = _currentProgress;

        int yield = _randomSource.NextInclusive(_definition.MinimumYield, _definition.MaximumYield);
        bool accepted = _outputInventory.TryAdd(_definition.OutputItemId, yield, out int acceptedQuantity);

        if (!accepted || acceptedQuantity != yield)
        {
            result = FarmWorkResult.Failed(previousPhase, previousProgress);
            return false;
        }

        float delta = _definition.HarvestProgressPerWork * workerEfficiency;
        _currentProgress = Mathf.Max(0f, _currentProgress - delta);

        if (_currentProgress <= 0f)
            _phase = FarmWorkPhase.Growing;

        result = new FarmWorkResult(true, previousPhase, _phase, previousProgress, _currentProgress,
            _definition.OutputItemId, yield);
        return true;
    }

    private bool ResolveOperational(out string failureReason)
    {
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
}
