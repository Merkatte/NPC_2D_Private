using UnityEngine;

// Only this startup policy knows that a scene launch should play its authored tutorial.
[DefaultExecutionOrder(-9500)]
public sealed class DefenseTutorialStartup : MonoBehaviour
{
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private GameTimeController _timeController;
    [SerializeField] private SequenceRunner _runner;
    [SerializeField] private SequenceDefinition _sequence;
    [SerializeField] private DefenseGameSession _gameSession;
    private int _startupPause;

    private void Awake()
    {
        if (!_autoStart) return;
        if (!_timeController || !_runner || !_sequence)
        { Debug.LogError("DefenseTutorialStartup: assign time controller, runner and sequence.", this); enabled = false; return; }
        _startupPause = _timeController.AcquirePause("Tutorial startup");
    }
    private void OnEnable()
    {
        if (_gameSession) _gameSession.Changed += HandleSessionChanged;
    }
    private void HandleSessionChanged()
    {
        if (_gameSession && _gameSession.IsGameOver && _runner) _runner.Cancel();
    }
    private void Start()
    {
        if (_autoStart && (!_gameSession || !_gameSession.IsGameOver) && !_runner.TryStart(_sequence))
            Debug.LogError("DefenseTutorialStartup: tutorial could not start.", this);
        ReleaseStartupPause();
    }
    public void SkipForContinue()
    {
        _autoStart = false;
        ReleaseStartupPause();
    }
    private void ReleaseStartupPause()
    {
        if (_timeController) _timeController.ReleasePause(_startupPause);
        _startupPause = 0;
    }
    private void OnDisable()
    {
        if (_gameSession) _gameSession.Changed -= HandleSessionChanged;
        ReleaseStartupPause();
    }
}
