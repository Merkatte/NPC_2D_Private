using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueView : MonoBehaviour
{
    [SerializeField] private LocalizeData _localization;
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _blocker;
    [SerializeField] private RectTransform _bubble;
    [SerializeField] private Text _bubbleText;
    [SerializeField] private RectTransform _bottomPanel;
    [SerializeField] private Text _bottomText;
    [SerializeField] private Text _speakerText;
    [SerializeField] private RectTransform _choiceRoot;
    [SerializeField] private Button _choiceTemplate;
    [SerializeField, Min(0f)] private float _characterInterval = 0.025f;
    [SerializeField, Min(0f)] private float _enterDuration = 0.2f;
    [SerializeField, Min(0f)] private float _exitDuration = 0.15f;
    [SerializeField, Min(0f)] private float _riseDistance = 18f;
    [SerializeField, Range(0f, 1f)] private float _hiddenScale = 0.65f;
    private CanvasGroup _bubbleGroup;
    private CanvasGroup _bottomGroup;
    private Vector3 _bubbleScale;
    private bool _hasPresentation;
    private float _motionProgress;
    private Tween _motionTween;
    private readonly MessageTyping _typing = new MessageTyping();
    private readonly List<Button> _choiceButtons = new List<Button>();
    private readonly List<Text> _choiceTexts = new List<Text>();
    private readonly List<Image> _choiceImages = new List<Image>();
    private Button _cachedChoiceTemplate;
    private Text _templateText;
    private Image _templateImage;
    private bool _isSessionVisible;
    private readonly List<string> _choiceIds = new List<string>();
    private readonly Vector3[] _corners = new Vector3[4];
    private Transform _anchor;
    private RectTransform _canvasRect;
    private int _selectedChoice;
    public event Action<string> ChoiceRequested;
    public event Action<string> PresentationFailed;
    public bool IsTyping => _typing.IsTyping;
    public bool IsConfigured
    {
        get
        {
            if (!_localization || !_worldCamera || !_canvas || !_bubble || !_bubbleText ||
                !_bottomPanel || !_bottomText || !_speakerText || !_choiceRoot || !_choiceTemplate || !_blocker)
                return false;
            // Resolve the template only at an identity boundary, never every frame.
            if (_cachedChoiceTemplate != _choiceTemplate)
            {
                _cachedChoiceTemplate = _choiceTemplate;
                _templateText = _choiceTemplate.GetComponentInChildren<Text>(true);
                _templateImage = _choiceTemplate.image;
            }
            if (!_templateText || !_templateImage || _choiceTemplate.image != _templateImage) return false;
            for (int i = 0; i < _choiceButtons.Count; i++)
                if (!_choiceButtons[i] || !_choiceTexts[i] || !_choiceImages[i] ||
                    _choiceButtons[i].image != _choiceImages[i]) return false;
            return true;
        }
    }

    public bool TryShow(SequenceStep step, Transform anchor)
    {
        if (step == null || !IsConfigured || !_localization.TryGetText(step.TextKey, "ko", out string text)) return false;
        if (step.Presentation == DialoguePresentation.Bubble && !anchor) return false;
        HideLineImmediately();
        EnsurePresentation();
        if (!_localization.TryGetText(step.SpeakerKey, "ko", out string speaker)) return false;
        _speakerText.text = speaker;
        _canvasRect = _canvas.transform as RectTransform;
        _anchor = anchor;
        bool bubble = step.Presentation == DialoguePresentation.Bubble;
        _bubble.gameObject.SetActive(bubble);
        _bottomPanel.gameObject.SetActive(!bubble);
        _typing.Begin(bubble ? _bubbleText : _bottomText, text, _characterInterval);
        if (step.Kind == SequenceStepKind.Choice)
        {
            foreach (SequenceChoice choice in step.Choices)
            {
                if (!_localization.TryGetText(choice.TextKey, "ko", out string label)) { HideLine(); return false; }
                Button button = Instantiate(_choiceTemplate, _choiceRoot);
                Text choiceText = button.GetComponentInChildren<Text>(true);
                Image choiceImage = button.image;
                _choiceButtons.Add(button);
                _choiceTexts.Add(choiceText);
                _choiceImages.Add(choiceImage);
                if (!choiceText || !choiceImage) { HideLineImmediately(); return false; }
                button.gameObject.SetActive(true);
                choiceText.text = label;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                string id = choice.Id;
                button.onClick.AddListener(() => { if (!IsTyping) ChoiceRequested?.Invoke(id); });
                _choiceIds.Add(id);
            }
        }
        _selectedChoice = 0;
        RefreshChoices();
        PlayMotion(true);
        return true;
    }

    public void SetSessionVisible(bool visible)
    {
        _isSessionVisible = visible;
        if (_blocker) _blocker.SetActive(visible);
        if (visible) transform.SetAsLastSibling();
        else HideLineImmediately();
    }

    public void Reveal() { if (!ValidatePresentation()) return; _typing.Reveal(); RefreshChoices(); }
    public void MoveChoice(int direction)
    {
        if (!ValidatePresentation() || IsTyping || _choiceButtons.Count == 0) return;
        _selectedChoice = (_selectedChoice + direction + _choiceButtons.Count) % _choiceButtons.Count;
        RefreshChoices();
    }
    public string SelectedChoiceId => _choiceIds.Count > 0 ? _choiceIds[_selectedChoice] : null;
    public string ChoiceIdAt(int index) => index >= 0 && index < _choiceIds.Count ? _choiceIds[index] : null;

    private void RefreshChoices()
    {
        if (_choiceRoot) _choiceRoot.gameObject.SetActive(_choiceButtons.Count > 0 && !IsTyping);
        for (int i = 0; i < _choiceButtons.Count; i++)
            if (_choiceImages[i]) _choiceImages[i].color = i == _selectedChoice ? new Color(1f, 0.9f, 0.55f) : Color.white;
    }

    private bool ValidatePresentation()
    {
        if (!_isSessionVisible || IsConfigured) return true;
        SetSessionVisible(false);
        PresentationFailed?.Invoke("Dialogue presentation reference was destroyed or removed.");
        return false;
    }

    private void LateUpdate()
    {
        if (!ValidatePresentation()) return;
        RefreshChoices();
        if (!_bubble || !_bubble.gameObject.activeSelf || !_anchor || !_worldCamera || !_canvasRect) return;
        _bubble.localScale = _bubbleScale * Mathf.Lerp(_hiddenScale, 1f, _motionProgress);
        bool visible = MessageBubblePosition.Apply(_bubble, _canvasRect, _canvas, _worldCamera,
            _anchor.position, 8f, _corners, Vector2.down * (_riseDistance * (1f - _motionProgress)));
        if (_bubbleGroup) _bubbleGroup.alpha = visible ? _motionProgress : 0f;
    }

    private void EnsurePresentation()
    {
        if (_hasPresentation) return;
        _bubbleScale = _bubble.localScale;
        _bubbleGroup = _bubble.GetComponent<CanvasGroup>();
        if (!_bubbleGroup) _bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
        _bottomGroup = _bottomPanel.GetComponent<CanvasGroup>();
        if (!_bottomGroup) _bottomGroup = _bottomPanel.gameObject.AddComponent<CanvasGroup>();
        _hasPresentation = true;
    }

    private void PlayMotion(bool show)
    {
        _motionTween?.Kill();
        float target = show ? 1f : 0f;
        float duration = (show ? _enterDuration : _exitDuration) * Mathf.Abs(target - _motionProgress);
        if (duration <= 0f)
        {
            _motionProgress = target;
            ApplyOpacity();
            if (!show) HideLineImmediately();
            return;
        }
        _motionTween = DOTween.To(() => _motionProgress, value => { _motionProgress = value; ApplyOpacity(); }, target, duration)
            .SetEase(show ? Ease.OutCubic : Ease.InCubic).SetUpdate(true)
            .OnComplete(() => { _motionTween = null; if (!show) HideLineImmediately(); });
    }

    private void ApplyOpacity()
    {
        if (_bubbleGroup) _bubbleGroup.alpha = _motionProgress;
        if (_bottomGroup) _bottomGroup.alpha = _motionProgress;
    }

    public void HideLine()
    {
        ClearChoices();
        _typing.Reset();
        if (!_hasPresentation) { HideLineImmediately(); return; }
        PlayMotion(false);
    }

    private void ClearChoices()
    {
        if (_choiceRoot) _choiceRoot.gameObject.SetActive(false);
        foreach (Button button in _choiceButtons) if (button) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
        _choiceButtons.Clear();
        _choiceTexts.Clear();
        _choiceImages.Clear();
        _choiceIds.Clear();
    }

    private void HideLineImmediately()
    {
        _motionTween?.Kill();
        _motionTween = null;
        _motionProgress = 0f;
        ApplyOpacity();
        _typing.Reset();
        if (_bubble) _bubble.gameObject.SetActive(false);
        if (_bottomPanel) _bottomPanel.gameObject.SetActive(false);
        ClearChoices();
        _anchor = null;
    }

    private void OnDisable() { SetSessionVisible(false); }
    private void OnValidate()
    {
        _characterInterval = Mathf.Max(0f, _characterInterval);
        _enterDuration = Mathf.Max(0f, _enterDuration);
        _exitDuration = Mathf.Max(0f, _exitDuration);
        _riseDistance = Mathf.Max(0f, _riseDistance);
        _hiddenScale = Mathf.Clamp01(_hiddenScale);
    }
}
