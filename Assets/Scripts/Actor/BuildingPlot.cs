using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingPlot : BaseInteractionProvider, IClickPopupSource
{
    [SerializeField] private DataManager _dataManager;
    [SerializeField] private ResourceManager _resources;
    [SerializeField] private BuildingFactory _factory;
    [SerializeField] private BuildingPlotRegistry _registry;
    [SerializeField] private Transform _entrance;
    [SerializeField] private Transform[] _workPositions = Array.Empty<Transform>();
    [SerializeField] private BoxCollider2D _workArea;
    [SerializeField] private int[] _allowedBuildingIds = Array.Empty<int>();
    [SerializeField] private GameObject _materialsVisual;
    [SerializeField] private GameObject _emptyVisual;
    [SerializeField] private GameObject _scaffoldingVisual;
    private ConstructionState _construction;
    private long _nextConstructionId;
    private bool _isChanging;
    public BuildingPlotState State { get; private set; }
    public CompletedBuildingFacility CompletedFacility { get; private set; }
    public BuildingDefinition Definition => _construction?.Definition;
    public float Progress => _construction == null ? 0f : Mathf.Clamp01(_construction.Work / _construction.Definition.RequiredWork);
    public bool HasWorkStarted => _construction != null && _construction.HasWorkStarted;
    public bool HasCompletionFailure => !string.IsNullOrEmpty(_construction?.CompletionFailure);
    public string CompletionFailure => _construction?.CompletionFailure;
    public long ApplicationOrder => _construction?.Order ?? long.MaxValue;
    public ResourceManager Resources => _resources;
    public IReadOnlyList<BuildingDefinition> Definitions => _dataManager
        ? _dataManager.BuildingDefinitions : Array.Empty<BuildingDefinition>();
    public int ReservedWorkers
    {
        get
        {
            int count = 0;
            if (_construction != null)
                foreach (var reservation in _construction.Reservations)
                    if (reservation != null && reservation.Status == ConstructionReservationStatus.Active)
                        ++count;
            return count;
        }
    }
    public bool CanReserve => !_isChanging && isActiveAndEnabled && State == BuildingPlotState.UnderConstruction
        && _construction != null && _construction.Work < Definition.RequiredWork && ReservedWorkers < Definition.MaxWorkers;
    public event Action StateChanged;

    private void Awake() { RefreshPresentation(); }
    public bool IsAllowed(int id) => _allowedBuildingIds.Length == 0 || Array.IndexOf(_allowedBuildingIds, id) >= 0;
    public bool TryGetClickPopup(out PopupType type)
    {
        type = isActiveAndEnabled && _dataManager && _resources && State != BuildingPlotState.Completed
            ? PopupType.Construction : PopupType.None;
        return type != PopupType.None;
    }

    public bool TryStartConstruction(int buildingId, out string reason)
    {
        reason = "지금은 건설을 시작할 수 없습니다.";
        if (_isChanging || State != BuildingPlotState.Empty || !isActiveAndEnabled || !TryInitialize(out reason)
            || !IsAllowed(buildingId) || !_dataManager.TryGetBuildingDefinition(buildingId, out var definition)
            || definition.MaxWorkers > _workPositions.Length)
            return false;
        _isChanging = true;
        try
        {
            using (_resources.DeferNotifications())
            {
                if (!_resources.TrySpend(definition.Cost)) { reason = "자원이 부족합니다."; return false; }
                _construction = new ConstructionState(definition, ++_nextConstructionId, _registry.NextApplicationOrder());
                State = BuildingPlotState.UnderConstruction;
                PublishState();
            }
            reason = null;
            return true;
        }
        finally { _isChanging = false; }
    }

    public IReadOnlyDictionary<int, int> GetExpectedRefund()
    {
        var refund = new Dictionary<int, int>();
        if (_construction == null || State != BuildingPlotState.UnderConstruction)
            return refund;
        foreach (var entry in _construction.PaidCost)
        {
            int amount = !_construction.HasWorkStarted ? entry.Value
                : (int)Math.Floor(entry.Value * Math.Max(0d, 1d - (double)_construction.Work / Definition.RequiredWork));
            if (amount > 0)
                refund.Add(entry.Key, amount);
        }
        return refund;
    }

    public bool TryCancelConstruction(out string reason)
    {
        reason = "취소할 공사가 없습니다.";
        if (_isChanging || State != BuildingPlotState.UnderConstruction || !_resources)
            return false;
        _isChanging = true;
        try
        {
            using (_resources.DeferNotifications())
            {
                if (!_resources.TryRefund(GetExpectedRefund())) { reason = "환불을 완료할 수 없습니다. 공사를 유지합니다."; return false; }
                EndReservations(ConstructionReservationStatus.Cancelled);
                _construction = null;
                State = BuildingPlotState.Empty;
                PublishState();
            }
            reason = null;
            return true;
        }
        finally { _isChanging = false; }
    }

    public bool TryReserve(out ConstructionReservation reservation)
    {
        reservation = null;
        if (!CanReserve)
            return false;
        for (int i = 0; i < _construction.Reservations.Length; ++i)
        {
            if (_construction.Reservations[i] != null || !_workPositions[i])
                continue;
            reservation = new ConstructionReservation(this, _construction.Id, i, _workPositions[i].position);
            _construction.Reservations[i] = reservation;
            PublishState();
            if (reservation.IsValid) return true;
            reservation.Dispose();
            reservation = null;
            return false;
        }
        return false;
    }

    internal bool IsReservationValid(ConstructionReservation reservation)
        => reservation != null && reservation.Issuer == this && State == BuildingPlotState.UnderConstruction
            && isActiveAndEnabled && _construction != null && reservation.ConstructionId == _construction.Id
            && reservation.Slot >= 0 && reservation.Slot < _construction.Reservations.Length
            && ReferenceEquals(_construction.Reservations[reservation.Slot], reservation)
            && reservation.Status == ConstructionReservationStatus.Active && _construction.Work < Definition.RequiredWork;

    internal void Release(ConstructionReservation reservation)
    {
        if (_construction != null && reservation.ConstructionId == _construction.Id
            && reservation.Slot < _construction.Reservations.Length
            && ReferenceEquals(_construction.Reservations[reservation.Slot], reservation))
            _construction.Reservations[reservation.Slot] = null;
        reservation.End(ConstructionReservationStatus.Released);
        PublishState();
    }

    protected override bool SupportsCore(ActionType type) => type == ActionType.Build;
    protected override bool CanInteractCore(ActionType type) => isActiveAndEnabled
        && State == BuildingPlotState.UnderConstruction && _construction != null && _construction.Work < Definition.RequiredWork;
    protected override bool TryInitializeCore(out string reason)
    {
        reason = "BuildingPlot requires data, resources, factory, registry, entrance, work area and work positions.";
        if (!_dataManager || !_resources || !_factory || !_registry || !_entrance || !_workArea || _workPositions.Length == 0)
            return false;
        foreach (Transform position in _workPositions)
            if (!position) return false;
        reason = null;
        return true;
    }

    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    {
        result = default;
        if (_isChanging || !(request.Reservation is ConstructionReservation reservation) || !IsReservationValid(reservation))
            return false;
        _isChanging = true;
        try
        {
            _construction.Work = Mathf.Min(Definition.RequiredWork, _construction.Work + request.Strength);
            _construction.HasWorkStarted = true;
            if (_construction.Work >= Definition.RequiredWork)
                CompleteFacility(out _);
            PublishState();
            return true;
        }
        finally { _isChanging = false; }
    }

    public bool TryRetryCompletion(out string reason)
    {
        reason = "No failed completion to retry.";
        if (_isChanging || State != BuildingPlotState.UnderConstruction || !HasCompletionFailure)
            return false;
        _isChanging = true;
        try { bool success = CompleteFacility(out reason); PublishState(); return success; }
        finally { _isChanging = false; }
    }

    private bool CompleteFacility(out string reason)
    {
        using (_resources.DeferNotifications())
        {
            bool success = _factory.TryCreate(Definition, transform, _entrance, _workArea, out var facility, out reason);
            _construction.CompletionFailure = success ? null : reason;
            EndReservations(success ? ConstructionReservationStatus.Completed : ConstructionReservationStatus.CompletionFailed);
            if (success)
            {
                CompletedFacility = facility;
                State = BuildingPlotState.Completed;
            }
            return success;
        }
    }

    private void EndReservations(ConstructionReservationStatus status)
    {
        if (_construction == null) return;
        for (int i = 0; i < _construction.Reservations.Length; ++i)
        {
            _construction.Reservations[i]?.End(status);
            _construction.Reservations[i] = null;
        }
    }
    private void OnDisable() { EndReservations(ConstructionReservationStatus.Released); PublishState(); }
    private void RefreshPresentation()
    {
        if (_emptyVisual) _emptyVisual.SetActive(State == BuildingPlotState.Empty);
        if (_materialsVisual) _materialsVisual.SetActive(State == BuildingPlotState.UnderConstruction && !HasWorkStarted);
        if (_scaffoldingVisual) _scaffoldingVisual.SetActive(State == BuildingPlotState.UnderConstruction && HasWorkStarted);
    }
    private void PublishState()
    {
        RefreshPresentation();
        if (StateChanged == null) return;
        foreach (Action handler in StateChanged.GetInvocationList())
            try { handler(); } catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
