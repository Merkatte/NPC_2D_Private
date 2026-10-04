using UnityEngine;

public class FarmingAction : BaseWorkingAction
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

        if (actionContext.InteractionProvider == null || actionContext.Request == null)
        {
            Fail("FarmingAction started without a usable IInteractionProvider");
            return;
        }

        _interactionProvider = actionContext.InteractionProvider;

        if (!_interactionProvider.CanInteract(GetMyActionType()))
        {
            // The site flipped out of Growing (e.g. reached max progress) between queue build
            // and this action starting — a normal environment change, not a config error.
            RequestReplan();
            return;
        }

        _request = actionContext.Request.Value;
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || ReplanIfWorkUnavailable())
        {
            return;
        }

        if (!actionContext.Component)
        {
            Fail("FarmingAction lost its NPCComponent reference");
            return;
        }

        if (!ProviderStillUsable())
        {
            RequestReplan();
            return;
        }

        _currentWorkingTime += Time.deltaTime;
        if (_currentWorkingTime >= _workingTime)
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
            // ApplyGrowingWork always succeeds internally, so a false result here can only mean
            // CanInteract flipped between Tick's check and this call — a normal race, not a failure.
            RequestReplan();
            return;
        }

        stat.ChangeFatigue(actionCost.FarmingActionPerFatigue);
        stat.ChangeHunger(actionCost.FarmingActionPerHunger);
        stat.ChangeThirst(actionCost.FarmingActionPerThirst);

        Complete();
    }
}
