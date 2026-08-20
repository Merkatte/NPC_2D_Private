using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Development-only enemy stream used to exercise Guard combat in test scenes.
/// A scene Enemy acts as the visual and collision template; spawned copies move downward.
/// </summary>
public class TestEnemyRainSpawner : MonoBehaviour
{
    private const float WindowWidth = 260f;
    private const float WindowHeight = 310f;
    private const float MinimumSpawnInterval = 0.1f;
    private const float MaximumSpawnInterval = 5f;
    private const float MaximumFallSpeed = 10f;
    private const float MaximumSpawnWidth = 30f;
    private const float MinimumSpawnY = -10f;
    private const float MaximumSpawnY = 30f;
    private const int MaximumEnemyLimit = 100;

    [SerializeField] private Enemy _enemyTemplate;
    [SerializeField] private bool _autoSpawn = true;
    [SerializeField] private float _spawnInterval = 1f;
    [SerializeField] private float _fallSpeed = 1.5f;
    [SerializeField] private float _spawnCenterX;
    [SerializeField] private float _spawnWidth = 10f;
    [SerializeField] private float _spawnY = 9f;
    [SerializeField] private float _despawnY = -7f;
    [SerializeField] private int _maxActiveEnemies = 30;

    private readonly List<Enemy> spawnedEnemies = new List<Enemy>();
    private Rect windowRect = new Rect(260f, 20f, WindowWidth, WindowHeight);
    private float spawnTimer;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForEnemyTestScene()
    {
        if (FindFirstObjectByType<TestEnemyRainSpawner>())
        {
            return;
        }

        Enemy template = FindFirstObjectByType<Enemy>();
        if (!template)
        {
            return;
        }

        GameObject host = new GameObject("[TestOnly] Enemy Rain Spawner");
        TestEnemyRainSpawner spawner = host.AddComponent<TestEnemyRainSpawner>();
        spawner.SetTemplate(template);
    }
#endif

    private void Awake()
    {
        if (!_enemyTemplate)
        {
            SetTemplate(FindFirstObjectByType<Enemy>());
        }

        ClampSettings();
    }

    private void Update()
    {
        MoveAndRemoveSpawnedEnemies();

        if (!_autoSpawn)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer = _spawnInterval;
        SpawnEnemy();
    }

    private void OnGUI()
    {
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "Test Enemy Rain");
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
        GUILayout.Label(_enemyTemplate ? $"Template: {_enemyTemplate.name}" : "Template: Missing Enemy");
        _autoSpawn = GUILayout.Toggle(_autoSpawn, "Auto Spawn");

        _spawnInterval = DrawSlider("Interval", _spawnInterval, MinimumSpawnInterval, MaximumSpawnInterval, "0.0 s");
        _fallSpeed = DrawSlider("Fall Speed", _fallSpeed, 0f, MaximumFallSpeed, "0.0");
        _spawnCenterX = DrawSlider("Center X", _spawnCenterX, -15f, 15f, "0.0");
        _spawnWidth = DrawSlider("Spawn Width", _spawnWidth, 0f, MaximumSpawnWidth, "0.0");
        _spawnY = DrawSlider("Spawn Y", _spawnY, MinimumSpawnY, MaximumSpawnY, "0.0");

        GUILayout.Label($"Max Enemies: {_maxActiveEnemies}");
        _maxActiveEnemies = Mathf.RoundToInt(GUILayout.HorizontalSlider(_maxActiveEnemies, 1, MaximumEnemyLimit));
        GUILayout.Label($"Active: {spawnedEnemies.Count}");

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
        RemoveMissingEnemies();
        if (!_enemyTemplate || spawnedEnemies.Count >= _maxActiveEnemies)
        {
            return;
        }

        float halfWidth = _spawnWidth * 0.5f;
        float spawnX = Random.Range(_spawnCenterX - halfWidth, _spawnCenterX + halfWidth);
        Vector3 spawnPosition = new Vector3(spawnX, _spawnY, _enemyTemplate.transform.position.z);
        Enemy enemy = Instantiate(_enemyTemplate, spawnPosition, _enemyTemplate.transform.rotation, transform);
        enemy.name = $"{_enemyTemplate.name} (Rain)";
        spawnedEnemies.Add(enemy);
    }

    private void MoveAndRemoveSpawnedEnemies()
    {
        float movement = _fallSpeed * Time.deltaTime;
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = spawnedEnemies[i];
            if (!enemy || !enemy.IsAlive)
            {
                if (enemy)
                {
                    Destroy(enemy.gameObject);
                }

                spawnedEnemies.RemoveAt(i);
                continue;
            }

            enemy.transform.position += Vector3.down * movement;
            if (enemy.transform.position.y >= _despawnY)
            {
                continue;
            }

            Destroy(enemy.gameObject);
            spawnedEnemies.RemoveAt(i);
        }
    }

    private void RemoveMissingEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (!spawnedEnemies[i])
            {
                spawnedEnemies.RemoveAt(i);
            }
        }
    }

    private void ClearSpawnedEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = spawnedEnemies[i];
            if (enemy)
            {
                Destroy(enemy.gameObject);
            }
        }

        spawnedEnemies.Clear();
    }

    private void SetTemplate(Enemy template)
    {
        if (!template)
        {
            return;
        }

        _enemyTemplate = template;
        _spawnCenterX = template.transform.position.x;
        _spawnY = template.transform.position.y;
    }

    private void ClampSettings()
    {
        _spawnInterval = Mathf.Clamp(_spawnInterval, MinimumSpawnInterval, MaximumSpawnInterval);
        _fallSpeed = Mathf.Clamp(_fallSpeed, 0f, MaximumFallSpeed);
        _spawnWidth = Mathf.Clamp(_spawnWidth, 0f, MaximumSpawnWidth);
        _spawnY = Mathf.Clamp(_spawnY, MinimumSpawnY, MaximumSpawnY);
        _maxActiveEnemies = Mathf.Clamp(_maxActiveEnemies, 1, MaximumEnemyLimit);
    }
}
