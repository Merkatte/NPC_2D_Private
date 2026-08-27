using System.Collections.Generic;
using UnityEngine;

public class WorkerNPC : MonoBehaviour
{
    [SerializeField] private NPCComponent _component;

    private IAction _currentAction;
    private NPCStat _stat;
    private NPCType _npcType;
    private BaseNPCActionSelector _selector;
    private Queue<IAction> _actionQueue;
    private bool _isInitialized;

    public void Init(NPCType npcType, NPCStat stat, BaseNPCActionSelector selector)
    {
        if (_isInitialized)
        {
            CancelAndReturnQueue();
        }

        if (_component)
        {
            _component.ResetRuntimeState();
        }

        _npcType = npcType;
        _stat = stat;
        _component.Init(_stat);
        _component.SetToolVisible(npcType == NPCType.Farmer);
        _selector = selector;

        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_currentAction == null)
        {
            AdvanceQueue();
            return;
        }

        _currentAction.Tick();

        switch (_currentAction.Result)
        {
            case ActionResult.Running:
                break;

            case ActionResult.Completed:
                _selector.ReturnAction(_currentAction);
                _currentAction = null;
                AdvanceQueue();
                break;

            case ActionResult.ReplanRequested:
            case ActionResult.Failed:
                CancelAndReturnQueue();
                AdvanceQueue();
                break;
        }
    }

    void OnDisable()
    {
        if (!_isInitialized)
        {
            return;
        }

        CancelAndReturnQueue();
        _selector = null;
        _stat = null;

        if (_component)
        {
            _component.ResetRuntimeState();
        }

        _isInitialized = false;
    }

    private void AdvanceQueue()
    {
        if (_actionQueue == null || _actionQueue.Count == 0)
        {
            _actionQueue = _selector.RequestNewActionQueue(_stat, _npcType, _component);

            if (_actionQueue == null || _actionQueue.Count == 0)
            {
                _actionQueue = null;
                return;
            }
        }

        _currentAction = _actionQueue.Dequeue();
        _currentAction.Start();
    }

    private void CancelAndReturnQueue()
    {
        if (_currentAction != null)
        {
            IAction action = _currentAction;
            _currentAction = null;
            action.Stop();
            _selector.ReturnAction(action);
        }

        if (_actionQueue == null)
        {
            return;
        }

        while (_actionQueue.Count > 0)
        {
            IAction action = _actionQueue.Dequeue();
            action.Stop();
            _selector.ReturnAction(action);
        }

        _actionQueue = null;
    }
}
