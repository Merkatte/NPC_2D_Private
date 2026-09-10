/// <summary>
/// A worker rented from the pool but not yet initialized: NPCManager.TryReserveWorker hands this
/// back before spending gold, so a caller can cancel it (return the worker to the pool) if the
/// transaction fails downstream. Selector is not created here — it's the existing reference
/// already serialized on the role's NPCCreationEntry. Stat is created fresh at reservation time so
/// CanUseStat failures surface before any gold is spent.
///
/// The constructor is internal only to keep external Assembly-CSharp assemblies from fabricating
/// one — this project has no .asmdef, so it does not stop other code inside Assembly-CSharp (e.g.
/// TestOnly) from calling it. The only production caller is NPCManager; actual reservation
/// ownership is enforced there via NPCManager._reservedWorkers, not by this type.
/// </summary>
public readonly struct WorkerReservation
{
    public WorkerNPC Worker { get; }
    public NPCType NpcType { get; }
    public NPCStat Stat { get; }
    public BaseNPCActionSelector Selector { get; }

    public bool IsValid => Worker;

    internal WorkerReservation(WorkerNPC worker, NPCType npcType, NPCStat stat, BaseNPCActionSelector selector)
    {
        Worker = worker;
        NpcType = npcType;
        Stat = stat;
        Selector = selector;
    }
}
