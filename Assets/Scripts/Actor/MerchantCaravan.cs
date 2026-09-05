using UnityEngine;

/// <summary>
/// Owns *when* the merchant caravan's visit phase changes — arrival, dwell, departure — while
/// MerchantVisual owns *what it looks like* at any given moment. Not a Worker/NPCType role: no
/// stat, no selector, no action queue. Not pooled: there is only ever one caravan, so a single
/// persistent scene GameObject enabled/disabled by MerchantArrivalScheduler is enough.
/// </summary>
public sealed class MerchantCaravan : MonoBehaviour
{
    private const float MinimumFlightSpeed = 0.1f;
    private const float MinimumDwellDuration = 1f;
    private const float ArrivalDistanceSquared = 0.0001f; // 0.01 world units

    [SerializeField] private Transform _moveRoot;
    [SerializeField] private MerchantVisual _visual;
    [SerializeField] private Transform _arrivalPoint;
    [SerializeField] private Transform _dockPoint;
    [SerializeField] private Transform _departurePoint;
    [SerializeField, Min(MinimumFlightSpeed)] private float _flightSpeed = 3f;
    [SerializeField, Min(MinimumDwellDuration)] private float _dwellDuration = 30f;

    private MerchantVisitPhase _phase = MerchantVisitPhase.Away;
    private float _dwellRemaining;
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    public bool IsVisiting => _phase != MerchantVisitPhase.Away;

    private Transform DeparturePoint => _departurePoint ? _departurePoint : _arrivalPoint;

    private void Awake()
    {
        if (!_moveRoot || !_visual || !_arrivalPoint || !_dockPoint)
        {
            ReportConfigurationFailure("missing a required reference (_moveRoot/_visual/_arrivalPoint/_dockPoint)");
            return;
        }

        _isConfigured = true;
    }

    public void BeginVisit()
    {
        if (!_isConfigured || IsVisiting)
            return;

        _moveRoot.position = _arrivalPoint.position;
        _phase = MerchantVisitPhase.Approaching;

        // SetActive must precede the visual reset: Animator.Play/Update(0f) is a no-op on a
        // disabled Animator.
        gameObject.SetActive(true);
        _visual.ResetToFlying();
    }

    private void Update()
    {
        switch (_phase)
        {
            case MerchantVisitPhase.Approaching:
                if (!TryMoveToward(_dockPoint.position))
                    return;

                // Fixed dwell, counted from arrival. No interaction path reaches this timer.
                _dwellRemaining = _dwellDuration;
                _phase = MerchantVisitPhase.Landed;
                _visual.SetLanded(true);
                return;

            case MerchantVisitPhase.Landed:
                _dwellRemaining -= Time.deltaTime;
                if (_dwellRemaining > 0f)
                    return;

                _phase = MerchantVisitPhase.Departing;
                _visual.SetLanded(false);
                return;

            case MerchantVisitPhase.Departing:
                if (!TryMoveToward(DeparturePoint.position))
                    return;

                _phase = MerchantVisitPhase.Away; // before SetActive(false) so IsVisiting reads correctly
                gameObject.SetActive(false);
                return;

            default:
                return;
        }
    }

    private bool TryMoveToward(Vector3 target)
    {
        Vector3 next = Vector3.MoveTowards(_moveRoot.position, target, _flightSpeed * Time.deltaTime);
        _moveRoot.position = next;
        return (next - target).sqrMagnitude <= ArrivalDistanceSquared;
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantCaravan '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }

    private void OnValidate()
    {
        _flightSpeed = Mathf.Max(MinimumFlightSpeed, _flightSpeed);
        _dwellDuration = Mathf.Max(MinimumDwellDuration, _dwellDuration);
    }
}
