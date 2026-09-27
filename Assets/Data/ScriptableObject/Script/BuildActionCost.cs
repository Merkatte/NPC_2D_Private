using UnityEngine;

[CreateAssetMenu(fileName = "BuildActionCost", menuName = "Scriptable Objects/ActionCost/BuildActionCost")]
public sealed class BuildActionCost : DefaultActionCost
{
    [SerializeField] private BuildingDataContext _buildingDataContext;
    [SerializeField] private NPCDecisionTuning _decisionTuning;
    public override ActionType MyType => ActionType.Build;
    public BuilderWorkDefinition Work => _buildingDataContext ? _buildingDataContext.Work : null;
    public bool IsConfigured => Work != null && _decisionTuning;
    public bool ShouldInterrupt(IStatView stat) => !_decisionTuning || _decisionTuning.HasCriticalNeed(stat);
    public void ApplyElapsedNeeds(NPCStat stat, float seconds)
    {
        BuilderWorkDefinition work = Work;
        stat.ChangeHunger(work.HungerPerSecond * seconds);
        stat.ChangeThirst(work.ThirstPerSecond * seconds);
        stat.ChangeFatigue(work.FatiguePerSecond * seconds);
    }
}
