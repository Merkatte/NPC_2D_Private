using UnityEngine;

public sealed class MoveAction : DefaultAction
{
    private const float DefaultStoppingDistance = 0.05f;
    private float _stoppingDistance = DefaultStoppingDistance;
    private Vector3 _destination;

    public MoveAction() : base(ActionType.Move)
    {
    }

    public bool IsComplete => _isComplete;
    public bool IsRunning => _isRunning;

    public override void Init(ActionContext context)
    {
        _stoppingDistance = Mathf.Max(0f, 0.1f);
        _destination = context.Destination ?? Vector3.zero;
        base.Init(context);
    }

    public override void Start()
    {
        base.Start();
        UpdateCompletion();
    }

    public override void Tick()
    {
        var component = actionContext.Component;
        
        if (!_isRunning || _isPaused || _isComplete)
        {
            return;
        }

        if (!component)
        {
            Stop();
            return;
        }

        Vector3 toDestination = _destination - component.Position;
        toDestination.z = 0f;

        if (toDestination.sqrMagnitude <= _stoppingDistance * _stoppingDistance)
        {
            Complete();
            return;
        }
        
        if(toDestination.x > 0f)
            component.Flip(false);
        else component.Flip(true);
        
        component.Move(toDestination.normalized);
        UpdateCompletion();
    }
    

    public override void Clear()
    {
        _stoppingDistance = DefaultStoppingDistance;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var component = actionContext.Component;
        
        if (!component)
        {
            return;
        }

        Vector3 toDestination = _destination - component.Position;
        toDestination.z = 0f;

        if (toDestination.sqrMagnitude <= _stoppingDistance * _stoppingDistance)
        {
            Complete();
        }
    }
}
