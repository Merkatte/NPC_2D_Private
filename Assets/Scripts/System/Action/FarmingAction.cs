using UnityEngine;

public class FarmingAction : DefaultAction
{
    private float _workingTime = 3f;
    private float _currentWorkingTime = 0f;
    
    public FarmingAction() : base(ActionType.Farming)
    {
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished)
        {
            return;
        }

        if (!actionContext.Component)
        {
            Fail("FarmingAction lost its NPCComponent reference");
            return;
        }

        _currentWorkingTime += Time.deltaTime;
        if(_currentWorkingTime >= _workingTime)
            UpdateCompletion();
    }

    public override void Clear()
    {
        _currentWorkingTime = 0f;
        base.Clear();
    }

    protected override void UpdateCompletion()
    {
        var stat = actionContext.Stat;
        var actionCost = actionContext.CostInfo as FarmingActionCost;

        if (actionCost == null)
        {
            Fail("FarmingAction has no valid FarmingActionCost in ActionContext");
            return;
        }

        stat.ChangeFatigue(actionCost.FarmingActionPerFatigue);
        stat.ChangeHunger(actionCost.FarmingActionPerHunger);
        stat.ChangeThirst(actionCost.FarmingActionPerThirst);
        
        Debug.Log("Work is done!");
        Complete();
    }
}
