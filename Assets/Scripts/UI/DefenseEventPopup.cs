using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class DefenseEventPopup : MonoBehaviour
{
    [SerializeField] private DefenseEventController _events;
    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _bodyText;
    [SerializeField] private Button[] _choiceButtons;
    [SerializeField] private Text[] _choiceLabels;
    [SerializeField] private Button _closeButton;

    private UnityAction[] _choiceListeners;
    private bool _isConfigured;
    private bool _isSubscribed;
    private bool _hasReportedCapacityError;

    public void Configure(DefenseEventController events, GameObject panel, Text titleText, Text bodyText,
        Button[] choiceButtons, Text[] choiceLabels, Button closeButton)
    {
        Unsubscribe();
        if (_events) _events.ClosePopup();
        _events = events;
        _panel = panel;
        _titleText = titleText;
        _bodyText = bodyText;
        _choiceButtons = choiceButtons;
        _choiceLabels = choiceLabels;
        _closeButton = closeButton;
        _isConfigured = false;
        if (Application.isPlaying && isActiveAndEnabled) Initialize();
    }

    private void Start() => Initialize();

    private void Initialize()
    {
        if (_isConfigured) return;
        _isConfigured = _events && _panel && _panel != gameObject && _titleText && _bodyText &&
            _closeButton && _choiceButtons != null && _choiceLabels != null && _choiceButtons.Length > 0 &&
            _choiceButtons.Length == _choiceLabels.Length;
        if (_isConfigured)
            for (int i = 0; i < _choiceButtons.Length; ++i)
                _isConfigured &= _choiceButtons[i] && _choiceLabels[i];
        if (!_isConfigured)
        {
            Debug.LogError($"DefenseEventPopup '{name}': missing controller, separate panel, labels or matching choice buttons.", this);
            enabled = false;
            return;
        }
        _choiceListeners = new UnityAction[_choiceButtons.Length];
        for (int i = 0; i < _choiceListeners.Length; ++i)
        {
            int index = i;
            _choiceListeners[i] = () => Choose(index);
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
        if (_events) _events.ClosePopup();
        if (_panel && _panel != gameObject) _panel.SetActive(false);
    }

    private void Subscribe()
    {
        if (_isSubscribed) return;
        _events.Changed += Refresh;
        _closeButton.onClick.AddListener(Close);
        for (int i = 0; i < _choiceButtons.Length; ++i)
            _choiceButtons[i].onClick.AddListener(_choiceListeners[i]);
        _isSubscribed = true;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;
        if (_events) _events.Changed -= Refresh;
        if (_closeButton) _closeButton.onClick.RemoveListener(Close);
        for (int i = 0; i < _choiceButtons.Length; ++i)
            if (_choiceButtons[i]) _choiceButtons[i].onClick.RemoveListener(_choiceListeners[i]);
        _isSubscribed = false;
    }

    private void Update()
    {
        if (_isConfigured && !_events) _panel.SetActive(false);
    }

    private void Refresh()
    {
        if (!_isConfigured || !_events) return;
        DefenseEventDefinition definition = _events.CurrentEvent;
        bool isOpen = _events.IsPopupOpen && definition;
        _panel.SetActive(isOpen);
        if (!isOpen) return;
        if (definition.Choices.Count > _choiceButtons.Length)
        {
            if (!_hasReportedCapacityError)
            {
                Debug.LogError($"DefenseEventPopup '{name}': event '{definition.EventId}' needs more choice buttons.", this);
                _hasReportedCapacityError = true;
            }
            _events.ClosePopup();
            return;
        }
        _titleText.text = definition.Title;
        _bodyText.text = definition.Body;
        for (int i = 0; i < _choiceButtons.Length; ++i)
        {
            bool hasChoice = i < definition.Choices.Count;
            _choiceButtons[i].gameObject.SetActive(hasChoice);
            if (hasChoice) _choiceLabels[i].text = definition.Choices[i].Label;
        }
    }

    private void Close()
    {
        if (_events) _events.ClosePopup();
    }

    private void Choose(int index)
    {
        if (_events) _events.TryChoose(index);
    }
}
