using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [SerializeField] private WorkerPool _workerPool;
    [SerializeField] private List<NPCCreationEntry> _creationEntries;

    private Dictionary<NPCType, List<WorkerNPC>> _workers;
    private Dictionary<NPCType, NPCCreationEntry> _entryLookup;

    void Awake()
    {
        _workers = new Dictionary<NPCType, List<WorkerNPC>>();
        _entryLookup = new Dictionary<NPCType, NPCCreationEntry>();

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
        if (!_entryLookup.TryGetValue(npcType, out NPCCreationEntry entry))
        {
            Debug.LogError($"NPCManager has no creation entry registered for {npcType}");
            return;
        }

        if (!_workerPool)
        {
            Debug.LogError("NPCManager has no WorkerPool assigned.");
            return;
        }

        NPCStat stat = entry.StatDefinition.CreateRuntimeStat();
        if (stat == null || !entry.Selector.CanUseStat(stat))
        {
            Debug.LogError($"NPCManager creation entry for {npcType} has an incompatible stat definition.");
            return;
        }

        WorkerNPC worker = _workerPool.GetWorker(Vector2.zero);
        if (!worker)
        {
            Debug.LogError($"WorkerPool could not provide a worker for {npcType}.");
            return;
        }

        worker.Init(npcType, stat, entry.Selector);

        if (!_workers.ContainsKey(npcType))
            _workers.Add(npcType, new List<WorkerNPC>());

        _workers[npcType].Add(worker);
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
