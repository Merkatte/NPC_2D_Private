using UnityEngine;

public class FarmingAction : DefaultAction
{
    private float _workingTime = 10f;
    private float _currentWorkingTime = 0f;
    
    public FarmingAction() : base(ActionType.Farming)
    {
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || _isComplete)
        {
            return;
        }

        if (!actionContext.Component)
        {
            Stop();
            return;
        }

        _currentWorkingTime += Time.deltaTime;
        if(_currentWorkingTime >= _workingTime)
            UpdateCompletion();
    }

    protected override void UpdateCompletion()
    {
        var _stat = actionContext.Stat;
        var actionCost = actionContext.CostInfo as FarmingActionCost;
        Debug.Log("Casting Success +" + actionCost.name);
        
        
        _stat.ChangeFatigue(actionCost.FarmingActionPerFatigue);
        _stat.ChangeHunger(actionCost.FarmingActionPerHunger);
        _stat.ChangeThirst(actionCost.FarmingActionPerThirst);

        Complete();
    }
}
