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
    [SerializeField] private CompletedBuildingFacility _initialFacility;
    private void Start()
    {
        if (_initialFacility && State == BuildingPlotState.Empty)
        {
            CompletedFacility = _initialFacility;
            State = BuildingPlotState.Completed;
            CompletedFacility.BindWorkshopPlot(this);
            PublishLifecycle();
        }
    }
    public void OnFacilityDestroyed(CompletedBuildingFacility facility)
    {
        if (!facility || (CompletedFacility && CompletedFacility != facility)) return;
        EndReservations(ConstructionReservationStatus.Cancelled);
        _construction = null;
        CompletedFacility = facility;
        State = BuildingPlotState.Rubble;
        PublishLifecycle();
    }
    public bool ClearDestroyedFacility(CompletedBuildingFacility facility)
    {
        if (State != BuildingPlotState.Rubble || CompletedFacility != facility) return false;
        bool clearedSingleton = HasSingleton(BuildingType.MilitaryTraining) || HasSingleton(BuildingType.ConstructionWorkshop);
        CompletedFacility = null; _initialFacility = null; _construction = null;
        State = BuildingPlotState.Empty;
        PublishLifecycle();
        if (clearedSingleton && _registry) _registry.NotifyConstructionAvailabilityChanged();
        return true;
    }
    private ConstructionState _construction;
    private long _nextConstructionId;
    private bool _isChanging;
    private BuildingType _startingType;
    internal bool HasSingleton(BuildingType type)
    {
        if (!IsSingletonType(type)) return false;
        if (_startingType == type || Definition?.BuildingType == type) return true;
        CompletedBuildingFacility facility = CompletedFacility ? CompletedFacility : _initialFacility;
        return facility && (type == BuildingType.MilitaryTraining ? facility.IsMilitaryTraining : facility.IsConstructionWorkshop);
    }

    private static bool IsSingletonType(BuildingType type)
        => type == BuildingType.MilitaryTraining || type == BuildingType.ConstructionWorkshop;

    internal bool IsOperationalWorkshop => isActiveAndEnabled && State == BuildingPlotState.Completed
        && CompletedFacility && CompletedFacility.IsConstructionWorkshop && CompletedFacility.isActiveAndEnabled;

    internal void NotifyWorkshopActivityChanged(CompletedBuildingFacility facility)
    {
        if (facility == CompletedFacility && _registry) _registry.NotifyPlotLifecycleChanged(this);
    }
    public BuildingPlotState State { get; private set; }
    public CompletedBuildingFacility CompletedFacility { get; private set; }
    public bool IsUpgrade => _construction != null && _construction.IsUpgrade;
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

    internal BuildingPlotRegistry Registry => _registry;

    internal bool IsAvailableInVillage(BuildingDefinition definition, out string reason)
    {
        reason = null;
        if (!IsSingletonType(definition.BuildingType)) return true;
        if (_registry) return _registry.CanStartSingleton(this, definition.BuildingType, out reason);
        reason = "마을 건설 목록을 확인할 수 없습니다.";
        return false;
    }

    public bool IsAllowed(int id) => (_allowedBuildingIds.Length == 0 || Array.IndexOf(_allowedBuildingIds, id) >= 0)
        && _dataManager && _dataManager.TryGetBuildingDefinition(id, out var definition)
        && _factory && _factory.CanStartNewConstruction(definition);

    public bool TryGetNextUpgrade(out BuildingDefinition definition)
    {
        definition = null;
        return CompletedFacility && _factory && _factory.TryGetNextUpgrade(CompletedFacility, out definition);
    }

    public bool TryStartUpgrade(out string reason)
    {
        reason = "지금은 업그레이드를 시작할 수 없습니다.";
        if (Time.timeScale <= 0f) return false;
        if (_isChanging || State != BuildingPlotState.Completed || !isActiveAndEnabled)
            return false;
        if (!TryInitialize(out reason)) return false;
        if (!TryGetNextUpgrade(out var definition) || definition.MaxWorkers > _workPositions.Length)
        {
            reason = "다음 단계 정의나 작업 자리를 확인할 수 없습니다.";
            return false;
        }
        _isChanging = true;
        try
        {
            using (_resources.DeferNotifications())
            {
                if (!_resources.TrySpend(definition.Cost)) { reason = "자원이 부족합니다."; return false; }
                _construction = new ConstructionState(definition, ++_nextConstructionId,
                    _registry.NextApplicationOrder(), true);
                State = BuildingPlotState.UnderConstruction;
                PublishLifecycle();
            }
            reason = null;
            return true;
        }
        finally { _isChanging = false; }
    }
    public bool TryGetClickPopup(out PopupType type)
    {
        type = isActiveAndEnabled && _dataManager && _resources && State != BuildingPlotState.Completed && State != BuildingPlotState.Rubble && !IsUpgrade
            ? PopupType.Construction : PopupType.None;
        return type != PopupType.None;
    }

    public bool TryStartConstruction(int buildingId, out string reason)
    {
        reason = "지금은 건설을 시작할 수 없습니다.";
        if (Time.timeScale <= 0f) return false;
        if (_isChanging || State != BuildingPlotState.Empty || !isActiveAndEnabled || !TryInitialize(out reason))
            return false;
        reason = "이 부지에서 선택한 시설을 건설할 수 없습니다.";
        if (!IsAllowed(buildingId) || !_dataManager.TryGetBuildingDefinition(buildingId, out var definition)
            || definition.MaxWorkers > _workPositions.Length)
            return false;
        if (!IsAvailableInVillage(definition, out reason)) return false;
        _isChanging = true;
        // Other plots in the same village must see the claim before payment or notifications.
        _startingType = definition.BuildingType;
        try
        {
            using (_resources.DeferNotifications())
            {
                if (!_resources.TrySpend(definition.Cost)) { reason = "자원이 부족합니다."; return false; }
                _construction = new ConstructionState(definition, ++_nextConstructionId, _registry.NextApplicationOrder());
                State = BuildingPlotState.UnderConstruction;
                PublishLifecycle();
                if (IsSingletonType(_startingType)) _registry.NotifyConstructionAvailabilityChanged();
            }
            reason = null;
            return true;
        }
        finally
        {
            _startingType = BuildingType.None;
            _isChanging = false;
        }
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
        if (Time.timeScale <= 0f) return false;
        if (_isChanging || State != BuildingPlotState.UnderConstruction || !_resources)
            return false;
        _isChanging = true;
        try
        {
            using (_resources.DeferNotifications())
            {
                if (!_resources.TryRefund(GetExpectedRefund())) { reason = "환불을 완료할 수 없습니다. 공사를 유지합니다."; return false; }
                EndReservations(ConstructionReservationStatus.Cancelled);
                bool wasUpgrade = IsUpgrade;
                bool cancelledSingleton = IsSingletonType(Definition.BuildingType);
                _construction = null;
                State = wasUpgrade ? BuildingPlotState.Completed : BuildingPlotState.Empty;
                PublishLifecycle();
                if (cancelledSingleton) _registry.NotifyConstructionAvailabilityChanged();
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
            if (TryGetFreeWorkPosition(i, out _))
                return TryReserve(ApplicationOrder, i, out reservation);
        }
        return false;
    }

    internal bool TryGetFreeWorkPosition(int slot, out Vector3 position)
    {
        position = default;
        if (!CanReserve || slot < 0 || slot >= _construction.Reservations.Length
            || _construction.Reservations[slot] != null || !_workPositions[slot])
            return false;
        position = _workPositions[slot].position;
        return true;
    }

    internal bool TryReserve(long applicationOrder, int slot, out ConstructionReservation reservation)
    {
        reservation = null;
        if (applicationOrder != ApplicationOrder || !TryGetFreeWorkPosition(slot, out Vector3 position))
            return false;
        reservation = new ConstructionReservation(this, _construction.Id, slot, position);
        _construction.Reservations[slot] = reservation;
        PublishLifecycle();
        if (reservation.IsValid) return true;
        reservation.Dispose();
        reservation = null;
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
        PublishLifecycle();
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
        if (_isChanging || Time.timeScale <= 0f || request.Strength <= 0f || float.IsNaN(request.Strength) || float.IsInfinity(request.Strength) ||
            !(request.Reservation is ConstructionReservation reservation) || !IsReservationValid(reservation))
            return false;
        _isChanging = true;
        try
        {
            float previousWork = _construction.Work;
            _construction.Work = Mathf.Min(Definition.RequiredWork, _construction.Work + request.Strength);
            result = new InteractionResult(null, WorkContributionKind.ConstructionWork, _construction.Work - previousWork);
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
            ConstructionState project = _construction;
            CompletedBuildingFacility facility = CompletedFacility;
            bool success = IsUpgrade
                ? _factory.TryUpgrade(CompletedFacility, Definition, out reason)
                : _factory.TryCreate(Definition, transform, _entrance, _workArea, out facility, out reason);
            if (_construction != project || State != BuildingPlotState.UnderConstruction)
            {
                reason = "The construction project ended during facility setup.";
                return false;
            }
            if (success && facility && facility.HousePopupSource)
                facility.HousePopupSource.Configure(facility.House, this);
            _construction.CompletionFailure = success ? null : reason;
            EndReservations(success ? ConstructionReservationStatus.Completed : ConstructionReservationStatus.CompletionFailed);
            if (success)
            {
                CompletedFacility = facility;
                State = BuildingPlotState.Completed;
                facility.BindWorkshopPlot(this);
                _registry.NotifyProjectCompleted(this);
            }
            else
            {
                _registry.NotifyPlotLifecycleChanged(this);
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
    private void OnEnable()
    {
        if (_registry) _registry.NotifyPlotLifecycleChanged(this);
    }
    private void OnDisable() { EndReservations(ConstructionReservationStatus.Released); PublishLifecycle(); }
    private void PublishLifecycle()
    {
        if (_registry) _registry.NotifyPlotLifecycleChanged(this);
        PublishState();
    }
    private void PublishState()
    {
        if (StateChanged == null) return;
        foreach (Action handler in StateChanged.GetInvocationList())
            try { handler(); } catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
