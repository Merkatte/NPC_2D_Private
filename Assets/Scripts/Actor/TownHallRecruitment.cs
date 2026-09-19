using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TownHallRecruitment : MonoBehaviour
{
    [Serializable]
    private sealed class RecruitmentSetting
    {
        [SerializeField] private NPCType _npcType;
        [SerializeField, Min(1f)] private float _cooldownDuration = 60f;
        [SerializeField, Min(1)] private int _settlementCost = 100;
        public NPCType NpcType => _npcType;
        public float CooldownDuration => Mathf.Max(1f, _cooldownDuration);
        public int SettlementCost => Mathf.Max(1, _settlementCost);
        public void Validate()
        {
            _cooldownDuration = CooldownDuration;
            _settlementCost = SettlementCost;
        }
    }

    private sealed class RecruitmentState
    {
        public RecruitmentSetting Setting;
        public RecruitPhase Phase = RecruitPhase.CandidateReady;
        public float CooldownRemaining;
        public WorkerReservation Reservation;
        public Coroutine DropRoutine;
        public bool IsDispatching;
    }

    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private GoldManager _goldManager;
    [SerializeField] private TownHallVisual _visual;

    [SerializeField] private List<RecruitmentSetting> _recruitments = new List<RecruitmentSetting>();
    [SerializeField] private Vector2 _landingPoint;
    [SerializeField, Min(0.1f)] private float _dropHeight = 6f;
    [SerializeField, Min(0.1f)] private float _dropDuration = 0.7f;
    [SerializeField] private AnimationCurve _dropEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<NPCType, RecruitmentState> _states = new Dictionary<NPCType, RecruitmentState>();
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    private Vector3 LandingPosition => transform.position + new Vector3(_landingPoint.x, _landingPoint.y, 0f);

    private void Awake()
    {
        OnValidate();
        if (!_npcManager || !_goldManager || !_visual || _recruitments == null || _recruitments.Count == 0)
        {
            ReportConfigurationFailure("missing managers, visual or recruitment settings");
            return;
        }
        foreach (RecruitmentSetting setting in _recruitments)
        {
            if (setting == null || !Enum.IsDefined(typeof(NPCType), setting.NpcType) || _states.ContainsKey(setting.NpcType))
            {
                ReportConfigurationFailure("invalid or duplicate recruitment setting");
                _states.Clear();
                return;
            }
            _states.Add(setting.NpcType, new RecruitmentState { Setting = setting });
        }
        _isConfigured = true;
        RefreshWorldIcon();
    }

    public bool TryGetRecruitment(NPCType npcType, out RecruitmentStatus status)
    {
        status = default;
        if (!_isConfigured || !_states.TryGetValue(npcType, out RecruitmentState state))
            return false;
        status = new RecruitmentStatus(npcType, state.Phase, state.CooldownRemaining,
            state.Setting.SettlementCost, state.IsDispatching);
        return true;
    }

    private void Update()
    {
        if (!_isConfigured)
            return;
        foreach (RecruitmentState state in _states.Values)
        {
            if (state.Phase != RecruitPhase.Recruiting || state.IsDispatching)
                continue;
            state.CooldownRemaining = Mathf.Max(0f, state.CooldownRemaining - Time.deltaTime);
            if (state.CooldownRemaining <= 0f)
            {
                state.Phase = RecruitPhase.CandidateReady;
                RefreshWorldIcon();
            }
        }
    }

    public RecruitResult TryDispatchCandidate(NPCType npcType)
    {
        if (!_isConfigured || !isActiveAndEnabled || !_states.TryGetValue(npcType, out RecruitmentState state) ||
            state.Phase != RecruitPhase.CandidateReady || state.IsDispatching)
            return RecruitResult.NotReady;
        Vector3 landingPosition = LandingPosition;
        Vector3 dropStart = landingPosition + Vector3.up * _dropHeight;
        if (!_npcManager || !_goldManager || !_npcManager.TryReserveWorker(npcType, dropStart, out WorkerReservation reservation))
            return RecruitResult.SpawnUnavailable;
        if (!_goldManager.TrySpend(state.Setting.SettlementCost))
        {
            _npcManager.CancelReservation(reservation);
            return RecruitResult.NotEnoughGold;
        }

        // Each role waits for its own landing/get-up before its cooldown can tick.
        state.Reservation = reservation;
        state.IsDispatching = true;
        state.Phase = RecruitPhase.Recruiting;
        state.CooldownRemaining = state.Setting.CooldownDuration;
        RefreshWorldIcon();
        state.DropRoutine = StartCoroutine(DropRoutine(state, dropStart, landingPosition));
        return RecruitResult.Success;
    }

    private IEnumerator DropRoutine(RecruitmentState state, Vector3 from, Vector3 to)
    {
        WorkerNPC worker = state.Reservation.Worker;
        float presentationDuration = worker.PlaySpawnLandingPresentation(_dropDuration, out float visualDropHeight);
        if (presentationDuration > 0f)
        {
            from = to + Vector3.up * Mathf.Max(0f, _dropHeight - visualDropHeight);
            worker.transform.position = from;
        }
        float elapsed = 0f;
        while (elapsed < _dropDuration)
        {
            if (!worker)
            {
                CancelAndRefund(state);
                yield break;
            }
            elapsed += Time.deltaTime;
            float t = _dropEasing.Evaluate(Mathf.Clamp01(elapsed / _dropDuration));
            worker.transform.position = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }
        if (worker)
            worker.transform.position = to;
        float remainingDuration = presentationDuration - _dropDuration;
        if (remainingDuration > 0f)
            yield return new WaitForSeconds(remainingDuration);
        state.DropRoutine = null;
        CommitPendingReservation(state);
    }

    private void OnDisable()
    {
        foreach (RecruitmentState state in _states.Values)
        {
            // Component disable, unlike GameObject disable, does not stop coroutines.
            if (state.DropRoutine != null)
                StopCoroutine(state.DropRoutine);
            state.DropRoutine = null;
            if (!state.IsDispatching || !gameObject.scene.isLoaded)
                continue;
            if (state.Reservation.Worker)
                state.Reservation.Worker.transform.position = LandingPosition;
            CommitPendingReservation(state);
        }
    }

    private void CommitPendingReservation(RecruitmentState state)
    {
        if (!state.IsDispatching)
            return;
        if (_npcManager && _npcManager.CommitReservation(state.Reservation))
        {
            state.Reservation = default;
            state.IsDispatching = false;
            return;
        }
        ReportConfigurationFailure("reserved worker could not be committed; restoring candidate and subsidy");
        CancelAndRefund(state);
    }

    private void CancelAndRefund(RecruitmentState state)
    {
        if (!state.IsDispatching)
            return;
        if (_npcManager)
            _npcManager.CancelReservation(state.Reservation);
        if (_goldManager)
            _goldManager.Add(state.Setting.SettlementCost);
        state.Reservation = default;
        state.IsDispatching = false;
        state.DropRoutine = null;
        state.Phase = RecruitPhase.CandidateReady;
        state.CooldownRemaining = 0f;
        RefreshWorldIcon();
    }

    private void RefreshWorldIcon()
    {
        if (!_visual)
            return;
        foreach (RecruitmentState state in _states.Values)
        {
            if (state.Phase == RecruitPhase.CandidateReady)
            {
                _visual.SetPhase(RecruitPhase.CandidateReady);
                return;
            }
        }
        _visual.SetPhase(RecruitPhase.Recruiting);
    }

    private void OnValidate()
    {
        _dropHeight = Mathf.Max(0.1f, _dropHeight);
        _dropDuration = Mathf.Max(0.1f, _dropDuration);
        if (_dropEasing == null)
            _dropEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        if (_recruitments != null)
            foreach (RecruitmentSetting setting in _recruitments)
                setting?.Validate();
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;
        _hasLoggedConfigurationFailure = true;
        Debug.LogError($"TownHallRecruitment '{name}': {reason}.", this);
    }
}
