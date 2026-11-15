public sealed class DefenseWavePlan
{
    private readonly DefenseWaveSpawnGroup[] _groups;

    public int GroupCount => _groups.Length;
    public int EnemyCount { get; }

    public DefenseWavePlan(DefenseWaveSpawnGroup[] groups, int enemyCount)
    {
        _groups = (DefenseWaveSpawnGroup[])groups.Clone();
        EnemyCount = enemyCount;
    }

    public DefenseWaveSpawnGroup GetGroup(int index) => _groups[index];
}
