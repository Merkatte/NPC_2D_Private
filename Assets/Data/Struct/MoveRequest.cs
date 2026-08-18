using UnityEngine;

public readonly struct MoveRequest
{
    private readonly Vector3 _fixedPosition;
    private readonly IMoveTarget _dynamicTarget;

    public float StoppingDistance { get; }

    private MoveRequest(Vector3 fixedPosition, IMoveTarget dynamicTarget, float stoppingDistance)
    {
        _fixedPosition = fixedPosition;
        _dynamicTarget = dynamicTarget;
        StoppingDistance = stoppingDistance;
    }

    public static MoveRequest Fixed(Vector3 position, float stoppingDistance) =>
        new MoveRequest(position, null, stoppingDistance);

    public static MoveRequest Dynamic(IMoveTarget target, float stoppingDistance) =>
        new MoveRequest(default, target, stoppingDistance);

    public bool TryGetPosition(out Vector3 position)
    {
        if (_dynamicTarget != null)
            return _dynamicTarget.TryGetPosition(out position);

        position = _fixedPosition;
        return true;
    }
}
