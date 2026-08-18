using UnityEngine;

public abstract class DefaultAction : IAction
{
    private readonly ActionType _actionType;

    protected ActionContext actionContext;
    protected bool _isPaused;
    protected bool _isRunning;
    private ActionResult _result;

    public DefaultAction(ActionType actionType)
    {
        _actionType = actionType;
    }

    protected bool IsFinished => _result != ActionResult.Running;

    public virtual void Init(ActionContext context)
    {
        actionContext = context;

        _isPaused = false;
        _isRunning = false;
        _result = ActionResult.Running;
    }

    public virtual void Start()
    {
        if (!actionContext.Component)
        {
            Fail($"{_actionType} action started without a valid NPCComponent");
            return;
        }

        _isRunning = true;
        _isPaused = false;
    }


    public abstract void Tick();

    public virtual void Pause()
    {
        if (!_isRunning || IsFinished)
        {
            return;
        }

        _isPaused = true;
    }

    public virtual void Resume()
    {
        if (!_isRunning || IsFinished)
        {
            return;
        }

        _isPaused = false;
    }

    public virtual void Stop()
    {
        _isRunning = false;
        _isPaused = false;
    }

    public virtual void Clear()
    {
        actionContext = default;
        _isPaused = false;
        _isRunning = false;
        _result = ActionResult.Running;
    }

    public ActionResult Result => _result;

    public ActionType GetMyActionType() => _actionType;

    protected abstract void UpdateCompletion();

    protected virtual void Complete()
    {
        _result = ActionResult.Completed;
        _isRunning = false;
        _isPaused = false;
    }

    protected virtual void RequestReplan()
    {
        _result = ActionResult.ReplanRequested;
        _isRunning = false;
        _isPaused = false;
    }

    protected virtual void Fail(string reason)
    {
        Debug.LogError($"{_actionType} action failed: {reason}");
        _result = ActionResult.Failed;
        _isRunning = false;
        _isPaused = false;
    }
}
