using UnityEngine;

public sealed class MoveAction : DefaultAction
{
    private const float DefaultStoppingDistance = 0.1f;

    private float _stoppingDistance = DefaultStoppingDistance;
    private Vector3 _fixedDestination;
    private MoveRequest? _moveRequest;

    public MoveAction() : base(ActionType.Move)
    {
    }

    public override void Init(ActionContext context)
    {
        if (context.MoveRequest.HasValue)
        {
            _moveRequest = context.MoveRequest;
            _stoppingDistance = Mathf.Max(0f, _moveRequest.Value.StoppingDistance);
        }
        else
        {
            _moveRequest = null;
            _stoppingDistance = DefaultStoppingDistance;
        }

        _fixedDestination = context.Destination ?? Vector3.zero;
        base.Init(context);
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
        {
            return;
        }

        UpdateCompletion();
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
        {
            return;
        }

        var component = actionContext.Component;
        if (!component)
        {
            Fail("MoveAction lost its NPCComponent reference");
            return;
        }

        if (!TryGetDestination(out Vector3 destination))
        {
            RequestReplan();
            return;
        }

        Vector3 toDestination = destination - component.Position;
        toDestination.z = 0f;

        if (toDestination.sqrMagnitude <= _stoppingDistance * _stoppingDistance)
        {
            Complete();
            return;
        }

        component.Flip(toDestination.x <= 0f);
        component.Move(toDestination.normalized);
    }

    public override void Clear()
    {
        _stoppingDistance = DefaultStoppingDistance;
        _fixedDestination = Vector3.zero;
        _moveRequest = null;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var component = actionContext.Component;
        if (!component)
        {
            return;
        }

        if (!TryGetDestination(out Vector3 destination))
        {
            RequestReplan();
            return;
        }

        Vector3 toDestination = destination - component.Position;
        toDestination.z = 0f;

        if (toDestination.sqrMagnitude <= _stoppingDistance * _stoppingDistance)
        {
            Complete();
        }
    }

    private bool TryGetDestination(out Vector3 destination)
    {
        if (_moveRequest.HasValue)
        {
            return _moveRequest.Value.TryGetPosition(out destination);
        }

        destination = _fixedDestination;
        return true;
    }
}
