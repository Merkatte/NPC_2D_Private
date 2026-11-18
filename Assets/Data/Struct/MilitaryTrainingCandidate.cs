public readonly struct MilitaryTrainingCandidate
{
    internal DefenseActor Actor { get; }
    internal uint LifetimeVersion { get; }
    public string DisplayName { get; }
    public NPCType Profession { get; }
    public int Level { get; }
    public int TrainingLevelCap { get; }
    public bool CanAssign => Actor && Level < TrainingLevelCap;

    internal MilitaryTrainingCandidate(DefenseActor actor, int level, int trainingLevelCap)
    {
        Actor = actor;
        LifetimeVersion = actor.Worker.LifetimeVersion;
        DisplayName = actor.ResidentLabel;
        Profession = actor.Role;
        Level = level;
        TrainingLevelCap = trainingLevelCap;
    }
}
