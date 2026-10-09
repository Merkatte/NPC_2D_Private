using UnityEngine;
using UnityEngine.UI;

public sealed class DefenseHUD : MonoBehaviour
{
    [SerializeField] private DefenseGameSession _session;
    [SerializeField] private DefenseWaveController _waves;
    [SerializeField] private DefenseEventController _events;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _notificationText;
    [SerializeField] private Button _nextWaveButton;
    [SerializeField] private Button _notificationButton;
    [SerializeField] private GameObject _gameOverPanel;

    private bool _isConfigured;
    private bool _isSubscribed;
    private int _displayRestSeconds = -1;
    private int _displayEventSeconds = -1;
    private int _displayAliveCount = -1;

    public void Configure(DefenseGameSession session, DefenseWaveController waves, DefenseEventController events,
        Text statusText, Text notificationText, Button nextWaveButton, Button notificationButton,
        GameObject gameOverPanel)
    {
        Unsubscribe();
        _session = session;
        _waves = waves;
        _events = events;
        _statusText = statusText;
        _notificationText = notificationText;
        _nextWaveButton = nextWaveButton;
        _notificationButton = notificationButton;
        _gameOverPanel = gameOverPanel;
        _isConfigured = false;
        if (Application.isPlaying && isActiveAndEnabled) Initialize();
    }

    private void Start() => Initialize();

    private void Initialize()
    {
        if (_isConfigured) return;
        _isConfigured = _session && _waves && _events && _statusText && _notificationText &&
            _nextWaveButton && _notificationButton && _gameOverPanel && _gameOverPanel != gameObject;
        if (!_isConfigured)
        {
            Debug.LogError($"DefenseHUD '{name}': missing session, controllers, labels, buttons or separate game-over panel.", this);
            enabled = false;
            return;
        }
        Subscribe();
    }

    private void OnEnable()
    {
        if (_isConfigured) Subscribe();
    }

    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (_isSubscribed) return;
        _session.Changed += Refresh;
        _waves.Changed += Refresh;
        _events.Changed += Refresh;
        _nextWaveButton.onClick.AddListener(StartNextWave);
        _notificationButton.onClick.AddListener(OpenEvent);
        _isSubscribed = true;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;
        if (_session) _session.Changed -= Refresh;
        if (_waves) _waves.Changed -= Refresh;
        if (_events) _events.Changed -= Refresh;
        if (_nextWaveButton) _nextWaveButton.onClick.RemoveListener(StartNextWave);
        if (_notificationButton) _notificationButton.onClick.RemoveListener(OpenEvent);
        _isSubscribed = false;
    }

    private void Update()
    {
        if (!_isConfigured || !_waves || !_events || !_session) return;
        if (_displayRestSeconds != Mathf.CeilToInt(_waves.RestRemainingSeconds) ||
            _displayEventSeconds != Mathf.CeilToInt(_events.RemainingSeconds) ||
            _displayAliveCount != _waves.AliveEnemyCount) Refresh();
    }

    private void Refresh()
    {
        if (!_isConfigured || !_session || !_waves || !_events) return;
        _displayRestSeconds = Mathf.CeilToInt(_waves.RestRemainingSeconds);
        _displayEventSeconds = Mathf.CeilToInt(_events.RemainingSeconds);
        _displayAliveCount = _waves.AliveEnemyCount;
        if (_session.IsGameOver)
            _statusText.text = "시청이 무너졌습니다. 게임 오버";
        else if (_waves.HasFailed)
            _statusText.text = "적 생성 오류로 습격 진행이 중단되었습니다.";
        else if (!_waves.IsInitialized)
            _statusText.text = "방어 준비 중";
        else if (_waves.IsResting)
            _statusText.text = $"정비 · 다음 습격 {_waves.NextWaveNumber} · {_displayRestSeconds}초";
        else
            _statusText.text = $"습격 {_waves.WaveNumber} · 남은 적 {_displayAliveCount} · 생성 대기 {_waves.PendingSpawnCount}";
        _nextWaveButton.interactable = _waves.CanStartNextWave;
        _gameOverPanel.SetActive(_session.IsGameOver);
        bool hasNotification = _events.HasNotification && !_session.IsGameOver;
        _notificationButton.gameObject.SetActive(hasNotification);
        _notificationButton.interactable = hasNotification && !_events.IsPopupOpen;
        _notificationText.text = hasNotification
            ? $"{_events.CurrentEvent.NotificationText} ({_displayEventSeconds}초)" : string.Empty;
    }

    private void StartNextWave()
    {
        if (_waves) _waves.TryStartNextWave();
    }

    private void OpenEvent()
    {
        if (_events) _events.TryOpenPopup();
    }
}
