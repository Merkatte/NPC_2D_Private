using UnityEngine;

public class DrinkAction : DefaultAction
{
    private float drinkTime = 1f; //Temp
    private float currentDrinkTime = 0f;
    public DrinkAction() : base(ActionType.Drink)
    {
    }

    public override void Start()
    {
        base.Start();
        if (!actionContext.InteractionProvider.CanInteract(GetMyActionType()))
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
        currentDrinkTime += Time.deltaTime;
        UpdateCompletion();
    }

    public override void Clear()
    {
        currentDrinkTime = 0f;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var stat = actionContext.Stat;
        
        if (currentDrinkTime >= drinkTime)
        {
            if (actionContext.InteractionProvider.TryInteraction(GetMyActionType(), out var result))
            {
                stat.ApplyStatEffect(result.Effect);
            }
            Complete();
        }
    }
}
