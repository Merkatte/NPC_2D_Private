using UnityEngine;
using UnityEngine.Pool;

public class WorkerPool : MonoBehaviour
{
    [SerializeField] private WorkerNPC _workerPrefab;
    [SerializeField] private Transform _activeParent;
    [SerializeField] private Transform _pooledParent;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 100;
    [SerializeField] private bool _collectionCheck = true;

    private ObjectPool<WorkerNPC> _pool;

    private Transform ActiveParent => _activeParent ? _activeParent : transform;
    private Transform PooledParent => _pooledParent ? _pooledParent : transform;

    private void Awake()
    {
        _pool = new ObjectPool<WorkerNPC>(
            CreateWorker,
            OnGetWorker,
            OnReleaseWorker,
            OnDestroyWorker,
            _collectionCheck,
            _defaultCapacity,
            _maxSize);
    }

    public WorkerNPC GetWorker()
    {
        return _pool.Get();
    }

    public WorkerNPC GetWorker(Vector3 position)
    {
        WorkerNPC worker = GetWorker();
        worker.transform.position = position;
        return worker;
    }

    public WorkerNPC GetWorker(Vector3 position, Quaternion rotation)
    {
        WorkerNPC worker = GetWorker();
        worker.transform.SetPositionAndRotation(position, rotation);
        return worker;
    }

    public void ReleaseWorker(WorkerNPC worker)
    {
        if (!worker)
        {
            return;
        }

        _pool.Release(worker);
    }

    public void ClearPool()
    {
        _pool.Clear();
    }

    private WorkerNPC CreateWorker()
    {
        WorkerNPC worker = Instantiate(_workerPrefab, PooledParent);
        worker.gameObject.SetActive(false);
        return worker;
    }

    private void OnGetWorker(WorkerNPC worker)
    {
        worker.transform.SetParent(ActiveParent);
        worker.gameObject.SetActive(true);
    }

    private void OnReleaseWorker(WorkerNPC worker)
    {
        worker.gameObject.SetActive(false);
        worker.transform.SetParent(PooledParent);
    }

    private void OnDestroyWorker(WorkerNPC worker)
    {
        if (!worker)
        {
            return;
        }

        Destroy(worker.gameObject);
    }
}
