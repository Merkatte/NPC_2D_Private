using System;
using UnityEngine;

public sealed class DefenseActor : MonoBehaviour
{
    [SerializeField] private WorkerNPC _worker;
    [SerializeField] private CombatTarget _combatTarget;
    [SerializeField] private DefenseCombatSettings _settings;
    [SerializeField] private DefenseCombatPresentation _presentation;
    [SerializeField] private DefenseProjectile _projectilePrefab;
    private NPCStat _stat;
    private DefenseArcherSlotLease _archerSlot;
    private INavigationService _navigation;
    private bool _isRegistered;
    private bool _isFleeing;
    public DefenseBattlefield Battlefield { get; private set; }
    public NPCType Role { get; private set; }
    public DefenseActorState State { get; private set; }
    public DefenseCombatSettings Settings => _settings;
    public NPCComponent Component => _worker ? _worker.Component : null;
    public DefenseCombatPresentation Presentation => _presentation;
    public DefenseProjectile ProjectilePrefab => _projectilePrefab;
    public CombatTarget Target => _combatTarget;
    public NPCStat Stat => _stat;
    public INavigationService Navigation => _navigation;
    public bool IsEnemy => Role == NPCType.Enemy;
    public bool IsSoldier => Role == NPCType.Guard || Role == NPCType.Archer;
    public bool CanAct => _stat != null && State == DefenseActorState.Active && _stat.CurrentHealth > 0f;
    public bool IsFleeing => _isFleeing;
    public bool IsEmergencyDuty { get; private set; }
    public Vector3 Position => _worker.Component.Position;
    public Vector3 GuardPosition { get; private set; }
    public DefenseWallSegment ArcherWall => _archerSlot?.Wall;
    public DefenseWallSegment CommittedWall { get; set; }
    public DefenseMaintenanceSite CurrentMaintenance { get; set; }
    public DefenseWallSegment SelectedBreach { get; set; }
    public bool PlannedInside { get; set; }
    public Vector3 DutyPosition => Role == NPCType.Archer && ArcherWall
        ? (ArcherWall.IsPassable ? ArcherWall.GroundPosition : ArcherWall.TopPosition) : GuardPosition;
    public event Action FleeStarted;
    public event Action Downed;
    public void BindBattlefield(DefenseBattlefield battlefield) { Battlefield = battlefield; }
    public void AssignArcherSlot(DefenseArcherSlotLease slot) { _archerSlot = slot; }
    public void Initialize(NPCType role, NPCStat stat)
    {
        if (_combatTarget) _combatTarget.Died -= OnHealthDepleted;
        if (_isRegistered && Battlefield) Battlefield.Unregister(this);
        _isRegistered = false;
        _stat = stat; Role = role; State = DefenseActorState.Active; _isFleeing = false;
        IsEmergencyDuty = false;
        CommittedWall = null; SelectedBreach = null; CurrentMaintenance = null;
        if (!Battlefield || !_worker || !_combatTarget || !_settings)
        { Debug.LogError($"DefenseActor '{name}' requires battlefield, worker, target and settings.", this); return; }
        _navigation = new NavigationAccessService(Battlefield.Navigation, IsEnemy ? NavigationAccess.Enemy : NavigationAccess.Friendly);
        _combatTarget.Initialize(stat);
        _combatTarget.Died += OnHealthDepleted;
        GuardPosition = Battlefield.Register(this);
        _isRegistered = true;
        if (_presentation) { _presentation.ResetPose(); _presentation.SetRole(role); }
        if (stat.CurrentHealth <= 0f) OnHealthDepleted();
    }
    public void SetFleeing(bool fleeing)
    {
        if (_isFleeing == fleeing) return;
        _isFleeing = fleeing;
        if (fleeing) FleeStarted?.Invoke();
    }
    public void SetEmergencyDuty(bool isEmergencyDuty) { IsEmergencyDuty = isEmergencyDuty; }
    private void Update()
    {
        if (_stat != null && State == DefenseActorState.Active && _stat.CurrentHealth <= 0f)
            OnHealthDepleted();
    }
    private void OnHealthDepleted()
    {
        if (State == DefenseActorState.Downed) return;
        State = DefenseActorState.Downed;
        _combatTarget.SetTargetable(false);
        _worker.SetActionExecutionEnabled(false);
        _worker.Component.CombatRuntimeState.ClearTarget();
        SetFleeing(false);
        IsEmergencyDuty = false;
        if (_presentation) _presentation.ShowDowned();
        Downed?.Invoke();
        if (IsEnemy)
        {
            if (_isRegistered && Battlefield) Battlefield.Unregister(this);
            _isRegistered = false;
            Destroy(gameObject);
        }
        else if (Battlefield) Battlefield.NotifyChanged();
    }
    public void Unbind()
    {
        if (_combatTarget) { _combatTarget.Died -= OnHealthDepleted; _combatTarget.SetTargetable(false); }
        if (_isRegistered && Battlefield) Battlefield.Unregister(this);
        _isRegistered = false;
        _archerSlot?.Dispose(); _archerSlot = null;
        _stat = null; _navigation = null; CommittedWall = null; CurrentMaintenance = null; SelectedBreach = null; _isFleeing = false;
        IsEmergencyDuty = false;
    }
    private void OnDisable() { Unbind(); }
}
