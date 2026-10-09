public static class BuilderWorkPolicy
{
    // Eligibility is independent of free reservation slots: an active builder owns its slot.
    public static bool CanWorkOn(NPCStat stat, BuildingPlot plot)
    {
        if (stat == null || !plot)
            return false;
        if (!stat.IsOnStrike)
            return true;

        return !plot.IsUpgrade && plot.Definition != null
            && plot.Definition.BuildingType == BuildingType.House
            && !stat.Dissatisfaction.IsOnStrikeExcluding(DissatisfactionCause.Homeless);
    }
}
