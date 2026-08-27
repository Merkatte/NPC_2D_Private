using UnityEngine;

public sealed class SleepAction : BaseBuildingAction
{
    private float sleepTime = 2f; //Temp
    private float currentRestTime = 0f;

    public SleepAction() : base(ActionType.Sleep)
    {

    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
        {
            return;
        }
        currentRestTime += Time.deltaTime;
        UpdateCompletion();
    }

    public override void Clear()
    {
        currentRestTime = 0f;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var stat = actionContext.Stat;
        
        if (currentRestTime >= sleepTime)
        {
            stat.ChangeFatigue(-stat.GetFatigue);
            Complete();
        }
    }
}
