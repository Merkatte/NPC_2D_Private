using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [SerializeField] private WorkerPool _workerPool;
    [SerializeField] private List<NPCCreationEntry> _creationEntries;

    private Dictionary<NPCType, List<WorkerNPC>> _workers;
    private Dictionary<NPCType, NPCCreationEntry> _entryLookup;
    private HashSet<WorkerNPC> _reservedWorkers;

    void Awake()
    {
        _workers = new Dictionary<NPCType, List<WorkerNPC>>();
        _entryLookup = new Dictionary<NPCType, NPCCreationEntry>();
        _reservedWorkers = new HashSet<WorkerNPC>();

        if (_creationEntries == null)
            return;

        foreach (var entry in _creationEntries)
        {
            if (entry == null)
            {
                Debug.LogError("NPCManager has a null creation entry.");
                continue;
            }

            if (!entry.Selector)
            {
                Debug.LogError($"NPCManager creation entry for {entry.NpcType} is missing a selector.");
                continue;
            }

            if (!entry.StatDefinition)
            {
                Debug.LogError($"NPCManager creation entry for {entry.NpcType} is missing a stat definition.");
                continue;
            }

            if (_entryLookup.ContainsKey(entry.NpcType))
            {
                Debug.LogError($"NPCManager has a duplicate creation entry for {entry.NpcType}.");
                continue;
            }

            _entryLookup.Add(entry.NpcType, entry);
        }
    }

    public void CreateNPC(NPCType npcType)
    {
        // Reserve then immediately commit at the origin — identical to the old single-step
        // behavior, since Begin/CompleteSpawnPresentation pair up within the same frame.
        if (TryReserveWorker(npcType, Vector2.zero, out WorkerReservation reservation))
        {
            CommitReservation(reservation);
        }
    }

    /// <summary>
    /// Rents a worker from the pool at spawnPosition without initializing it — the returned
    /// WorkerNPC's Update() stays a no-op (see WorkerNPC._isInitialized) and its gameplay
    /// colliders are disabled (WorkerNPC.BeginSpawnPresentation) until the caller follows up with
    /// CommitReservation or CancelReservation. Validates the creation entry and CanUseStat before
    /// renting so a caller (e.g. TownHallRecruitment) can fail before spending anything irreversible.
    /// </summary>
    public bool TryReserveWorker(NPCType npcType, Vector3 spawnPosition, out WorkerReservation reservation)
    {
        reservation = default;

        if (!_entryLookup.TryGetValue(npcType, out NPCCreationEntry entry))
        {
            Debug.LogError($"NPCManager has no creation entry registered for {npcType}");
            return false;
        }

        if (!_workerPool)
        {
            Debug.LogError("NPCManager has no WorkerPool assigned.");
            return false;
        }

        NPCStat stat = entry.StatDefinition.CreateRuntimeStat();
        if (stat == null || !entry.Selector.CanUseStat(stat))
        {
            Debug.LogError($"NPCManager creation entry for {npcType} has an incompatible stat definition.");
            return false;
        }

        WorkerNPC worker = _workerPool.GetWorker(spawnPosition);
        if (!worker)
        {
            Debug.LogError($"WorkerPool could not provide a worker for {npcType}.");
            return false;
        }

        worker.BeginSpawnPresentation();
        _reservedWorkers.Add(worker);

        reservation = new WorkerReservation(worker, npcType, stat, entry.Selector);
        return true;
    }

    /// <summary>
    /// Finalizes a reservation from TryReserveWorker: restores gameplay physics/collider
    /// participation, initializes the worker, and registers it in _workers. Registration happens
    /// only here — a reservation that gets canceled instead must never leave a dangling, un-Init'd
    /// entry in the population list.
    /// </summary>
    public bool CommitReservation(WorkerReservation reservation)
    {
        if (!reservation.IsValid || !_reservedWorkers.Remove(reservation.Worker))
        {
            return false;
        }

        reservation.Worker.CompleteSpawnPresentation();
        reservation.Worker.Init(reservation.NpcType, reservation.Stat, reservation.Selector);

        if (!_workers.ContainsKey(reservation.NpcType))
            _workers.Add(reservation.NpcType, new List<WorkerNPC>());

        _workers[reservation.NpcType].Add(reservation.Worker);
        return true;
    }

    /// <summary>
    /// Reverts a reservation that never got committed: restores physics/collider participation and
    /// returns the worker to the pool untouched. Never registers it in _workers.
    /// </summary>
    public void CancelReservation(WorkerReservation reservation)
    {
        if (!reservation.IsValid || !_reservedWorkers.Remove(reservation.Worker))
        {
            return;
        }

        reservation.Worker.CompleteSpawnPresentation();
        _workerPool.ReleaseWorker(reservation.Worker);
    }

    [System.Serializable]
    private class NPCCreationEntry
    {
        [SerializeField] private NPCType _npcType;
        [SerializeField] private BaseNPCActionSelector _selector;
        [SerializeField] private NPCStatDefinition _statDefinition;

        public NPCType NpcType => _npcType;
        public BaseNPCActionSelector Selector => _selector;
        public NPCStatDefinition StatDefinition => _statDefinition;
    }
}
