using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class GameTimeController : MonoBehaviour
{
    private readonly Dictionary<int, string> _leases = new Dictionary<int, string>();
    private int _nextToken;
    private float _resumeScale;
    private bool _hasSnapshot;

    public bool IsPaused => _leases.Count > 0;
    public event Action Changed;

    public int AcquirePause(string reason)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(reason) || _nextToken == int.MaxValue) return 0;
        if (!_hasSnapshot)
        {
            _resumeScale = Time.timeScale;
            _hasSnapshot = true;
        }
        int token = ++_nextToken;
        _leases.Add(token, reason);
        Time.timeScale = 0f;
        Changed?.Invoke();
        return token;
    }

    public void ReleasePause(int token)
    {
        if (!_leases.Remove(token)) return;
        if (!IsPaused) Restore();
        Changed?.Invoke();
    }

    private void Restore()
    {
        if (!_hasSnapshot) return;
        Time.timeScale = _resumeScale;
        _hasSnapshot = false;
    }

    private void OnDisable()
    {
        _leases.Clear();
        Restore();
        Changed?.Invoke();
    }
}
