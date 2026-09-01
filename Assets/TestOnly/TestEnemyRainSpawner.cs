using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Development-only Enemy spawner used to exercise EnemyActionSelector's melee/ranged combat AI
/// (IMP-035) and Guard-vs-Enemy combat in test scenes. Spawned Enemy actors move themselves via
/// WorkerNPC/NPCComponent - this spawner only places them and hands them a stat + the shared
/// scene EnemyActionSelector; it no longer pushes their position every frame.
/// </summary>
public class TestEnemyRainSpawner : MonoBehaviour
{
    private const float WindowWidth = 260f;
    private const float WindowHeight = 260f;
    private const float MinimumSpawnInterval = 0.1f;
    private const float MaximumSpawnInterval = 5f;
    private const float MaximumSpawnWidth = 30f;
    private const float MinimumSpawnY = -10f;
    private const float MaximumSpawnY = 30f;
    private const int MaximumEnemyLimit = 100;

    [SerializeField] private WorkerNPC _enemyTemplate;
    [SerializeField] private EnemyActionSelector _selector;
    [SerializeField] private EnemyStatDefinition[] _statVariants;
    [SerializeField] private bool _autoSpawn = true;
    [SerializeField] private float _spawnInterval = 1f;
    [SerializeField] private float _spawnCenterX;
    [SerializeField] private float _spawnWidth = 10f;
    [SerializeField] private float _spawnY = 9f;
    [SerializeField] private int _maxActiveEnemies = 30;

    private readonly List<SpawnedEnemyEntry> _spawnedEnemies = new List<SpawnedEnemyEntry>();
    private Rect _windowRect = new Rect(260f, 20f, WindowWidth, WindowHeight);
    private float _spawnTimer;
    private int _nextVariantIndex;
    private bool _hasLoggedMissingSelector;
    private bool _hasLoggedMissingVariants;

    private readonly struct SpawnedEnemyEntry
    {
        public readonly WorkerNPC Worker;
        public readonly CombatTarget CombatTarget;

        public SpawnedEnemyEntry(WorkerNPC worker, CombatTarget combatTarget)
        {
            Worker = worker;
            CombatTarget = combatTarget;
        }
    }

    private void Awake()
    {
        ClampSettings();
    }

    private void Update()
    {
        RemoveDeadOrMissingEnemies();

        if (!_autoSpawn)
        {
            return;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        _spawnTimer = _spawnInterval;
        SpawnEnemy();
    }

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Test Enemy Spawner");
    }

    private void OnDestroy()
    {
        ClearSpawnedEnemies();
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label(_enemyTemplate ? $"Template: {_enemyTemplate.name}" : "Template: Missing WorkerNPC");
        _autoSpawn = GUILayout.Toggle(_autoSpawn, "Auto Spawn");

        _spawnInterval = DrawSlider("Interval", _spawnInterval, MinimumSpawnInterval, MaximumSpawnInterval, "0.0 s");
        _spawnCenterX = DrawSlider("Center X", _spawnCenterX, -15f, 15f, "0.0");
        _spawnWidth = DrawSlider("Spawn Width", _spawnWidth, 0f, MaximumSpawnWidth, "0.0");
        _spawnY = DrawSlider("Spawn Y", _spawnY, MinimumSpawnY, MaximumSpawnY, "0.0");

        GUILayout.Label($"Max Enemies: {_maxActiveEnemies}");
        _maxActiveEnemies = Mathf.RoundToInt(GUILayout.HorizontalSlider(_maxActiveEnemies, 1, MaximumEnemyLimit));
        GUILayout.Label($"Active: {_spawnedEnemies.Count}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn One"))
        {
            SpawnEnemy();
        }

        if (GUILayout.Button("Clear"))
        {
            ClearSpawnedEnemies();
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow();
    }

    private static float DrawSlider(string label, float value, float minimum, float maximum, string format)
    {
        GUILayout.Label($"{label}: {value.ToString(format)}");
        return GUILayout.HorizontalSlider(value, minimum, maximum);
    }

