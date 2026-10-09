using UnityEngine;

public readonly struct MoveRequest
{
    public const float DefaultStoppingDistance = 0.1f;
    private readonly Vector3 _fixedPosition;
    private readonly IMoveTarget _dynamicTarget;

    public float StoppingDistance { get; }
    public MoveMode Mode { get; }
    public bool IsDynamic => _dynamicTarget != null;

    private MoveRequest(Vector3 fixedPosition, IMoveTarget dynamicTarget, float stoppingDistance,
        MoveMode mode = MoveMode.Direct)
    {
        _fixedPosition = fixedPosition;
        _dynamicTarget = dynamicTarget;
        StoppingDistance = stoppingDistance;
        Mode = mode;
    }

    public static MoveRequest Fixed(Vector3 position, float stoppingDistance = DefaultStoppingDistance) =>
        new MoveRequest(position, null, stoppingDistance);

    public static MoveRequest Dynamic(IMoveTarget target, float stoppingDistance) =>
        new MoveRequest(default, target, stoppingDistance);

    public static MoveRequest Navigated(Vector3 position, float stoppingDistance = DefaultStoppingDistance) =>
        new MoveRequest(position, null, stoppingDistance, MoveMode.Navigation);

    public static MoveRequest Navigated(IMoveTarget target, float stoppingDistance) =>
        new MoveRequest(default, target, stoppingDistance, MoveMode.Navigation);

    public bool TryGetPosition(out Vector3 position)
    {
        if (_dynamicTarget != null)
            return _dynamicTarget.TryGetPosition(out position);

        position = _fixedPosition;
        return true;
    }
}
