using UnityEngine;

public class EatAction : DefaultAction
{
    private float eatTime = 2f; //Temp
    private float currentEatTime = 0f;
    public EatAction() : base(ActionType.Eat) { }

    public override void Start()
    {
        base.Start();
        if (actionContext.InteractionProvider == null || actionContext.Request == null ||
            !actionContext.InteractionProvider.CanInteract(GetMyActionType()))
        {
            Debug.LogError("Current InteractionProvider does not support this action");
            Complete();
        }
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || _isComplete)
        {
            return;
        }

        currentEatTime += Time.deltaTime;
        UpdateCompletion();
    }

    public override void Clear()
    {
        currentEatTime = 0f;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var stat = actionContext.Stat;

        if (currentEatTime >= eatTime)
        {
            if (actionContext.InteractionProvider != null && actionContext.Request != null &&
                actionContext.InteractionProvider.TryInteraction(actionContext.Request.Value, out var result))
            {
                stat.ApplyStatEffect(result.Effect);
            }
            Complete();
        }
    }
}
