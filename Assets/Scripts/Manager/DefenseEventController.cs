using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class DefenseEventController : MonoBehaviour
{
    [SerializeField] private DefenseGameSession _session;
    [SerializeField] private DefenseWaveController _waves;
    [SerializeField] private DefenseEventCatalog _catalog;
    [SerializeField] private UnityEvent<string, string> _choiceResolved = new UnityEvent<string, string>();

    private readonly HashSet<string> _presentedIds = new HashSet<string>();
    private float _expiresAtRestElapsed;
    private int _restWaveNumber = -1;
    private int _pauseLease;
    private bool _isConfigured;
    private bool _isSubscribed;

    public DefenseEventDefinition CurrentEvent { get; private set; }
    public bool HasNotification => CurrentEvent;
    public bool IsPopupOpen => _pauseLease != 0;
    public float RemainingSeconds => CurrentEvent && _waves
        ? Mathf.Max(0f, _expiresAtRestElapsed - _waves.RestElapsedSeconds) : 0f;
    public event Action Changed;
    public event Action<string, string> ChoiceResolved;

    public void Configure(DefenseGameSession session, DefenseWaveController waves, DefenseEventCatalog catalog)
    {
        Unsubscribe();
        CancelCurrent();
        _session = session;
        _waves = waves;
        _catalog = catalog;
        _isConfigured = false;
        _restWaveNumber = -1;
        _presentedIds.Clear();
        if (Application.isPlaying && isActiveAndEnabled) Initialize();
    }

    private void Start() => Initialize();

    private void Initialize()
    {
        if (_isConfigured) return;
        _isConfigured = _session && _waves && _catalog && _catalog.IsValid;
        if (!_isConfigured)
        {
            Debug.LogError($"DefenseEventController '{name}': missing session, waves or valid event catalog.", this);
            enabled = false;
            return;
        }
        Subscribe();
    }

    private void OnEnable()
    {
        if (_isConfigured) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CancelCurrent();
    }

    private void Subscribe()
    {
        if (_isSubscribed) return;
        _session.Changed += HandleSessionChanged;
        _waves.Changed += HandleWaveChanged;
        _isSubscribed = true;
        HandleSessionChanged();
        HandleWaveChanged();
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;
        if (_session) _session.Changed -= HandleSessionChanged;
        if (_waves) _waves.Changed -= HandleWaveChanged;
        _isSubscribed = false;
    }

    private void Update()
    {
        if (!_isConfigured) return;
        if (!_session || !_waves || !_catalog || !_waves.isActiveAndEnabled || !_session.isActiveAndEnabled)
        {
            CancelCurrent();
            return;
        }
        if (_session.IsGameOver || !_waves.IsResting || _waves.HasFailed || !_waves.IsInitialized) return;
        if (_session.IsPaused || Time.timeScale <= 0f) return;
        if (CurrentEvent)
        {
            if (RemainingSeconds <= 0f) CancelCurrent();
            return;
        }
        foreach (DefenseEventDefinition definition in _catalog.Events)
        {
            if (!definition || _presentedIds.Contains(definition.EventId) ||
                !definition.CanAppear(_waves.NextWaveNumber, _waves.RestElapsedSeconds)) continue;
            _presentedIds.Add(definition.EventId);
            CurrentEvent = definition;
            _expiresAtRestElapsed = _waves.RestElapsedSeconds + definition.LifetimeSeconds;
            Changed?.Invoke();
            break;
        }
    }

    public bool TryOpenPopup()
    {
        if (!isActiveAndEnabled || !_isConfigured || !_session || !_waves || !_waves.IsResting ||
            _waves.HasFailed || _session.IsGameOver || !CurrentEvent || RemainingSeconds <= 0f) return false;
        if (IsPopupOpen) return true;
        _pauseLease = _session.AcquirePause($"Defense event: {CurrentEvent.EventId}");
        if (_pauseLease == 0) return false;
        Changed?.Invoke();
        return true;
    }

    public void ClosePopup()
    {
        if (_pauseLease == 0) return;
        int lease = _pauseLease;
        _pauseLease = 0;
        if (_session) _session.ReleasePause(lease);
        Changed?.Invoke();
    }

    public bool TryChoose(int choiceIndex)
    {
        if (!isActiveAndEnabled || !_session || _session.IsGameOver || !IsPopupOpen || !CurrentEvent ||
            choiceIndex < 0 || choiceIndex >= CurrentEvent.Choices.Count) return false;
        string eventId = CurrentEvent.EventId;
        string resultId = CurrentEvent.Choices[choiceIndex].ResultId;
        // Clear first: listeners and repeated button clicks cannot resolve the same offer twice.
        CancelCurrent();
        _choiceResolved.Invoke(eventId, resultId);
        ChoiceResolved?.Invoke(eventId, resultId);
        return true;
    }

    private void HandleSessionChanged()
    {
        if (!_session || _session.IsGameOver || !_session.isActiveAndEnabled) CancelCurrent();
    }

    private void HandleWaveChanged()
    {
        if (!_waves || !_waves.IsResting || _waves.HasFailed)
        {
            CancelCurrent();
            return;
        }
        if (_restWaveNumber == _waves.NextWaveNumber) return;
        CancelCurrent();
        _restWaveNumber = _waves.NextWaveNumber;
        _presentedIds.Clear();
    }

    private void CancelCurrent()
    {
        bool hadCurrent = CurrentEvent || IsPopupOpen;
        CurrentEvent = null;
        _expiresAtRestElapsed = 0f;
        ClosePopup();
        if (hadCurrent) Changed?.Invoke();
    }
}
