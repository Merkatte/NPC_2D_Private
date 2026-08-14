using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WorkerNPC : MonoBehaviour
{
    private IAction _currentAction;
    private NPCStat _stat;
    private BaseNPCActionSelector _selector;
    
    [SerializeField] private NPCComponent _component;
    Queue<IAction> _actionQueue;
    public void Init(NPCStat stat, BaseNPCActionSelector selector)
    {
        _stat = stat;
        _component.Init(_stat);
        _selector = selector;
        SetNextAction();
    }

    void Update()
    {
        if (_currentAction == null || _currentAction.CheckComplete())
        {
            SetNextAction();
            return;
        }

        _currentAction.Tick();
    }

    void SetNextAction()
    { 
        if (_actionQueue == null || _actionQueue.Count == 0)
        {
            _actionQueue = _selector.RequestNewActionQueue(_stat, NPCType.Farmer, _component);
            Debug.Log(_actionQueue.Count);
            return;
        }
        if(_currentAction != null)
            _selector.ReturnAction(_currentAction);
        
        _currentAction = _actionQueue.Dequeue();
    }
}
