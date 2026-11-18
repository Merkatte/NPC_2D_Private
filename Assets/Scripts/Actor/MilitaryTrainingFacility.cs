using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MilitaryTrainingFacility : MonoBehaviour, IClickPopupSource
{
    [SerializeField] private MilitaryTrainingSettings _settings;
    [SerializeField] private Transform _trainingPoint;

    private DefenseBattlefield _battlefield;
    private MilitaryTrainingCandidate _trainee;
    private bool _hasArrived;
    private double _elapsedSeconds;
    private float _awardedExperience;

    internal Vector3 TrainingPosition => _trainingPoint.position;
    internal int TrainingLevelCap => _settings.TrainingLevelCap;

    public void Configure(DefenseBattlefield battlefield)
    {
        if (_battlefield != battlefield) CancelTraining();
        _battlefield = battlefield;
        if (!battlefield)
            Debug.LogError($"MilitaryTrainingFacility '{name}' requires a battlefield.", this);
    }

    private void Awake()
    {
        if (_settings && _trainingPoint) return;
        Debug.LogError($"MilitaryTrainingFacility '{name}' requires _settings and _trainingPoint.", this);
        enabled = false;
    }

    public bool TryGetClickPopup(out PopupType popupType)
    {
        bool available = isActiveAndEnabled && _battlefield && _settings && _trainingPoint;
        popupType = available ? PopupType.MilitaryTraining : PopupType.None;
        return available;
    }

    public void CollectCandidates(List<MilitaryTrainingCandidate> candidates)
    {
        if (candidates == null || !TryGetClickPopup(out _) || _trainee.Actor) return;
        IReadOnlyList<DefenseActor> actors = _battlefield.Actors;
        for (int i = 0; i < actors.Count; i++)
            if (TryGetCandidate(actors[i], out MilitaryTrainingCandidate candidate))
                candidates.Add(candidate);
    }

    private bool TryGetCandidate(DefenseActor actor, out MilitaryTrainingCandidate candidate)
    {
        candidate = default;
        if (!actor || actor.Battlefield != _battlefield || !actor.isActiveAndEnabled ||
            !actor.IsSoldier || !actor.CanAct || !actor.Worker || actor.TrainingFacility) return false;
        NPCProfessionGrowth growth = actor.Worker.ProfessionGrowth;
        if (growth == null || !growth.TryGetProgress(actor.Role, out ProfessionProgress progress)) return false;
        candidate = new MilitaryTrainingCandidate(actor, progress.Level, Math.Min(progress.LevelCap, _settings.TrainingLevelCap));
        return true;
    }

    public bool TryAssign(MilitaryTrainingCandidate candidate)
    {
        DefenseActor actor = candidate.Actor;
        if (!TryGetClickPopup(out _) || _trainee.Actor ||
            !TryGetCandidate(actor, out MilitaryTrainingCandidate current) || !current.CanAssign ||
            current.LifetimeVersion != candidate.LifetimeVersion || current.Profession != candidate.Profession)
            return false;

        _trainee = current;
        actor.AssignTrainingFacility(this);
        return true;
    }

    public bool TryGetSession(out MilitaryTrainingCandidate trainee, out bool arrived, out float remainingSeconds)
    {
        trainee = _trainee;
        arrived = _hasArrived;
        remainingSeconds = trainee.Actor ? (float)Math.Max(0d, _settings.DurationSeconds - _elapsedSeconds) : 0f;
        return trainee.Actor;
    }

    internal bool CanTrain(DefenseActor actor)
    {
        if (!TryGetClickPopup(out _) || !actor || actor != _trainee.Actor || !actor.CanAct ||
            !actor.Worker || actor.Worker.LifetimeVersion != _trainee.LifetimeVersion) return false;
        NPCProfessionGrowth growth = actor.Worker.ProfessionGrowth;
        return growth != null && growth.TryGetProgress(_trainee.Profession, out ProfessionProgress progress) &&
            !progress.IsAtCap && progress.Level < _settings.TrainingLevelCap;
    }

    internal bool TryArrive(DefenseActor actor)
    {
        if (!CanTrain(actor)) return false;
        _hasArrived = true;
        return true;
    }

    internal bool TryAdvanceTraining(DefenseActor actor, float seconds, out double experience, out bool finished)
    {
        experience = 0d;
        finished = false;
        if (!_hasArrived || !CanTrain(actor) || Time.timeScale <= 0f ||
            seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;

        _elapsedSeconds = Math.Min(_settings.DurationSeconds, _elapsedSeconds + seconds);
        // Difference of cumulative amounts preserves the full session payout across frame sizes.
        float earned = (float)(_settings.ProficiencyGain * _elapsedSeconds / _settings.DurationSeconds);
        experience = (double)earned - _awardedExperience;
        _awardedExperience = earned;
        finished = _elapsedSeconds >= _settings.DurationSeconds;
        return true;
    }

    internal void CompleteTraining(DefenseActor actor)
    {
        if (!_hasArrived || actor != _trainee.Actor) return;
        string traineeLabel = _trainee.DisplayName;
        Release(actor);
        _battlefield.NotifyTrainingCompleted(traineeLabel);
    }

    internal void Release(DefenseActor actor)
    {
        if (actor != _trainee.Actor) return;
        _trainee = default;
        _hasArrived = false;
        _elapsedSeconds = 0d;
        _awardedExperience = 0f;
        if (actor) actor.ReleaseTrainingFacility(this);
    }

    public void CancelTraining()
    {
        if (_trainee.Actor) _trainee.Actor.CancelTrainingAssignment();
    }

    private void OnDisable() => CancelTraining();
}
