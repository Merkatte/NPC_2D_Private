using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DefenseGameSession : MonoBehaviour
{
    private readonly Dictionary<int, string> _pauseLeases = new Dictionary<int, string>();
    private int _nextLeaseId;
    private float _resumeTimeScale;
    private bool _hasCapturedTimeScale;

    public bool IsGameOver { get; private set; }
    public bool IsPaused => IsGameOver || _pauseLeases.Count > 0;
    public event Action Changed;

    private void OnEnable()
    {
        if (IsPaused) ApplyPause();
    }

    public int AcquirePause(string reason)
    {
        if (!isActiveAndEnabled || IsGameOver || string.IsNullOrWhiteSpace(reason)) return 0;
        do { _nextLeaseId = _nextLeaseId == int.MaxValue ? 1 : _nextLeaseId + 1; }
        while (_pauseLeases.ContainsKey(_nextLeaseId));
        _pauseLeases.Add(_nextLeaseId, reason);
        ApplyPause();
        Changed?.Invoke();
        return _nextLeaseId;
    }

    public void ReleasePause(int leaseId)
    {
        if (!_pauseLeases.Remove(leaseId)) return;
        if (!IsPaused) RestoreTimeScale();
        Changed?.Invoke();
    }

    public void EndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        ApplyPause();
        Changed?.Invoke();
    }

    private void ApplyPause()
    {
        if (!_hasCapturedTimeScale)
        {
            _resumeTimeScale = Time.timeScale;
            _hasCapturedTimeScale = true;
        }
        Time.timeScale = 0f;
    }

    private void RestoreTimeScale()
    {
        if (!_hasCapturedTimeScale) return;
        Time.timeScale = _resumeTimeScale;
        _hasCapturedTimeScale = false;
    }

    private void OnDisable()
    {
        _pauseLeases.Clear();
        // Time.timeScale is global: scene teardown must not leave the next scene paused.
        RestoreTimeScale();
        Changed?.Invoke();
    }
}
