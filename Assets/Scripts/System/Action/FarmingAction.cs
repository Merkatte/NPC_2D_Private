using UnityEngine;

public class FarmingAction : DefaultAction
{
    private float _workingTime = 3f;
    private float _currentWorkingTime = 0f;
    private IInteractionProvider _interactionProvider;
    private InteractionRequest _request;

    public FarmingAction() : base(ActionType.Farming)
    {
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
            return;

        _interactionProvider = actionContext.InteractionProvider;
        if (_interactionProvider == null || actionContext.Request == null ||
            !_interactionProvider.CanInteract(GetMyActionType()))
        {
            Fail("FarmingAction started without a usable IInteractionProvider");
            return;
        }

        _request = actionContext.Request.Value;
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
        _interactionProvider = null;
        _request = default;
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

        if (!_interactionProvider.TryInteract(_request, out _))
        {
            Fail("FarmWorkSite rejected the Farming transaction");
            return;
        }

        stat.ChangeFatigue(actionCost.FarmingActionPerFatigue);
        stat.ChangeHunger(actionCost.FarmingActionPerHunger);
        stat.ChangeThirst(actionCost.FarmingActionPerThirst);

        Complete();
    }
}
