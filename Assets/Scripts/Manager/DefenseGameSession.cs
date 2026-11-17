using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DefenseGameSession : MonoBehaviour
{
    [SerializeField] private GameTimeController _timeController;
    private readonly HashSet<int> _pauseLeases = new HashSet<int>();
    private int _gameOverLease;
    public bool IsGameOver { get; private set; }
    public bool IsPaused => IsGameOver || (_timeController && _timeController.IsPaused);
    public event Action Changed;

    private void OnEnable()
    {
        if (!_timeController)
        {
            Debug.LogError("DefenseGameSession: assign _timeController.", this);
            enabled = false;
            return;
        }
        _timeController.Changed += HandleTimeChanged;
        if (IsGameOver) _gameOverLease = _timeController.AcquirePause("GameOver");
    }
    private void HandleTimeChanged() => Changed?.Invoke();
    public int AcquirePause(string reason)
    {
        if (!isActiveAndEnabled || IsGameOver || !_timeController) return 0;
        int token = _timeController.AcquirePause(reason);
        if (token != 0) _pauseLeases.Add(token);
        return token;
    }
    public void ReleasePause(int leaseId)
    {
        if (!_pauseLeases.Remove(leaseId)) return;
        if (_timeController) _timeController.ReleasePause(leaseId);
    }
    public void EndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        if (_timeController) _gameOverLease = _timeController.AcquirePause("GameOver");
        Changed?.Invoke();
    }
    private void OnDisable()
    {
        if (_timeController)
        {
            _timeController.Changed -= HandleTimeChanged;
            foreach (int lease in _pauseLeases) _timeController.ReleasePause(lease);
            _timeController.ReleasePause(_gameOverLease);
        }
        _pauseLeases.Clear();
        _gameOverLease = 0;
        Changed?.Invoke();
    }
}
