using UnityEngine;

public class EatAction : DefaultAction
{
    private float eatTime = 2f; //Temp
    private float currentEatTime = 0f;
    public EatAction() : base(ActionType.Eat) { }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
        {
            return;
        }

        if (actionContext.InteractionProvider == null || actionContext.Request == null ||
            !actionContext.InteractionProvider.CanInteract(GetMyActionType()))
        {
            Fail("Current InteractionProvider does not support this action");
        }
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
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
            if (actionContext.InteractionProvider == null || actionContext.Request == null ||
                !actionContext.InteractionProvider.TryInteract(actionContext.Request.Value, out var result))
            {
                Fail("InteractionProvider rejected the Eat transaction");
                return;
            }

            if (result.HasActorEffect)
                stat.ApplyStatEffect(result.ActorEffect);

            Complete();
        }
    }
}
