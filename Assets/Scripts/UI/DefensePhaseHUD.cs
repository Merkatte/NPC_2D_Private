using UnityEngine;
using UnityEngine.UI;

public sealed class DefensePhaseHUD : MonoBehaviour
{
    [SerializeField] private DefenseGameSession _session;
    [SerializeField] private DefenseWaveController _waves;
    [SerializeField] private Text _statusText;
    [SerializeField] private Button _nextWaveButton;

    private bool _hasStarted;
    private bool _isSubscribed;
    private bool _hasReportedConfigurationError;
    private int _displayRestSeconds = -1;
    private int _displayAliveCount = -1;
    private int _displayPendingCount = -1;
    private bool _canStartNextWave;

    public void Configure(DefenseGameSession session, DefenseWaveController waves)
    {
        Unsubscribe();
        _session = session;
        _waves = waves;
        if (Application.isPlaying && isActiveAndEnabled)
            Initialize();
    }

    private void Start()
    {
        _hasStarted = true;
        Initialize();
    }

    private void OnEnable()
    {
        if (_hasStarted)
            Initialize();
    }

    private void OnDisable() => Unsubscribe();

    private void Initialize()
    {
        if (_isSubscribed)
            return;
        if (!_session || !_waves || !_statusText || !_nextWaveButton || _nextWaveButton.gameObject == gameObject)
        {
            FailConfiguration();
            return;
        }
        _statusText.raycastTarget = false;
        _session.Changed += Refresh;
        _waves.Changed += Refresh;
        _nextWaveButton.onClick.AddListener(StartNextWave);
        _isSubscribed = true;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed)
            return;
        if (_session)
            _session.Changed -= Refresh;
        if (_waves)
            _waves.Changed -= Refresh;
        if (_nextWaveButton)
            _nextWaveButton.onClick.RemoveListener(StartNextWave);
        _isSubscribed = false;
    }

    private void Update()
    {
        if (!_isSubscribed)
            return;
        if (!_session || !_waves || !_statusText || !_nextWaveButton)
        {
            Unsubscribe();
            FailConfiguration();
            return;
        }
        // Controllers publish phase/spawn changes; poll only cheap values for seconds and deaths.
        if (_displayRestSeconds != Mathf.CeilToInt(_waves.RestRemainingSeconds) ||
            _displayAliveCount != _waves.AliveEnemyCount || _displayPendingCount != _waves.PendingSpawnCount ||
            _canStartNextWave != _waves.CanStartNextWave)
            Refresh();
    }

    private void Refresh()
    {
        if (!_isSubscribed || !_session || !_waves || !_statusText || !_nextWaveButton)
            return;
        _displayRestSeconds = Mathf.CeilToInt(_waves.RestRemainingSeconds);
        _displayAliveCount = _waves.AliveEnemyCount;
        _displayPendingCount = _waves.PendingSpawnCount;
        _canStartNextWave = _waves.CanStartNextWave;
        if (_session.IsGameOver)
            _statusText.text = "게임 오버";
        else if (_waves.HasFailed)
            _statusText.text = "습격 진행 중단 · 생성 오류";
        else if (!_waves.IsInitialized)
            _statusText.text = "방어 준비 중";
        else if (_waves.IsResting)
            _statusText.text = $"정비 · 다음 습격 {_waves.NextWaveNumber} · {_displayRestSeconds}초";
        else
            _statusText.text = $"습격 {_waves.WaveNumber} · 적 {_displayAliveCount} · 대기 {_displayPendingCount}";
        _nextWaveButton.gameObject.SetActive(_waves.IsInitialized && _waves.IsResting &&
            !_waves.HasFailed && !_session.IsGameOver);
        _nextWaveButton.interactable = _canStartNextWave;
    }

    private void StartNextWave()
    {
        if (_waves)
            _waves.TryStartNextWave();
    }

    private void FailConfiguration()
    {
        if (!_hasReportedConfigurationError)
        {
            Debug.LogError($"DefensePhaseHUD '{name}': assign _session, _waves, _statusText and a separate child _nextWaveButton.", this);
            _hasReportedConfigurationError = true;
        }
        if (_statusText)
            _statusText.text = string.Empty;
        if (_nextWaveButton)
            _nextWaveButton.interactable = false;
    }
}