    private void SpawnEnemy()
    {
        if (!_enemyTemplate || _spawnedEnemies.Count >= _maxActiveEnemies)
        {
            return;
        }

        if (_statVariants == null || _statVariants.Length == 0)
        {
            if (!_hasLoggedMissingVariants)
            {
                Debug.LogError("TestEnemyRainSpawner has no EnemyStatDefinition variants assigned; cannot spawn.", this);
                _hasLoggedMissingVariants = true;
            }
            return;
        }

        if (!_selector)
        {
            if (!_hasLoggedMissingSelector)
            {
                Debug.LogError("TestEnemyRainSpawner has no EnemyActionSelector assigned; cannot spawn.", this);
                _hasLoggedMissingSelector = true;
            }
            return;
        }

        EnemyStatDefinition definition = _statVariants[_nextVariantIndex];
        _nextVariantIndex = (_nextVariantIndex + 1) % _statVariants.Length;

        if (!definition)
        {
            Debug.LogError("TestEnemyRainSpawner has a null entry in _statVariants; skipping this spawn.", this);
            return;
        }

        EnemyStat stat = definition.CreateRuntimeStat() as EnemyStat;
        if (stat == null)
        {
            Debug.LogError($"EnemyStatDefinition '{definition.name}' did not produce an EnemyStat; skipping this spawn.", this);
            return;
        }

        if (!_selector.CanUseStat(stat))
        {
            Debug.LogError("EnemyActionSelector rejected the EnemyStat produced by the selected EnemyStatDefinition; skipping this spawn.", this);
            return;
        }

        float halfWidth = _spawnWidth * 0.5f;
        float spawnX = Random.Range(_spawnCenterX - halfWidth, _spawnCenterX + halfWidth);
        Vector3 spawnPosition = new Vector3(spawnX, _spawnY, _enemyTemplate.transform.position.z);

        WorkerNPC worker = Instantiate(_enemyTemplate, spawnPosition, _enemyTemplate.transform.rotation, transform);

        CombatTarget combatTarget = worker.GetComponent<CombatTarget>();
        if (!combatTarget)
        {
            Debug.LogError("Spawned Enemy prefab has no CombatTarget component; destroying it.", this);
            Destroy(worker.gameObject);
            return;
        }

        worker.name = $"{_enemyTemplate.name} ({definition.name})";
        combatTarget.Initialize(stat);
        worker.Init(NPCType.Enemy, stat, _selector);

        _spawnedEnemies.Add(new SpawnedEnemyEntry(worker, combatTarget));
    }

    private void RemoveDeadOrMissingEnemies()
    {
        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            SpawnedEnemyEntry entry = _spawnedEnemies[i];

            // Unity destroyed-object check must come first: calling .IsAlive on an already-
            // destroyed Enemy would throw MissingReferenceException.
            if (!entry.CombatTarget || !entry.CombatTarget.IsAlive)
            {
                if (entry.Worker)
                {
                    Destroy(entry.Worker.gameObject);
                }

                _spawnedEnemies.RemoveAt(i);
            }
        }
    }

    private void ClearSpawnedEnemies()
    {
        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            SpawnedEnemyEntry entry = _spawnedEnemies[i];
            if (entry.Worker)
            {
                Destroy(entry.Worker.gameObject);
            }
        }

        _spawnedEnemies.Clear();
    }

    private void ClampSettings()
    {
        _spawnInterval = Mathf.Clamp(_spawnInterval, MinimumSpawnInterval, MaximumSpawnInterval);
        _spawnWidth = Mathf.Clamp(_spawnWidth, 0f, MaximumSpawnWidth);
        _spawnY = Mathf.Clamp(_spawnY, MinimumSpawnY, MaximumSpawnY);
        _maxActiveEnemies = Mathf.Clamp(_maxActiveEnemies, 1, MaximumEnemyLimit);
    }
}
