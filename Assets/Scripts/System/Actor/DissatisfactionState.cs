using System;
using System.Collections.Generic;

public sealed class DissatisfactionState
{
    private sealed class CauseState
    {
        public DissatisfactionCause Cause;
        public bool IsActive;
        public double Amount;
    }

    private readonly List<CauseState> _causes = new List<CauseState>();
    private readonly DissatisfactionSettings _settings;
    private double _total;

    public DissatisfactionSettings Settings => _settings;
    public float Current => (float)_total;
    public float Maximum => _settings.Maximum;
    public bool IsOnStrike => _total >= _settings.StrikeThreshold;

    public DissatisfactionState(DissatisfactionSettings settings)
    {
        if (!settings.IsValid)
            throw new ArgumentException("Dissatisfaction settings must have finite nonnegative rates and 0 < threshold <= maximum.", nameof(settings));
        _settings = settings;
    }

    public bool TrySetCauseActive(DissatisfactionCause cause, bool active)
    {
        if (cause == DissatisfactionCause.None || !Enum.IsDefined(typeof(DissatisfactionCause), cause))
            return false;
        foreach (CauseState entry in _causes)
        {
            if (entry.Cause != cause)
                continue;
            entry.IsActive = active;
            return true;
        }
        if (active)
            _causes.Add(new CauseState { Cause = cause, IsActive = true });
        return true;
    }

    public float GetContribution(DissatisfactionCause cause)
    {
        foreach (CauseState entry in _causes)
            if (entry.Cause == cause)
                return (float)entry.Amount;
        return 0f;
    }

    public void Tick(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f)
            return;

        double recoveredTotal = 0d;
        int activeCount = 0;
        for (int i = _causes.Count - 1; i >= 0; --i)
        {
            CauseState cause = _causes[i];
            if (cause.IsActive)
                ++activeCount;
            else
                cause.Amount = Math.Max(0d, cause.Amount - (double)_settings.DecreasePerSecond * seconds);
            recoveredTotal += cause.Amount;
            if (!cause.IsActive && cause.Amount == 0d)
                _causes.RemoveAt(i);
        }

        // Recovery frees space first; each active cause receives the same capped increment.
        double increment = activeCount == 0 ? 0d : Math.Min(
            (double)_settings.IncreasePerSecond * seconds,
            Math.Max(0d, _settings.Maximum - recoveredTotal) / activeCount);
        foreach (CauseState cause in _causes)
            if (cause.IsActive)
                cause.Amount += increment;
        _total = Math.Min(_settings.Maximum, recoveredTotal + increment * activeCount);
    }

    public void Reset()
    {
        _causes.Clear();
        _total = 0d;
    }
}
