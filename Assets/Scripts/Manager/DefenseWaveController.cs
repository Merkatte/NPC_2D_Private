using System;
using UnityEngine;

public sealed class DefenseWaveController : MonoBehaviour
{
    [SerializeField] private DefenseGameSession _session;
    [SerializeField] private DefenseBattlefield _battlefield;
    [SerializeField] private DefenseWaveCatalog _catalog;
    [SerializeField] private WorkerNPC _enemyPrefab;
    [SerializeField] private NPCStatDefinition _enemyStatDefinition;
    [SerializeField] private BaseNPCActionSelector _enemySelector;
    [SerializeField] private Transform[] _spawnPoints;

    private DefenseWaveDefinition _currentDefinition;
    private float _spawnElapsed;
    private int _spawnedCount;
    private string _failureReason;

    public bool IsInitialized { get; private set; }
    public bool IsResting { get; private set; }
    public bool HasFailed => !string.IsNullOrEmpty(_failureReason);
    public string FailureReason => _failureReason;
    public int WaveNumber { get; private set; }
    public int NextWaveNumber => WaveNumber + 1;
    public float RestElapsedSeconds { get; private set; }
    public float RestRemainingSeconds => _catalog
        ? Mathf.Max(0f, _catalog.RestDurationSeconds - RestElapsedSeconds) : 0f;
    public int PendingSpawnCount => _currentDefinition
        ? Mathf.Max(0, _currentDefinition.EnemyCount - _spawnedCount) : 0;
    public int AliveEnemyCount => _battlefield ? _battlefield.AliveEnemyCount : 0;
    public bool CanStartNextWave => isActiveAndEnabled && IsInitialized && IsResting && !HasFailed &&
        _session && _session.isActiveAndEnabled && !_session.IsPaused && Time.timeScale > 0f;
    public event Action Changed;

    public void Configure(DefenseGameSession session, DefenseBattlefield battlefield, DefenseWaveCatalog catalog,
        WorkerNPC enemyPrefab, NPCStatDefinition enemyStatDefinition, BaseNPCActionSelector enemySelector,
        Transform[] spawnPoints)
    {
        if (Application.isPlaying && IsInitialized)
            throw new InvalidOperationException("DefenseWaveController cannot be reconfigured during a session.");
        _session = session;
        _battlefield = battlefield;
        _catalog = catalog;
        _enemyPrefab = enemyPrefab;
        _enemyStatDefinition = enemyStatDefinition;
        _enemySelector = enemySelector;
        _spawnPoints = spawnPoints;
    }

    private void Start()
    {
        if (!_session || !_battlefield || !_catalog || !_catalog.IsValid || !_enemyPrefab ||
            !_enemyStatDefinition || !_enemySelector || !_enemyPrefab.Component ||
            !_enemyPrefab.TryGetComponent(out DefenseActor _) || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            Fail("Missing or invalid session, battlefield, catalog, enemy prefab/component, stat, selector or spawn points.");
            return;
        }
        foreach (Transform point in _spawnPoints)
        {
            if (point) continue;
            Fail("Spawn points contain a missing Transform.");
            return;
        }
        IsInitialized = true;
        BeginRest();
    }

    private void Update()
    {
        if (!IsInitialized || HasFailed) return;
        if (!_session || !_battlefield || !_enemySelector || !_enemyStatDefinition)
        {
            Fail("A required runtime dependency was destroyed.");
            return;
        }
        if (!_session.isActiveAndEnabled || _session.IsPaused || Time.timeScale <= 0f) return;
        if (IsResting)
        {
            RestElapsedSeconds += Time.deltaTime;
            if (RestRemainingSeconds <= 0f) TryStartNextWave();
            return;
        }
        if (PendingSpawnCount > 0)
        {
            _spawnElapsed += Time.deltaTime;
            if (_spawnElapsed >= _currentDefinition.SpawnIntervalSeconds)
            {
                _spawnElapsed -= _currentDefinition.SpawnIntervalSeconds;
                if (!TrySpawnEnemy()) return;
                ++_spawnedCount;
                Changed?.Invoke();
            }
        }
        // Never infer wave completion from an empty battlefield while spawns remain.
        if (PendingSpawnCount == 0 && _battlefield.AliveEnemyCount == 0) BeginRest();
    }

    public bool TryStartNextWave()
    {
        if (!CanStartNextWave) return false;
        if (!_catalog || !_catalog.TryGetWave(NextWaveNumber, out DefenseWaveDefinition definition))
        {
            Fail("Wave catalog has no valid next formation.");
            return false;
        }
        _currentDefinition = definition;
        ++WaveNumber;
        IsResting = false;
        _spawnedCount = 0;
        _spawnElapsed = definition.SpawnIntervalSeconds;
        Changed?.Invoke();
        return true;
    }

    private bool TrySpawnEnemy()
    {
        WorkerNPC worker = null;
        try
        {
            NPCStat stat = _enemyStatDefinition.CreateRuntimeStat();
            if (stat == null || stat.CurrentHealth <= 0f || !_enemySelector.CanUseStat(stat))
            {
                Fail("Enemy stat definition is dead or incompatible with the scene selector.");
                return false;
            }
            Transform point = _spawnPoints[_spawnedCount % _spawnPoints.Length];
            if (!point || !_enemyPrefab)
            {
                Fail("Enemy prefab or selected spawn point was destroyed.");
                return false;
            }
            worker = Instantiate(_enemyPrefab, point.position, point.rotation);
            if (!worker.TryGetComponent(out DefenseActor actor) || !worker.Component)
                throw new InvalidOperationException("Spawned enemy lacks DefenseActor or NPCComponent.");
            actor.BindBattlefield(_battlefield);
            worker.gameObject.SetActive(true);
            worker.Init(NPCType.Enemy, stat, _enemySelector);
            if (!worker.TryGetCurrentState(out IStatView _, out ActionType? _) || !IsRegistered(actor))
                throw new InvalidOperationException("Spawned enemy did not initialize and register with its battlefield.");
            return true;
        }
        catch (Exception exception)
        {
            if (worker)
            {
                worker.gameObject.SetActive(false);
                Destroy(worker.gameObject);
            }
            Fail($"Enemy initialization failed: {exception.Message}");
            return false;
        }
    }

    private bool IsRegistered(DefenseActor actor)
    {
        foreach (DefenseActor registered in _battlefield.Actors)
            if (registered == actor) return true;
        return false;
    }

    private void BeginRest()
    {
        IsResting = true;
        RestElapsedSeconds = 0f;
        _currentDefinition = null;
        Changed?.Invoke();
    }

    private void Fail(string reason)
    {
        if (HasFailed) return;
        _failureReason = reason;
        Debug.LogError($"DefenseWaveController '{name}': {reason}", this);
        Changed?.Invoke();
    }
}
