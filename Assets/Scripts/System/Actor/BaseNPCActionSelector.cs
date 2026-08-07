using System.Collections.Generic;
using UnityEngine;

public class BaseNPCActionSelector : MonoBehaviour
{
    [SerializeField] protected ActionPool actionPool;

    protected virtual void Start()
    {
        
    }

    protected virtual IAction GetAction(ActionType type)
    {
        return actionPool.GetAction(type);
    }

    public virtual void ReturnAction(IAction action)
    {
        actionPool.ReturnAction(action);
    }

    public virtual Queue<IAction> RequestNewActionQueue(NPCStat stat, NPCType npcType, NPCComponent component)
    {
        return null;
    }
}
