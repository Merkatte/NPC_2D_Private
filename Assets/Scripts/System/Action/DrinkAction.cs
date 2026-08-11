using UnityEngine;

public class DrinkAction : DefaultAction
{
    private float drinkTime = 1f; //Temp
    private float currentDrinkTime = 0f;
    public DrinkAction() : base(ActionType.Drink)
    {
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
            stat.ChangeThirst(-stat.GetThirst);
            Complete();
        }
    }
}
