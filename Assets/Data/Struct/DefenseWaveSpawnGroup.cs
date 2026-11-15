public readonly struct DefenseWaveSpawnGroup
{
    public int RecordNumber { get; }
    public int Wave { get; }
    public int Order { get; }
    public string EnemyId { get; }
    public int Count { get; }
    public float SpawnIntervalSeconds { get; }
    public WorkerNPC Prefab { get; }
    public NPCStatDefinition StatDefinition { get; }
    public string SourceKey => RecordNumber > 0
        ? $"CSV record {RecordNumber} (wave={Wave}, order={Order}, enemyId='{EnemyId}')"
        : $"Legacy wave {Wave}";

    public DefenseWaveSpawnGroup(int recordNumber, int wave, int order, string enemyId, int count,
        float spawnIntervalSeconds, WorkerNPC prefab, NPCStatDefinition statDefinition)
    {
        RecordNumber = recordNumber;
        Wave = wave;
        Order = order;
        EnemyId = enemyId;
        Count = count;
        SpawnIntervalSeconds = spawnIntervalSeconds;
        Prefab = prefab;
        StatDefinition = statDefinition;
    }
}
