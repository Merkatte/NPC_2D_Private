using UnityEngine;

public sealed class SleepAction : BaseBuildingAction
{
    private const float LegacySleepSeconds = 2f;
    private HousingLifeSettings _lifeSettings;
    public void ConfigureRecovery(HousingLifeSettings settings) => _lifeSettings = settings;
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
        if (_lifeSettings != null)
        {
            actionContext.Stat.ChangeFatigue(-_lifeSettings.InnFatigueRecoveryPerSecond * Time.deltaTime);
            if (actionContext.Stat.GetFatigue <= 0f) Complete();
            return;
        }
        currentRestTime += Time.deltaTime;
        UpdateCompletion();
    }

    public override void Clear()
    {
        currentRestTime = 0f;
        _lifeSettings = null;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var stat = actionContext.Stat;
        
        if (currentRestTime >= LegacySleepSeconds)
        {
            stat.ChangeFatigue(-stat.GetFatigue);
            Complete();
        }
    }
}
