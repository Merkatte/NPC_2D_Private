using UnityEngine;

// Deliberately DefaultAction, not BaseWorkingAction: the Tool animator layer's ToolWork state
// is gated on IsWorking, and swinging the hoe while setting cargo down at the warehouse would
// look wrong. This is one second of standing still with no stat cost.
public class DepositAction : DefaultAction
{
    private float _workingTime = 1f;
    private float _currentWorkingTime = 0f;
    private IInteractionProvider _interactionProvider;
    private InteractionRequest _request;

    public DepositAction() : base(ActionType.Deposit)
    {
    }

    public override void Start()
    {
        base.Start();
        if (IsFinished)
            return;

        if (actionContext.InteractionProvider == null || actionContext.Request == null)
        {
            Fail("DepositAction started without a usable IInteractionProvider");
            return;
        }

        if (!actionContext.Request.Value.HasCargo)
        {
            Fail("DepositAction started without carried cargo (selector wiring error)");
            return;
        }

        _interactionProvider = actionContext.InteractionProvider;

        if (!_interactionProvider.CanInteract(GetMyActionType()))
        {
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
            Fail("DepositAction lost its NPCComponent reference");
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
        // Cargo already empty (e.g. two Deposit queues raced) is not a failure — nothing to do.
        if (_request.Cargo.IsEmpty)
        {
            Complete();
            return;
        }

        if (!_interactionProvider.TryInteract(_request, out _))
        {
            // Warehouse refused (or accepted only part of) the transfer. Cargo already reflects
            // whatever WarehouseInventory actually took, and any remainder stays carried — this
            // is a normal outcome, not a config error, so replan rather than fail.
            RequestReplan();
            return;
        }

        Complete();
    }
}
