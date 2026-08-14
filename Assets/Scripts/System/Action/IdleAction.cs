using UnityEngine;

public sealed class IdleAction : DefaultAction
{
    private float _idleTime = 1f; //Temp
    private float _currentIdleTime = 0f;

    public IdleAction() : base(ActionType.Idle)
    {
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || _isComplete)
        {
            return;
        }

        _currentIdleTime += Time.deltaTime;
        UpdateCompletion();
    }

    public override void Clear()
    {
        _currentIdleTime = 0f;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        if (_currentIdleTime >= _idleTime)
        {
            Complete();
        }
    }
}
