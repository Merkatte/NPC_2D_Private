using UnityEngine;
using UnityEngine.PlayerLoop;

public class EatAction : DefaultAction
{
    private float eatTime = 2f; //Temp
    private float currentEatTime = 0f;
    public EatAction() : base(ActionType.Eat) { }

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
            stat.ChangeHunger(-stat.GetHunger);
            Complete();
        }
    }
}
