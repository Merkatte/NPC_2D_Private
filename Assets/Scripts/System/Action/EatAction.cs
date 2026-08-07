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
        if (currentEatTime >= eatTime)
        {
            _stat.ChangeHunger(-_stat.GetHunger);
            Complete();
        }
    }
}
