using UnityEngine;

public class HarvestAction : BaseWorkingAction
{
    private float _workingTime = 3f;
    private float _currentWorkingTime = 0f;
    private IInteractionProvider _interactionProvider;
    private InteractionRequest _request;

    public HarvestAction() : base(ActionType.Harvest)
    {
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
            return;

        if (actionContext.InteractionProvider == null || actionContext.Request == null)
        {
            Fail("HarvestAction started without a usable IInteractionProvider");
            return;
        }

        if (!actionContext.Request.Value.HasCargo)
        {
            Fail("HarvestAction started without carried cargo (selector wiring error)");
            return;
        }

        _interactionProvider = actionContext.InteractionProvider;

        if (!_interactionProvider.CanInteract(GetMyActionType()))
        {
            // Another Farmer may have emptied the crop, or the site is no longer in Harvesting
            // phase — a normal environment change, not a config error.
            RequestReplan();
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
            Fail("HarvestAction lost its NPCComponent reference");
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
            Fail("HarvestAction has no valid FarmingActionCost in ActionContext");
            return;
        }

        if (!_interactionProvider.TryInteract(_request, out _))
        {
            // Cargo is already full, or holds a different item type — normal, not a failure.
            RequestReplan();
            return;
        }

        // Reached even on a partial carry: the worker genuinely worked, so the cost applies
        // whether or not the whole yield fit in cargo this attempt.
        stat.ChangeFatigue(actionCost.FarmingActionPerFatigue);
        stat.ChangeHunger(actionCost.FarmingActionPerHunger);
        stat.ChangeThirst(actionCost.FarmingActionPerThirst);

        Complete();
    }
}
