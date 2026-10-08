using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class House : BaseInteractionProvider
{
    private readonly List<ResidentHousingState> _residents = new List<ResidentHousingState>();
    private IReadOnlyList<ResidentHousingState> _residentView;
    private HousingDataContext _data;
    private HousingManager _manager;
    private Transform _entrance;
    public HouseTierDefinition Tier { get; private set; }
    public int Capacity => Tier?.Capacity ?? 0;
    public IReadOnlyList<ResidentHousingState> Residents => _residentView ?? (_residentView = _residents.AsReadOnly());
    public Vector3 EntrancePosition => _entrance ? _entrance.position : transform.position;
    public event Action Changed;

    public bool TryConfigure(HousingDataContext data, HouseTierDefinition tier, HousingManager manager,
        Transform entrance, out string reason)
    {
        reason = "House requires canonical tier, matching housing manager and entrance.";
        if (!data || !manager || manager.Data != data || !entrance || tier == null ||
            !data.TryGetTier(tier.Tier, out var canonical) || !ReferenceEquals(canonical, tier) || Tier != null) return false;
        _data = data;
        _manager = manager;
        _entrance = entrance;
        Tier = tier;
        if (isActiveAndEnabled) _manager.Register(this);
        reason = null;
        return true;
    }

    public bool TryApplyTier(HouseTierDefinition tier, out string reason)
    {
        reason = "House upgrade requires the next canonical tier without losing capacity.";
        if (!_data || Tier == null || tier == null || tier.Tier != Tier.Tier + 1 ||
            !_data.TryGetTier(tier.Tier, out var canonical) || !ReferenceEquals(canonical, tier) ||
            tier.Capacity < Capacity || tier.Capacity < _residents.Count) return false;
        Tier = tier;
        PublishChanged();
        reason = null;
        return true;
    }

    internal bool TryAddResident(ResidentHousingState resident)
    {
        if (!isActiveAndEnabled || Tier == null || resident == null || !resident.IsRegistered ||
            resident.Home || _residents.Count >= Capacity || _residents.Contains(resident)) return false;
        _residents.Add(resident);
        resident.AssignHome(this);
        PublishChanged();
        return true;
    }

    internal void RemoveResident(ResidentHousingState resident)
    {
        if (!_residents.Remove(resident)) return;
        if (resident.Home == this) resident.AssignHome(null);
        PublishChanged();
    }

    private void PublishChanged()
    {
        if (Changed == null) return;
        foreach (Action listener in Changed.GetInvocationList())
        {
            try { listener(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private void OnEnable() { if (_manager && Tier != null) _manager.Register(this); }
    private void OnDisable() { if (_manager) _manager.Unregister(this); }
    protected override bool SupportsCore(ActionType type) => type == ActionType.HomeStay;
    protected override bool CanInteractCore(ActionType type) => isActiveAndEnabled && Tier != null && _entrance;
    protected override bool TryInitializeCore(out string reason) { reason = null; return true; }
    protected override bool TryGetActionPositionCore(ActionType type, Vector3 fallback, out Vector3 position)
    { position = EntrancePosition; return true; }
    protected override bool TryInteractCore(InteractionRequest request, out InteractionResult result)
    { result = default; return false; }
}
