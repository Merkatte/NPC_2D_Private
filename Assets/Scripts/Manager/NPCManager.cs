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
        if(!_workers.ContainsKey(npcType))
            _workers.Add(npcType, new List<WorkerNPC>());
        
        //추후 NPC 다양화 시
        // switch (npcType)
        // {
        //     case NPCType.Farmer:
        //         newWorker = _workerPool.GetWorker(npcType);
        //         break;
        //     default:
        //         return;
        // }
        WorkerNPC newWorker = _workerPool.GetWorker(Vector2.zero);

        var newStat = dataManager.GetStat();
        newWorker.Init(newStat, _selectors[(int)npcType]);
    }
}
