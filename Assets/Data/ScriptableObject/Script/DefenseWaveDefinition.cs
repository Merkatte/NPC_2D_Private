using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Defense/Wave")]
public sealed class DefenseWaveDefinition : ScriptableObject
{
    [SerializeField, Min(1)] private int _enemyCount = 5;
    [SerializeField, Min(0.01f)] private float _spawnIntervalSeconds = 1f;

    public int EnemyCount => _enemyCount;
    public float SpawnIntervalSeconds => _spawnIntervalSeconds;
    public bool IsValid => _enemyCount > 0 && _spawnIntervalSeconds > 0f &&
        !float.IsInfinity(_spawnIntervalSeconds);

    private void OnValidate()
    {
        _enemyCount = Mathf.Max(1, _enemyCount);
        _spawnIntervalSeconds = float.IsNaN(_spawnIntervalSeconds) || float.IsInfinity(_spawnIntervalSeconds)
            ? 1f : Mathf.Max(0.01f, _spawnIntervalSeconds);
    }
}
