using UnityEngine;

public sealed class ResidentHousingState
{
    public WorkerNPC Worker { get; }
    public NPCStat Stat { get; }
    public NPCComponent Component { get; }
    public HousingLifeSettings LifeSettings { get; }
    public INavigationService Navigation { get; }
    public House Home { get; private set; }
    public bool IsInsideHome { get; private set; }
    public bool RequiresFirstHomeVisit { get; private set; }
    public long RegistrationOrder { get; }
    public bool IsRegistered { get; private set; } = true;

    public ResidentHousingState(WorkerNPC worker, NPCStat stat, NPCComponent component,
        HousingLifeSettings life, INavigationService navigation, long order)
    {
        Worker = worker;
        Stat = stat;
        Component = component;
        LifeSettings = life;
        Navigation = navigation;
        RegistrationOrder = order;
        Stat.Dissatisfaction.TrySetCauseActive(DissatisfactionCause.Homeless, true);
    }

    internal void AssignHome(House home)
    {
        Home = home;
        IsInsideHome = false;
        RequiresFirstHomeVisit = home;
        Stat.Dissatisfaction.TrySetCauseActive(DissatisfactionCause.Homeless, !home);
    }

    public bool TryEnterHome(House home)
    {
        if (!IsRegistered || !home || Home != home || !home.isActiveAndEnabled) return false;
        IsInsideHome = true;
        RequiresFirstHomeVisit = false;
        return true;
    }

    public void ExitHome() => IsInsideHome = false;

    internal void Unregister()
    {
        IsRegistered = false;
        Home = null;
        IsInsideHome = false;
        RequiresFirstHomeVisit = false;
        Stat.Dissatisfaction.TrySetCauseActive(DissatisfactionCause.Homeless, false);
    }

    public float DissatisfactionRecoveryBonus => Home && Home.isActiveAndEnabled
        ? Home.Tier.GetEffect(HousingEffectType.DissatisfactionRecovery) : 0f;
}
