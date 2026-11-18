using UnityEngine;

public sealed class MilitaryTrainingAction : DefaultAction
{
    private DefenseActor _actor;
    private MilitaryTrainingFacility _facility;

    public MilitaryTrainingAction() : base(ActionType.MilitaryTraining) { }

    internal void Init(ActionContext context, DefenseActor actor, MilitaryTrainingFacility facility)
    {
        base.Init(context);
        _actor = actor;
        _facility = facility;
    }

    public override void Start()
    {
        base.Start();
        if (!IsFinished && (!_facility || !_facility.TryArrive(_actor))) RequestReplan();
    }

    public override void Tick()
    {
        if (!_isRunning || _isPaused || IsFinished || Time.deltaTime <= 0f) return;
        if (!_facility || !_facility.TryAdvanceTraining(_actor, Time.deltaTime, out double experience, out bool finished))
        {
            RequestReplan();
            return;
        }
        bool belowCap = actionContext.Stat.ProfessionGrowth.AwardTrainingExperience(_actor.Role,
            experience, _facility.TrainingLevelCap);
        if (finished || !belowCap) Complete();
    }

    public override void Clear()
    {
        // A failed approach also returns the queued, still-unstarted training action.
        if (_facility) _facility.Release(_actor);
        _actor = null;
        _facility = null;
        base.Clear();
    }
}
