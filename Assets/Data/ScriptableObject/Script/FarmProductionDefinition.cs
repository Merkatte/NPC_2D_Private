using UnityEngine;

[CreateAssetMenu(fileName = "FarmProductionDefinition", menuName = "Scriptable Objects/FarmProductionDefinition")]
public class FarmProductionDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private int _cropId = -1;
    [SerializeField] private string _displayName;

    [Header("Gauge")]
    [SerializeField] private float _maxProgress = 100f;
    [SerializeField] private float _growthPerWork = 10f;
    [SerializeField] private float _harvestProgressPerWork = 10f;

    [Header("Output")]
    [SerializeField] private int _outputItemId = -1;
    [SerializeField] private int _minimumYield = 1;
    [SerializeField] private int _maximumYield = 1;

    [Header("Presentation")]
    [SerializeField] private RuntimeAnimatorController _visualController;
    [SerializeField] private CropVisualStage[] _visualStages;

    public int CropId => _cropId;
    public string DisplayName => _displayName;
    public float MaxProgress => _maxProgress;
    public float GrowthPerWork => _growthPerWork;
    public float HarvestProgressPerWork => _harvestProgressPerWork;
    public int OutputItemId => _outputItemId;
    public int MinimumYield => _minimumYield;
    public int MaximumYield => _maximumYield;
    public RuntimeAnimatorController VisualController => _visualController;
    public int VisualStageCount => _visualStages == null ? 0 : _visualStages.Length;

    public bool IsValid => _cropId >= 0 && !string.IsNullOrWhiteSpace(_displayName)
        && _maxProgress > 0f && _growthPerWork > 0f && _harvestProgressPerWork > 0f
        && _outputItemId >= 0 && _minimumYield >= 1 && _maximumYield >= _minimumYield
        && HasValidPresentation();

    public bool TryGetVisualStage(int index, out CropVisualStage stage)
    {
        if (_visualStages == null || index < 0 || index >= _visualStages.Length)
        {
            stage = default;
            return false;
        }

        stage = _visualStages[index];
        return stage.IsValid;
    }

    public int GetVisualStageIndex(float normalizedProgress)
    {
        if (_visualStages == null || _visualStages.Length == 0)
            return -1;

        float progress = Mathf.Clamp01(normalizedProgress);
        int stageIndex = 0;

        for (int i = 1; i < _visualStages.Length; ++i)
        {
            if (progress < _visualStages[i].NormalizedThreshold)
                break;

            stageIndex = i;
        }

        return stageIndex;
    }

    private void OnValidate()
    {
        _maxProgress = Mathf.Max(0.01f, _maxProgress);
        _growthPerWork = Mathf.Max(0.01f, _growthPerWork);
        _harvestProgressPerWork = Mathf.Max(0.01f, _harvestProgressPerWork);

        _minimumYield = Mathf.Max(1, _minimumYield);
        _maximumYield = Mathf.Max(_minimumYield, _maximumYield);
    }

    private bool HasValidPresentation()
    {
        if (!_visualController || _visualStages == null || _visualStages.Length < 2)
            return false;

        const float thresholdTolerance = 0.0001f;

        if (Mathf.Abs(_visualStages[0].NormalizedThreshold) > thresholdTolerance ||
            Mathf.Abs(_visualStages[_visualStages.Length - 1].NormalizedThreshold - 1f) > thresholdTolerance)
        {
            return false;
        }

        float previousThreshold = -1f;
        for (int i = 0; i < _visualStages.Length; ++i)
        {
            CropVisualStage stage = _visualStages[i];
            if (!stage.IsValid || stage.NormalizedThreshold <= previousThreshold)
                return false;

            previousThreshold = stage.NormalizedThreshold;
        }

        return true;
    }
}
