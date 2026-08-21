using UnityEngine;

[CreateAssetMenu(fileName = "FarmProductionDefinition", menuName = "Scriptable Objects/FarmProductionDefinition")]
public class FarmProductionDefinition : ScriptableObject
{
    [Header("Gauge")]
    [SerializeField] private float _maxProgress = 100f;
    [SerializeField] private float _growthPerWork = 10f;
    [SerializeField] private float _harvestProgressPerWork = 10f;

    [Header("Output")]
    [SerializeField] private int _outputItemId = -1;
    [SerializeField] private int _minimumYield = 1;
    [SerializeField] private int _maximumYield = 1;

    public float MaxProgress => _maxProgress;
    public float GrowthPerWork => _growthPerWork;
    public float HarvestProgressPerWork => _harvestProgressPerWork;
    public int OutputItemId => _outputItemId;
    public int MinimumYield => _minimumYield;
    public int MaximumYield => _maximumYield;

    public bool IsValid => _maxProgress > 0f && _growthPerWork > 0f && _harvestProgressPerWork > 0f
        && _outputItemId >= 0 && _minimumYield >= 1 && _maximumYield >= _minimumYield;

    private void OnValidate()
    {
        _maxProgress = Mathf.Max(0.01f, _maxProgress);
        _growthPerWork = Mathf.Max(0.01f, _growthPerWork);
        _harvestProgressPerWork = Mathf.Max(0.01f, _harvestProgressPerWork);

        _minimumYield = Mathf.Max(1, _minimumYield);
        _maximumYield = Mathf.Max(_minimumYield, _maximumYield);
    }
}
