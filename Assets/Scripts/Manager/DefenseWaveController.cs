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

    private DefenseWavePlan[] _waves;
    private DefenseWavePlan _currentPlan;
    private float _restDurationSeconds;
    private float _spawnElapsed;
    private int _spawnedCount;
    private int _groupIndex;
    private int _spawnedInGroup;
    private string _failureReason;

    public bool IsInitialized { get; private set; }
    public bool IsResting { get; private set; }
    public bool HasFailed => !string.IsNullOrEmpty(_failureReason);
    public string FailureReason => _failureReason;
    public int WaveNumber { get; private set; }
    public int NextWaveNumber => WaveNumber + 1;
    public float RestElapsedSeconds { get; private set; }
    public float RestRemainingSeconds => Mathf.Max(0f, _restDurationSeconds - RestElapsedSeconds);
    public int PendingSpawnCount => _currentPlan != null
        ? Mathf.Max(0, _currentPlan.EnemyCount - _spawnedCount) : 0;
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
        if (!_session || !_battlefield || !_catalog || !_enemySelector ||
            _spawnPoints == null || _spawnPoints.Length == 0)
        {
            Fail("Missing session, battlefield, catalog, selector or spawn points.");
            return;
        }
        foreach (Transform point in _spawnPoints)
        {
            if (point) continue;
            Fail("Spawn points contain a missing Transform.");
            return;
        }
        if (!_catalog.TryCreateSnapshot(_enemyPrefab, _enemyStatDefinition,
            out DefenseWavePlan[] waves, out string error))
        {
            Fail(error);
            return;
        }
        // Validate the complete plan before accepting any wave, including later formations.
        foreach (DefenseWavePlan wave in waves)
        {
            for (int index = 0; index < wave.GroupCount; ++index)
            {
                DefenseWaveSpawnGroup group = wave.GetGroup(index);
                if (!TryValidateGroup(group, out error))
                {
                    Fail($"{group.SourceKey}: {error}");
                    return;
                }
            }
        }
        _waves = waves;
        _restDurationSeconds = _catalog.RestDurationSeconds;
        IsInitialized = true;
        BeginRest();
    }

    private void Update()
    {
        if (!IsInitialized || HasFailed) return;
        if (!_session || !_battlefield || !_enemySelector)
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
            DefenseWaveSpawnGroup group = _currentPlan.GetGroup(_groupIndex);
            _spawnElapsed += Time.deltaTime;
            if (_spawnElapsed >= group.SpawnIntervalSeconds)
            {
                _spawnElapsed -= group.SpawnIntervalSeconds;
                if (!TrySpawnEnemy(group)) return;
                ++_spawnedCount;
                if (++_spawnedInGroup == group.Count)
                {
                    ++_groupIndex;
                    _spawnedInGroup = 0;
                }
                Changed?.Invoke();
            }
        }
        // Never infer wave completion from an empty battlefield while spawns remain.
        if (PendingSpawnCount == 0 && _battlefield.AliveEnemyCount == 0) BeginRest();
    }

    public bool TryStartNextWave()
    {
        if (!CanStartNextWave) return false;
        if (WaveNumber == int.MaxValue)
        {
            Fail("Wave number exceeds Int32 capacity.");
            return false;
        }
        _currentPlan = _waves[Mathf.Min(WaveNumber, _waves.Length - 1)];
        ++WaveNumber;
        IsResting = false;
        _spawnedCount = 0;
        _groupIndex = 0;
        _spawnedInGroup = 0;
        // First spawn is ready on the next Update. Later waits use the incoming group's interval.
        _spawnElapsed = _currentPlan.GetGroup(0).SpawnIntervalSeconds;
        Changed?.Invoke();
        return true;
    }

    private bool TryValidateGroup(DefenseWaveSpawnGroup group, out string error)
    {
        error = "Missing enemy prefab/NPCComponent/DefenseActor or stat definition.";
        if (!group.Prefab || !group.Prefab.Component ||
            !group.Prefab.TryGetComponent(out DefenseActor _) || !group.StatDefinition) return false;
        try
        {
            NPCStat stat = group.StatDefinition.CreateRuntimeStat();
            error = "Enemy stat definition is dead or incompatible with the scene selector.";
            if (stat == null || !(stat.CurrentHealth > 0f) || !_enemySelector.CanUseStat(stat)) return false;
        }
        catch (Exception exception)
        {
            error = $"Enemy stat validation failed: {exception.Message}";
            return false;
        }
        error = null;
        return true;
    }

    private bool TrySpawnEnemy(DefenseWaveSpawnGroup group)
    {
        WorkerNPC worker = null;
        try
        {
            if (!group.StatDefinition)
            {
                Fail($"{group.SourceKey}: enemy stat definition was destroyed.");
                return false;
            }
            NPCStat stat = group.StatDefinition.CreateRuntimeStat();
            if (stat == null || !(stat.CurrentHealth > 0f) || !_enemySelector.CanUseStat(stat))
            {
                Fail($"{group.SourceKey}: enemy stat definition is dead or incompatible with the scene selector.");
                return false;
            }
            Transform point = _spawnPoints[_spawnedCount % _spawnPoints.Length];
            if (!point || !group.Prefab)
            {
                Fail($"{group.SourceKey}: enemy prefab or selected spawn point was destroyed.");
                return false;
            }
            worker = Instantiate(group.Prefab, point.position, point.rotation);
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
            Fail($"{group.SourceKey}: enemy initialization failed: {exception.Message}");
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
        _currentPlan = null;
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
