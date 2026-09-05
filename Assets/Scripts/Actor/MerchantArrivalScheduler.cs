using UnityEngine;

/// <summary>
/// Owns the gap between merchant visits. The interval measures the time *between* visits — it
/// does not tick while the caravan is on screen — so a visit can never overlap the next one
/// regardless of how long flight + dwell actually take. Lives on its own always-active
/// GameObject: it must keep ticking while the caravan itself is disabled.
/// </summary>
public sealed class MerchantArrivalScheduler : MonoBehaviour
{
    private const float MinimumVisitInterval = 1f;

    [SerializeField] private MerchantCaravan _caravan;
    [SerializeField, Min(MinimumVisitInterval)] private float _visitInterval = 120f;
    [SerializeField] private bool _startsVisitImmediately;

    private float _intervalRemaining;
    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        if (!_caravan)
        {
            ReportConfigurationFailure("missing _caravan reference");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _intervalRemaining = _startsVisitImmediately ? 0f : _visitInterval;
    }

    private void Update()
    {
        if (_caravan.IsVisiting)
            return;

        _intervalRemaining -= Time.deltaTime;
        if (_intervalRemaining > 0f)
            return;

        _intervalRemaining = _visitInterval;
        _caravan.BeginVisit();
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantArrivalScheduler '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }

    private void OnValidate()
    {
        _visitInterval = Mathf.Max(MinimumVisitInterval, _visitInterval);
    }
}
