using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BaseNPCActionSelector : MonoBehaviour
{
    [SerializeField] protected DataManager dataManager;
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

    /// <summary>
    /// Whether this selector can operate on the given stat's runtime capabilities.
    /// Called at NPC creation time so a role selector can never be paired with an
    /// incompatible stat definition.
    /// </summary>
    public virtual bool CanUseStat(NPCStat stat)
    {
        return stat != null;
    }

    /// <summary>
    /// Rents one action of the given type, initializes it, and appends it to the caller's
    /// in-progress rental list. Callers must roll back via ReturnAll on any failure so a
    /// partially built queue never leaks rented actions back to the pool.
    /// </summary>
    protected bool TryRentAction(ActionType type, ActionContext context, List<IAction> rented)
    {
        IAction action = GetAction(type);
        if (action == null)
        {
            Debug.LogError($"ActionPool could not provide {type}");
            return false;
        }

        action.Init(context);
        rented.Add(action);
        return true;
    }

    protected void ReturnAll(List<IAction> rented)
    {
        for (int i = 0; i < rented.Count; ++i)
            ReturnAction(rented[i]);
    }
}
