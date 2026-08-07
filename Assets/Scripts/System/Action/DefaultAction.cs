using UnityEngine;

public abstract class DefaultAction : IAction
{
    private readonly ActionType _actionType;
    
    protected NPCComponent _component;
    protected NPCStat _stat;
    protected bool _isPaused;
    protected bool _isRunning;
    protected bool _isComplete;
 
    public DefaultAction(ActionType actionType)
    {
        _actionType = actionType;
    }
    public virtual void Init(ActionContext context)
    {
        _component = context.Component;
        _stat = context.Stat;
        
        _isPaused = false;
        _isRunning = false;
        _isComplete = false;

        Start();
    }

    public virtual void Start()
    {
        if (!_component)
        {
            _isComplete = true;
            _isRunning = false;
            return; 
        }

        _isRunning = true;
        _isPaused = false;
    }


    public abstract void Tick();

    public virtual void Pause()
    {
        if (!_isRunning || _isComplete)
        {
            return;
        }

        _isPaused = true;
    }

    public virtual void Resume()
    {
        if (!_isRunning || _isComplete)
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
        _component = null;
        _isPaused = false;
        _isRunning = false;
        _isComplete = false;
    }

    public virtual bool CheckComplete()
    {
        return _isComplete;
    }

    public ActionType GetMyActionType() => _actionType;

    protected abstract void UpdateCompletion();
    protected virtual void Complete()
    {
        _isComplete = true;
        _isRunning = false;
        _isPaused = false;
    }
}
