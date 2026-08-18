using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [SerializeField] DataManager dataManager;
    
    [SerializeField] WorkerPool _workerPool;
    [SerializeField] List<BaseNPCActionSelector> _selectors;
    
    private Dictionary<NPCType, List<WorkerNPC>> _workers;

    void Awake()
    {
        _workers = new Dictionary<NPCType, List<WorkerNPC>>();
    }

    public void CreateNPC(NPCType npcType)
    {
        int selectorIndex = (int)npcType;
        if (_selectors == null || selectorIndex < 0 || selectorIndex >= _selectors.Count || !_selectors[selectorIndex])
        {
            Debug.LogError($"NPCManager has no selector registered for {npcType}");
            return;
        }

        if(!_workers.ContainsKey(npcType))
            _workers.Add(npcType, new List<WorkerNPC>());

        // TODO: NPC 다양화 시 npcType별로 다른 워커 풀/프리팹을 선택하도록 확장.
        WorkerNPC newWorker = _workerPool.GetWorker(Vector2.zero);

        var newStat = dataManager.GetStat();
        newWorker.Init(npcType, newStat, _selectors[selectorIndex]);
    }
}
