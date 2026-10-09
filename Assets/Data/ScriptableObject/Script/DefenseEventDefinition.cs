using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Defense/Event")]
public sealed class DefenseEventDefinition : ScriptableObject
{
    [SerializeField] private string _eventId;
    [SerializeField, Min(1)] private int _firstWave = 1;
    [SerializeField, Min(0)] private int _lastWave;
    [SerializeField, Min(0f)] private float _restElapsedSeconds;
    [SerializeField, Min(0.01f)] private float _lifetimeSeconds = 30f;
    [SerializeField] private string _notificationText;
    [SerializeField] private string _title;
    [SerializeField, TextArea] private string _body;
    [SerializeField] private DefenseEventChoice[] _choices;

    public string EventId => _eventId;
    public float LifetimeSeconds => _lifetimeSeconds;
    public string NotificationText => _notificationText;
    public string Title => _title;
    public string Body => _body;
    public IReadOnlyList<DefenseEventChoice> Choices => _choices;

    public bool IsValid
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_eventId) || _firstWave < 1 ||
                (_lastWave != 0 && _lastWave < _firstWave) || _restElapsedSeconds < 0f ||
                float.IsNaN(_restElapsedSeconds) || float.IsInfinity(_restElapsedSeconds) ||
                !(_lifetimeSeconds > 0f) || float.IsInfinity(_lifetimeSeconds) ||
                string.IsNullOrWhiteSpace(_notificationText) || string.IsNullOrWhiteSpace(_title) ||
                _choices == null || _choices.Length == 0) return false;
            foreach (DefenseEventChoice choice in _choices)
                if (!choice.IsValid) return false;
            return true;
        }
    }

    public bool CanAppear(int nextWave, float restElapsedSeconds)
        => nextWave >= _firstWave && (_lastWave == 0 || nextWave <= _lastWave) &&
            restElapsedSeconds >= _restElapsedSeconds;

    private void OnValidate()
    {
        _firstWave = Mathf.Max(1, _firstWave);
        _lastWave = _lastWave == 0 ? 0 : Mathf.Max(_firstWave, _lastWave);
        _restElapsedSeconds = float.IsNaN(_restElapsedSeconds) || float.IsInfinity(_restElapsedSeconds)
            ? 0f : Mathf.Max(0f, _restElapsedSeconds);
        _lifetimeSeconds = float.IsNaN(_lifetimeSeconds) || float.IsInfinity(_lifetimeSeconds)
            ? 30f : Mathf.Max(0.01f, _lifetimeSeconds);
    }
}
