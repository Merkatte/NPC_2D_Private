using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [SerializeField] WorkerPool _workerPool;
    [SerializeField] List<BaseNPCActionSelector> _selectors;
    [SerializeField] private DefaultStatContext _defaultStatContext;
    
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

        var newStat = DataManager.instance.GetStat();
        newWorker.Init(newStat, _selectors[(int)npcType]);
    }

    NPCStat CreateStat()
    {
        if (_defaultStatContext)
            return _defaultStatContext.CreateStat();

        return new NPCStat("something", 100, 100, UnityEngine.Random.Range(1f, 2f));
    }
}
