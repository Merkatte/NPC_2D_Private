using UnityEngine;

public sealed class SleepAction : DefaultAction
{
    private float sleepTime = 2f; //Temp
    private float currentRestTime = 0f;

    public SleepAction() : base(ActionType.Sleep)
    {

    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || _isComplete)
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
        if (currentRestTime >= sleepTime)
        {
            _stat.ChangeFatigue(-_stat.GetHunger);
            Complete();
        }
    }
}
