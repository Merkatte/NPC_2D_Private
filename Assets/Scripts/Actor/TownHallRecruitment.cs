using System.Collections;
using UnityEngine;

/// <summary>
/// Recruitment cooldown, candidate state, settlement-subsidy transaction, and the settler's drop
/// presentation, all in one component — the town hall is always active (unlike the merchant
/// caravan) so the timer needs no separate always-on GameObject, and splitting the drop into its
/// own presenter would just hand the reservation ticket across a class boundary for no benefit
/// while commit/cancel and the OnDisable abort path need to live where the coroutine lives.
///
/// Only two visible phases exist (Recruiting/CandidateReady) — there is no third "dispatching"
/// phase. Whether a settler is currently falling is represented entirely by
/// _pendingReservation.IsValid, orthogonal to _phase.
/// </summary>
public sealed class TownHallRecruitment : MonoBehaviour
{
    private const float MinimumCooldownDuration = 1f;
    private const int MinimumSettlementCost = 1;
    private const float MinimumDropHeight = 0.1f;
    private const float MinimumDropDuration = 0.1f;

    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private GoldManager _goldManager;
    [SerializeField] private TownHallVisual _visual;

    [SerializeField] private Vector2 _landingPoint;
    [SerializeField, Min(MinimumCooldownDuration)] private float _cooldownDuration = 60f;
    [SerializeField, Min(MinimumSettlementCost)] private int _settlementCost = 100;
    [SerializeField, Min(MinimumDropHeight)] private float _dropHeight = 6f;
    [SerializeField, Min(MinimumDropDuration)] private float _dropDuration = 0.7f;
    [SerializeField] private AnimationCurve _dropEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Fixed for this version — no candidate roster or IRandomSource-based draw. SelectRecruitType
    // is the single point to replace when that comes back; see PublicMD/Systems/Town_Hall.md TBD.
    [SerializeField] private NPCType _recruitNpcType = NPCType.Farmer;

    private RecruitPhase _phase;
    private float _cooldownRemaining;
    private WorkerReservation _pendingReservation;
    private Coroutine _dropRoutine;
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    public RecruitPhase Phase => _phase;
    public float CooldownRemaining => _cooldownRemaining;
    public NPCType CandidateNpcType => _recruitNpcType;
    public int SettlementCost => _settlementCost;

    private Vector3 LandingPosition => transform.position + new Vector3(_landingPoint.x, _landingPoint.y, 0f);

    private void Awake()
    {
        ClampTuningValues();

        if (!_npcManager || !_goldManager || !_visual)
        {
            ReportConfigurationFailure("missing a required reference (_npcManager/_goldManager/_visual)");
            return;
        }

        _isConfigured = true;
        StartInitialCooldown();
    }

    private void Update()
    {
        if (!_isConfigured || _phase != RecruitPhase.Recruiting || _pendingReservation.IsValid)
        {
            return;
        }

        TickCooldown();
    }

    private void OnDisable()
    {
        // Unity has already stopped the Coroutine once this GameObject loses activation (same
        // reasoning as MerchantCaravan.OnDisable) — only the handle needs clearing.
        _dropRoutine = null;

        if (!_pendingReservation.IsValid)
        {
            return;
        }

        // Scene teardown/application quit is destroying this worker too; touching Init on a
        // component mid-destruction risks touching already-destroyed dependencies.
        if (!gameObject.scene.isLoaded)
        {
            return;
        }

        // Gold is already spent — teleporting to the landing point and committing immediately is
        // the only option that doesn't take the player's gold for nothing. The visual pop-in is an
        // acceptable trade-off; the domain stays correct either way.
        _pendingReservation.Worker.transform.position = LandingPosition;
        CommitPendingReservation();
    }

    /// <summary>
    /// Spends the settlement subsidy and starts the settler's drop. Reservation happens before the
    /// (irreversible) gold spend specifically so a reservation failure never touches gold, and a
    /// gold failure can still fully undo the reservation via CancelReservation.
    /// </summary>
    public RecruitResult TryDispatchCandidate()
    {
        if (!_isConfigured || _phase != RecruitPhase.CandidateReady || _pendingReservation.IsValid)
        {
            return RecruitResult.NotReady;
        }

        Vector3 landingPosition = LandingPosition;
        Vector3 dropStart = landingPosition + Vector3.up * _dropHeight;

        if (!_npcManager.TryReserveWorker(SelectRecruitType(), dropStart, out WorkerReservation reservation))
        {
            return RecruitResult.SpawnUnavailable;
        }

        if (!_goldManager.TrySpend(_settlementCost))
        {
            _npcManager.CancelReservation(reservation);
            return RecruitResult.NotEnoughGold;
        }

        _pendingReservation = reservation;
        _phase = RecruitPhase.Recruiting;
        // Filled in immediately so a popup reopened mid-drop shows the full remaining time rather
        // than 00:00 — Update() above refuses to tick it down while a reservation is pending, so
        // the actual countdown only starts once CommitPendingReservation clears it on landing.
        _cooldownRemaining = _cooldownDuration;
        _visual.SetPhase(_phase);

        _dropRoutine = StartCoroutine(DropRoutine(dropStart, landingPosition));
        return RecruitResult.Success;
    }

    // Single extension point for reintroducing a candidate roster + IRandomSource draw later —
    // deliberately no abstraction or candidate data structure beyond this method today.
    private NPCType SelectRecruitType()
    {
        return _recruitNpcType;
    }

    private IEnumerator DropRoutine(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < _dropDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / _dropDuration);
            float t = _dropEasing.Evaluate(normalizedTime);
            _pendingReservation.Worker.transform.position = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        _pendingReservation.Worker.transform.position = to;
        _dropRoutine = null;
        CommitPendingReservation();
    }

    /// <summary>
    /// Clears _pendingReservation only once NPCManager confirms the commit — copying the ticket to
    /// a local first, but not discarding the field until success, so an unexpected commit failure
    /// never loses track of both the gold already spent and the reservation itself. The invariant
    /// this relies on: on any normal path, CommitReservation does not fail (the reservation was
    /// validated at TryReserveWorker time and nothing else releases it in between).
    /// </summary>
    private bool CommitPendingReservation()
    {
        if (!_pendingReservation.IsValid)
        {
            return false;
        }

        WorkerReservation reservation = _pendingReservation;
        if (!_npcManager.CommitReservation(reservation))
        {
            ReportConfigurationFailure("reserved worker could not be committed");
            return false;
        }

        _pendingReservation = default;
        return true;
    }

    private void StartInitialCooldown()
    {
        _phase = RecruitPhase.Recruiting;
        _cooldownRemaining = _cooldownDuration;
        _visual.SetPhase(_phase);
    }

    private void TickCooldown()
    {
        _cooldownRemaining -= Time.deltaTime;
        if (_cooldownRemaining > 0f)
        {
            return;
        }

        _cooldownRemaining = 0f;
        _phase = RecruitPhase.CandidateReady;
        _visual.SetPhase(_phase);
    }

    private void ClampTuningValues()
    {
        _cooldownDuration = Mathf.Max(MinimumCooldownDuration, _cooldownDuration);
        _settlementCost = Mathf.Max(MinimumSettlementCost, _settlementCost);
        _dropHeight = Mathf.Max(MinimumDropHeight, _dropHeight);
        _dropDuration = Mathf.Max(MinimumDropDuration, _dropDuration);
    }

    private void OnValidate()
    {
        ClampTuningValues();
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
        {
            return;
        }

        Debug.LogError($"TownHallRecruitment '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
