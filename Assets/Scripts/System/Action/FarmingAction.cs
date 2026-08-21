using UnityEngine;

public class FarmingAction : DefaultAction
{
    // Named seam for a future Farmer skill/proficiency system; always 1f until that exists.
    private const float WorkerEfficiency = 1f;

    private float _workingTime = 3f;
    private float _currentWorkingTime = 0f;
    private IFarmWorkProvider _farmWorkProvider;

    public FarmingAction() : base(ActionType.Farming)
    {
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
            return;

        _farmWorkProvider = actionContext.FarmWorkProvider;
        if (_farmWorkProvider == null || !_farmWorkProvider.CanApplyWork)
        {
            Fail("FarmingAction started without a usable IFarmWorkProvider");
        }
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
        _farmWorkProvider = null;
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

        if (!_farmWorkProvider.TryApplyWork(WorkerEfficiency, out _))
        {
            Fail("FarmWorkSite rejected TryApplyWork");
            return;
        }

        stat.ChangeFatigue(actionCost.FarmingActionPerFatigue);
        stat.ChangeHunger(actionCost.FarmingActionPerHunger);
        stat.ChangeThirst(actionCost.FarmingActionPerThirst);

        Complete();
    }
}
