using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DefenseBattlefield : MonoBehaviour
{
    [SerializeField] private TilemapNavigation _navigation;
    [SerializeField] private BoxCollider2D _interiorBounds;
    [SerializeField] private DefenseWallSegment[] _walls = Array.Empty<DefenseWallSegment>();
    [SerializeField] private Transform[] _guardPositions = Array.Empty<Transform>();
    private readonly List<DefenseActor> _actors = new List<DefenseActor>();
    private readonly List<DefenseBuildingDurability> _buildings = new List<DefenseBuildingDurability>();
    private readonly List<DefenseArcherSlotLease> _slots = new List<DefenseArcherSlotLease>();
    private readonly List<Vector3> _pathBuffer = new List<Vector3>();
    private IReadOnlyList<DefenseActor> _actorView;
    private int _nextGuardPosition;
    private bool _lastAlert;
    public TilemapNavigation Navigation => _navigation;
    public IReadOnlyList<DefenseActor> Actors => _actorView ?? (_actorView = _actors.AsReadOnly());
    public event Action Changed;
    public int AliveEnemyCount
    {
        get { int count = 0; foreach (DefenseActor actor in _actors) if (actor && actor.IsEnemy && actor.CanAct) ++count; return count; }
    }
    public bool IsAlert
    {
        get
        {
            foreach (DefenseWallSegment wall in _walls) if (wall && wall.IsPassable) return true;
            foreach (DefenseActor actor in _actors)
                if (actor && actor.IsEnemy && actor.CanAct && IsInside(actor.Position)) return true;
            return false;
        }
    }
    public bool HasArcherSlot
    {
        get { foreach (DefenseWallSegment wall in _walls) if (wall && !IsReserved(wall)) return true; return false; }
    }
    public bool IsInside(Vector3 position)
        => Contains(_interiorBounds, position);
    private static bool Contains(BoxCollider2D box, Vector3 position)
    {
        if (!box) return false;
        Vector2 local = box.transform.InverseTransformPoint(position);
        local -= box.offset;
        return Mathf.Abs(local.x) <= box.size.x * 0.5f && Mathf.Abs(local.y) <= box.size.y * 0.5f;
    }
    public Vector3 Register(DefenseActor actor)
    {
        if (!_actors.Contains(actor)) _actors.Add(actor);
        Vector3 guardPosition = actor.Position;
        if (actor.Role == NPCType.Guard && _guardPositions.Length > 0)
        {
            Transform anchor = _guardPositions[_nextGuardPosition++ % _guardPositions.Length];
            if (anchor) guardPosition = anchor.position;
        }
        NotifyChanged();
        return guardPosition;
    }
    public void Unregister(DefenseActor actor) { if (_actors.Remove(actor)) NotifyChanged(); }
    public void RegisterBuilding(DefenseBuildingDurability building)
    { if (building && !_buildings.Contains(building)) { _buildings.Add(building); NotifyChanged(); } }
    public void UnregisterBuilding(DefenseBuildingDurability building)
    { if (_buildings.Remove(building)) NotifyChanged(); }
    public DefenseActor FindActor(NPCComponent component)
    { foreach (DefenseActor actor in _actors) if (actor && actor.Component == component) return actor; return null; }
    public void NotifyChanged() { Changed?.Invoke(); }
    private void Update()
    {
        bool alert = IsAlert;
        if (alert == _lastAlert) return;
        _lastAlert = alert; NotifyChanged();
    }
    public bool TryReserveArcherSlot(out DefenseArcherSlotLease lease)
    {
        lease = null;
        foreach (DefenseWallSegment wall in _walls)
        {
            if (!wall || IsReserved(wall)) continue;
            lease = new DefenseArcherSlotLease(this, wall); _slots.Add(lease); NotifyChanged(); return true;
        }
        return false;
    }
    private bool IsReserved(DefenseWallSegment wall)
    { foreach (DefenseArcherSlotLease slot in _slots) if (slot.IsValid && slot.Wall == wall) return true; return false; }
    internal void ReleaseArcherSlot(DefenseArcherSlotLease slot) { if (_slots.Remove(slot)) NotifyChanged(); }
    public CombatTarget FindEnemy(Vector3 from, float range, bool insideOnly)
    {
        CombatTarget target = null; float best = range * range;
        foreach (DefenseActor actor in _actors)
        {
            if (!actor || !actor.IsEnemy || !actor.CanAct || (insideOnly && !IsInside(actor.Position))) continue;
            Consider(actor.Target, from, ref best, ref target);
        }
        return target;
    }
    public CombatTarget FindOutsideSwordsman(Vector3 from, float range)
    {
        CombatTarget target = null; float best = range * range;
        foreach (DefenseActor actor in _actors)
        {
            if (!actor || actor.Role != NPCType.Guard || !actor.CanAct || IsInside(actor.Position)) continue;
            Consider(actor.Target, from, ref best, ref target);
        }
        return target;
    }
    public CombatTarget FindInsideTarget(Vector3 from, INavigationService navigation)
    {
        CombatTarget target = null; float best = float.PositiveInfinity;
        foreach (DefenseActor actor in _actors)
            if (actor && !actor.IsEnemy && actor.CanAct && IsInside(actor.Position)) ConsiderReachable(actor.Target, from, navigation, ref best, ref target);
        foreach (DefenseBuildingDurability building in _buildings)
            if (building && building.IsFunctional) ConsiderReachable(building.Target, from, navigation, ref best, ref target);
        return target;
    }
    private void ConsiderReachable(CombatTarget candidate, Vector3 from, INavigationService navigation,
        ref float best, ref CombatTarget target)
    {
        if (!candidate || !candidate.CanBeTargeted || navigation == null) return;
        float distance = ((Vector2)(candidate.Position - from)).sqrMagnitude;
        if (distance >= best || !navigation.TryBuildPath(from, candidate.Position, _pathBuffer, out _)) return;
        best = distance; target = candidate;
    }
    private static void Consider(CombatTarget candidate, Vector3 from, ref float best, ref CombatTarget target)
    {
        if (!candidate || !candidate.CanBeTargeted) return;
        float distance = ((Vector2)(candidate.Position - from)).sqrMagnitude;
        if (distance > best || (target && Mathf.Approximately(distance, best))) return;
        best = distance; target = candidate;
    }
    public DefenseWallSegment FindWall(Vector3 from, bool passable, INavigationService navigation)
    {
        DefenseWallSegment selected = null; float best = float.PositiveInfinity;
        foreach (DefenseWallSegment wall in _walls)
        {
            if (!wall || wall.IsPassable != passable || (!passable && !wall.Target.CanBeTargeted)) continue;
            Vector3 destination = passable ? wall.EntryPosition : wall.AttackPosition;
            float distance = ((Vector2)(destination - from)).sqrMagnitude;
            if (distance >= best || navigation == null || !navigation.TryBuildPath(from, destination, _pathBuffer, out _)) continue;
            best = distance; selected = wall;
        }
        return selected;
    }
    public bool IsOccupied(BoxCollider2D area)
    {
        if (!area) return false;
        foreach (DefenseActor actor in _actors)
            if (actor && Contains(area, actor.Position)) return true;
        return false;
    }
    public bool TryFindFleePosition(DefenseActor actor, CombatTarget threat, out Vector3 position)
    {
        position = default;
        if (!actor || !threat || actor.Navigation == null) return false;
        Vector2 away = (Vector2)(actor.Position - threat.Position);
        if (away.sqrMagnitude < 0.01f) away = Vector2.down;
        away.Normalize();
        // Deterministic bounded alternatives, all tested against the real faction path.
        for (int i = 0; i < 8; ++i)
        {
            float angle = (i == 0 ? 0f : ((i + 1) / 2) * 30f * (i % 2 == 0 ? -1f : 1f)) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(away.x * Mathf.Cos(angle) - away.y * Mathf.Sin(angle), away.x * Mathf.Sin(angle) + away.y * Mathf.Cos(angle));
            Vector3 candidate = actor.Position + (Vector3)(direction * actor.Settings.FleeDistance);
            if (IsInside(candidate) && actor.Navigation.TryBuildPath(actor.Position, candidate, _pathBuffer, out _))
            { position = candidate; return true; }
        }
        return false;
    }
}
