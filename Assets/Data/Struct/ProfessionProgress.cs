public readonly struct ProfessionProgress
{
    public int Level { get; }
    public float Experience { get; }
    public int LevelCap { get; }
    public float RequiredExperience { get; }
    public bool IsAtCap => Level >= LevelCap;

    public ProfessionProgress(int level, float experience, int levelCap, float requiredExperience)
    {
        Level = level;
        Experience = experience;
        LevelCap = levelCap;
        RequiredExperience = requiredExperience;
    }
}
