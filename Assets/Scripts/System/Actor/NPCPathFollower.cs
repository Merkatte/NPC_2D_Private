using System.Collections.Generic;
using UnityEngine;

// Each rented action owns one follower; neither the grid nor the actor root owns its cursor.
public sealed class NPCPathFollower
{
    private const float PositionTolerance = 0.001f;
    private const float FailureReplanDelaySeconds = 1f;

    private readonly List<Vector3> _path = new List<Vector3>();
    private NPCComponent _component;
    private NPCStat _stat;
    private INavigationService _navigation;
    private MoveRequest _request;
    private Vector3 _expectedPosition;
    private int _nextPoint;
    private float _failureElapsed;
    private bool _active;
    private INavigationRevision _revisionSource;
    private int _pathRevision;
    private Vector3 _pathTarget;
    private const float MovingTargetRepathDistance = 0.5f;

    public bool HasArrived { get; private set; }
    public bool RequiresReplan { get; private set; }
    public NavigationFailure Failure { get; private set; }

    public void Begin(NPCComponent component, NPCStat stat, MoveRequest request, INavigationService navigation)
    {
        Clear();
        _component = component;
        _stat = stat;
        _request = request;
        _navigation = navigation;
        _revisionSource = navigation as INavigationRevision;
        _active = true;
        if (!_component || _stat == null)
        {
            SetFailure(NavigationFailure.InvalidConfiguration);
            return;
        }
        _expectedPosition = component.Position;
        BuildPath();
    }

    public void Tick(float deltaTime)
    {
        if (!_active || HasArrived || RequiresReplan || deltaTime <= 0f)
            return;
        if (!_component || _stat == null)
        {
            RequiresReplan = true;
            return;
        }

        if (Failure != NavigationFailure.None)
        {
            _failureElapsed += deltaTime;
            RequiresReplan = _failureElapsed >= FailureReplanDelaySeconds;
            return;
        }

        if (_request.Mode == MoveMode.Navigation)
        {
            if (_navigation == null || !_navigation.IsReady)
            {
                SetFailure(NavigationFailure.InvalidConfiguration);
                return;
            }
            // Visual bobbing changes a child, not Position. Only an external displacement
            // invalidates this route, including a direct leg inside a free movement area.
            bool targetMoved = false;
            if (_request.IsDynamic)
            {
                if (!_request.TryGetPosition(out Vector3 currentTarget)) { RequiresReplan = true; return; }
                targetMoved = DistanceSquared(currentTarget, _pathTarget) > MovingTargetRepathDistance * MovingTargetRepathDistance;
            }
            if ((_revisionSource != null && _revisionSource.Revision != _pathRevision) || targetMoved ||
                DistanceSquared(_component.Position, _expectedPosition) > PositionTolerance * PositionTolerance)
            {
                BuildPath();
                if (Failure != NavigationFailure.None)
                    return;
            }
        }
        else
        {
            if (!_request.TryGetPosition(out Vector3 target) || !IsFinite(target))
            {
                RequiresReplan = true;
                return;
            }
            _path[0] = target;
        }

        float step = Mathf.Max(0f, _stat.GetMoveSpeed * deltaTime);
        if (step <= 0f || float.IsInfinity(step) || float.IsNaN(step))
            return;
        float remaining = step;
        while (_nextPoint < _path.Count)
        {
            Vector3 offset = _path[_nextPoint] - _component.Position;
            offset.z = 0f;
            float distance = offset.magnitude;
            bool last = _nextPoint == _path.Count - 1;
            float stoppingDistance = last ? Mathf.Max(0f, _request.StoppingDistance) : PositionTolerance;
            if (distance <= stoppingDistance)
            {
                ++_nextPoint;
                continue;
            }
            if (remaining <= 0f)
                break;

            float travel = Mathf.Min(remaining, distance);
            _component.Flip(offset.x <= 0f);
            // Move applies speed * Time.deltaTime. Tick's deltaTime is that same gameplay
            // clock; fractions share a single frame budget across waypoint corners.
            _component.Move(offset / distance * (travel / step));
            remaining -= travel;
        }
        _expectedPosition = _component.Position;
        HasArrived = _nextPoint >= _path.Count;
    }

    public void Clear()
    {
        _path.Clear();
        _component = null;
        _stat = null;
        _navigation = null;
        _revisionSource = null;
        _pathRevision = 0;
        _pathTarget = default;
        _request = default;
        _expectedPosition = default;
        _nextPoint = 0;
        _failureElapsed = 0f;
        _active = false;
        HasArrived = false;
        RequiresReplan = false;
        Failure = NavigationFailure.None;
    }

    private void BuildPath()
    {
        _path.Clear();
        _nextPoint = 0;
        HasArrived = false;
        if (!_request.TryGetPosition(out Vector3 target) || !IsFinite(target) ||
            float.IsNaN(_request.StoppingDistance) || float.IsInfinity(_request.StoppingDistance))
        {
            SetFailure(NavigationFailure.InvalidDestination);
            return;
        }
        if (_request.Mode == MoveMode.Navigation)
        {
            if (_navigation == null || !_navigation.IsReady)
            {
                SetFailure(NavigationFailure.InvalidConfiguration);
                return;
            }
            if (!_navigation.TryBuildPath(_component.Position, target, _path, out NavigationFailure failure))
            {
                SetFailure(failure == NavigationFailure.None ? NavigationFailure.NoPath : failure);
                return;
            }
            if (_path.Count == 0)
            {
                SetFailure(NavigationFailure.NoPath);
                return;
            }
        }
        else
        {
            _path.Add(target);
        }
        _expectedPosition = _component.Position;
        _pathTarget = target;
        _pathRevision = _revisionSource?.Revision ?? 0;
        // Navigation must traverse its intermediate legs even if the final point is close.
        HasArrived = _path.Count == 1 && DistanceSquared(_expectedPosition, target) <=
            Mathf.Max(0f, _request.StoppingDistance) * Mathf.Max(0f, _request.StoppingDistance);
    }

    private void SetFailure(NavigationFailure failure)
    {
        _path.Clear();
        Failure = failure;
        _failureElapsed = 0f;
    }

    private static float DistanceSquared(Vector3 left, Vector3 right)
    {
        Vector3 difference = left - right;
        difference.z = 0f;
        return difference.sqrMagnitude;
    }

    private static bool IsFinite(Vector3 position) =>
        !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
        !float.IsNaN(position.y) && !float.IsInfinity(position.y);
}
