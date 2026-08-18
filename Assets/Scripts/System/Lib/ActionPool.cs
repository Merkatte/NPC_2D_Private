using System;
using System.Collections.Generic;
using UnityEngine;

public class ActionPool : MonoBehaviour
{
    [SerializeField] private int _defaultCapacity;
    [SerializeField] private int _maxSize;
    
    Dictionary<ActionType, Queue<IAction>> _actionDictionary = new Dictionary<ActionType, Queue<IAction>>();

    void Awake()
    {
        var values = Enum.GetValues(typeof(ActionType));
        for (int i = 0; i < values.Length; ++i)
        {
            _actionDictionary.Add((ActionType)values.GetValue(i), new Queue<IAction>());
            for (int j = 0; j < _defaultCapacity; ++j)
            {
                Create((ActionType)values.GetValue(i));
            }
        }
    }

    public IAction GetAction(ActionType actionType)
    {
        if (!_actionDictionary.TryGetValue(actionType, out var queue))
            return null;

        if (queue.Count == 0)
            Create(actionType);

        if (queue.Count == 0)
        {
            Debug.LogError($"ActionPool has no factory for {actionType}");
            return null;
        }

        return queue.Dequeue();
    }

    public void ReturnAction(IAction action)
    {
        if (action == null)
            return;

        action.Clear();

        if (!_actionDictionary.ContainsKey(action.GetMyActionType()))
            return;

        _actionDictionary[action.GetMyActionType()].Enqueue(action);
    }


    void Create(ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.Move:
                _actionDictionary[actionType].Enqueue(new MoveAction());
                break;
            case ActionType.Eat:
                _actionDictionary[actionType].Enqueue(new EatAction());
                break;
            case ActionType.Drink:
                _actionDictionary[actionType].Enqueue(new DrinkAction());
                break;
            case ActionType.Farming:
                _actionDictionary[actionType].Enqueue(new FarmingAction());
                break;
            case ActionType.Sleep:
                _actionDictionary[actionType].Enqueue(new SleepAction());
                break;
            case ActionType.Idle:
                _actionDictionary[actionType].Enqueue(new IdleAction());
                break;
            case ActionType.Guard:
                _actionDictionary[actionType].Enqueue(new GuardAction());
                break;
            case ActionType.Attack:
                _actionDictionary[actionType].Enqueue(new AttackAction());
                break;
            default:
                Debug.LogError($"ActionPool.Create has no case for {actionType}");
                break;
        }
    }
}
