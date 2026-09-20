using System.Collections.Generic;

// Actor-local message lifetime. Time and randomness are explicit inputs so querying
// a message never consumes gameplay time or a shared gameplay random stream.
public sealed class NPCMessageState
{
    private IStatView _stat;
    private uint _lastToken;
    private uint _fixedToken;
    private LocalizeKey _fixedKey;
    private LocalizeKey _thoughtKey;
    private LocalizeKey _previousKey;
    private double _expiresAt;
    private bool _hasThought;

    public void Synchronize(IStatView stat)
    {
        if (ReferenceEquals(_stat, stat))
            return;
        Reset();
        _stat = stat;
    }

    public bool TrySetFixedMessage(LocalizeKey key, out uint token)
    {
        token = 0;
        if (!NPCThoughtSelector.IsValidKey(key) || _lastToken == uint.MaxValue)
            return false;
        token = ++_lastToken;
        _fixedToken = token;
        _fixedKey = key;
        ClearThought();
        return true;
    }

    public bool TryClearFixedMessage(uint token)
    {
        if (token == 0 || token != _fixedToken)
            return false;
        _fixedToken = 0;
        _fixedKey = default;
        ClearThought();
        return true;
    }

    public bool TryGetMessage(IReadOnlyList<LocalizeKey> candidates, float duration,
        double now, IRandomSource random, out LocalizeKey key)
    {
        key = default;
        if (_fixedToken != 0)
        {
            key = _fixedKey;
            return true;
        }
        if (double.IsNaN(now) || double.IsInfinity(now) || float.IsNaN(duration)
            || float.IsInfinity(duration) || duration <= 0f)
            return false;

        if (_hasThought && now < _expiresAt && Contains(candidates, _thoughtKey))
        {
            key = _thoughtKey;
            return true;
        }
        if (!NPCThoughtSelector.TrySelect(candidates, _previousKey, random, out key))
        {
            ClearThought();
            return false;
        }
        _thoughtKey = key;
        _previousKey = key;
        _expiresAt = now + duration;
        _hasThought = true;
        return true;
    }

    public void Reset()
    {
        _stat = null;
        _fixedToken = 0;
        _fixedKey = default;
        _previousKey = default;
        ClearThought();
        // Never recycle issued tokens on disable or stat replacement.
    }

    private void ClearThought()
    {
        _thoughtKey = default;
        _expiresAt = 0d;
        _hasThought = false;
    }

    private static bool Contains(IReadOnlyList<LocalizeKey> candidates, LocalizeKey key)
    {
        if (candidates == null)
            return false;
        for (int i = 0; i < candidates.Count; ++i)
            if (candidates[i] == key)
                return true;
        return false;
    }
}
