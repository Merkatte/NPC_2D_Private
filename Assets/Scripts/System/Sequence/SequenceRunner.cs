using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-8000)]
public sealed class SequenceRunner : MonoBehaviour
{
    [SerializeField] private GameTimeController _timeController;
    [SerializeField] private GameplayInputGate _inputGate;
    [SerializeField] private SequenceBindings _bindings;
    [SerializeField] private SequenceCameraDirector _cameraDirector;
    [SerializeField] private SequenceActorDirector _actorDirector;
    [SerializeField] private DialogueView _dialogue;
    private readonly Dictionary<string, SequenceStep> _index = new Dictionary<string, SequenceStep>();
    private SequenceStep _step;
    private SequenceAction _action;
    private bool _isWaitingForAction;
    private string _pendingId;
    private bool _hasPending;
    private int _pauseToken;
    private int _inputToken;
    private int _generation;
    private int _entryFrame;
    private int _inputFrame = -1;
    private float _fullTextTime = -1f;
    public bool IsRunning { get; private set; }
    public string CurrentStepId => _step == null ? null : _step.Id;
    public event Action Completed;
    public event Action Cancelled;
    public event Action<string> Failed;

    private void OnEnable()
    {
        if (!_dialogue) return;
        _dialogue.ChoiceRequested += Choose;
        _dialogue.PresentationFailed += HandlePresentationFailure;
    }
    private void HandlePresentationFailure(string reason)
    {
        if (IsRunning) Fail(reason);
    }
    private void OnDisable()
    {
        if (_dialogue)
        {
            _dialogue.ChoiceRequested -= Choose;
            _dialogue.PresentationFailed -= HandlePresentationFailure;
        }
        Cancel();
    }

    public bool TryStart(SequenceDefinition definition)
    {
        if (IsRunning || !isActiveAndEnabled || !definition || !_timeController || !_inputGate ||
            !_bindings || !_cameraDirector || !_actorDirector || !_dialogue || !_dialogue.IsConfigured) return false;
        if (!_bindings.TryValidate()) return false;
        if (!definition.TryBuildIndex(_index, out string error)) { Debug.LogError(error, definition); return false; }
        foreach (SequenceStep step in definition.Steps)
        {
            if ((step.Kind == SequenceStepKind.Actor ||
                 ((step.Kind == SequenceStepKind.Dialogue || step.Kind == SequenceStepKind.Choice) && step.Presentation == DialoguePresentation.Bubble)) &&
                !_bindings.TryGetActor(step.ActorId, out _, out _, out _)) return false;
            if (((step.Kind == SequenceStepKind.Camera && !step.RestoreCamera) ||
                 (step.Kind == SequenceStepKind.Actor && step.ActorCommand == SequenceActorCommand.Walk)) &&
                !_bindings.TryGetTarget(step.TargetId, out _)) return false;
            if (step.Kind == SequenceStepKind.ExternalAction && !_bindings.TryGetAction(step.ActionId, out _)) return false;
        }
        if (!_cameraDirector.TryCapture()) return false;
        _pauseToken = _timeController.AcquirePause("Sequence: " + definition.name);
        _inputToken = _inputGate.Acquire();
        if (_pauseToken == 0 || _inputToken == 0 || !isActiveAndEnabled || !_timeController.isActiveAndEnabled || !_inputGate.isActiveAndEnabled) { Cleanup(); return false; }
        IsRunning = true;
        _generation++;
        try { _dialogue.SetSessionVisible(true); }
        catch (Exception exception) { Fail("Cannot open dialogue: " + exception.Message); return false; }
        QueueStep(definition.EntryStepId);
        return true;
    }

    private void Update()
    {
        if (!IsRunning) return;
        if (!_timeController || !_timeController.isActiveAndEnabled || !_inputGate || !_inputGate.isActiveAndEnabled ||
            !_bindings || !_dialogue || !_dialogue.isActiveAndEnabled || !_dialogue.IsConfigured || !_cameraDirector || !_actorDirector || !_cameraDirector.isActiveAndEnabled || !_actorDirector.isActiveAndEnabled)
        { Fail("Sequence dependency was disabled or destroyed."); return; }
        if (_isWaitingForAction && (!_action || !_action.isActiveAndEnabled))
        { Fail("Active sequence action is unavailable."); return; }
        if (_hasPending)
        {
            string id = _pendingId;
            _hasPending = false;
            EnterStep(id);
            return;
        }
        if (_step == null || (_step.Kind != SequenceStepKind.Dialogue && _step.Kind != SequenceStepKind.Choice)) return;
        if (_step.Presentation == DialoguePresentation.Bubble &&
            (!_bindings.TryGetActor(_step.ActorId, out Transform actor, out _, out _) || !actor.gameObject.activeInHierarchy))
        { Fail("Dialogue actor is unavailable."); return; }
        if (_dialogue.IsTyping) _fullTextTime = -1f;
        else if (_fullTextTime < 0f) _fullTextTime = Time.unscaledTime;
        Keyboard keyboard = Keyboard.current;
        bool key = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (_step.Kind == SequenceStepKind.Dialogue)
        {
            if (key || click) Advance();
            else if (_fullTextTime >= 0f && Time.unscaledTime - _fullTextTime >= _step.AutoAdvanceDelay) Advance();
            return;
        }
        if (_dialogue.IsTyping) { if (key || click) Advance(); return; }
        if (keyboard == null || Time.frameCount <= _entryFrame || _inputFrame == Time.frameCount) return;
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            _dialogue.MoveChoice(-1);
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            _dialogue.MoveChoice(1);
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            Choose(_dialogue.SelectedChoiceId);
        else if (keyboard.digit1Key.wasPressedThisFrame) Choose(_dialogue.ChoiceIdAt(0));
        else if (keyboard.digit2Key.wasPressedThisFrame) Choose(_dialogue.ChoiceIdAt(1));
    }

    public void Advance()
    {
        if (!CanAcceptInput()) return;
        _inputFrame = Time.frameCount;
        if (_dialogue.IsTyping)
        {
            try { _dialogue.Reveal(); _fullTextTime = Time.unscaledTime; }
            catch (Exception exception) { Fail("Cannot reveal dialogue: " + exception.Message); }
            return;
        }
        if (_step.Kind == SequenceStepKind.Dialogue) QueueStep(_step.NextId);
    }

    public void Choose(string choiceId)
    {
        if (!CanAcceptInput() || _step.Kind != SequenceStepKind.Choice || _dialogue.IsTyping || string.IsNullOrEmpty(choiceId)) return;
        foreach (SequenceChoice choice in _step.Choices)
            if (choice.Id == choiceId) { _inputFrame = Time.frameCount; QueueStep(choice.NextId); return; }
    }

    private bool CanAcceptInput()
    {
        if (!IsRunning) return false;
        if (!_dialogue || !_dialogue.IsConfigured)
        { Fail("Dialogue presentation is unavailable."); return false; }
        return !_hasPending && _step != null &&
            (_step.Kind == SequenceStepKind.Dialogue || _step.Kind == SequenceStepKind.Choice) &&
            Time.frameCount > _entryFrame && _inputFrame != Time.frameCount;
    }

    private void QueueStep(string id) { _pendingId = id; _hasPending = true; }
    private void EnterStep(string id)
    {
        _dialogue.HideLine();
        _fullTextTime = -1f;
        _entryFrame = Time.frameCount;
        if (string.IsNullOrEmpty(id)) { Finish(); return; }
        if (!_index.TryGetValue(id, out _step)) { Fail("Missing step: " + id); return; }
        if (_step.Kind == SequenceStepKind.End) { Finish(); return; }
        if (_step.Kind == SequenceStepKind.Dialogue || _step.Kind == SequenceStepKind.Choice)
        {
            try
            {
                _bindings.TryGetActor(_step.ActorId, out _, out _, out Transform anchor);
                if (!_dialogue.TryShow(_step, anchor)) Fail("Cannot present step: " + id);
            }
            catch (Exception exception) { Fail("Presentation exception at " + id + ": " + exception.Message); }
            return;
        }
        _action = _step.Kind == SequenceStepKind.Camera ? _cameraDirector :
            _step.Kind == SequenceStepKind.Actor ? _actorDirector : null;
        if (_step.Kind == SequenceStepKind.ExternalAction) _bindings.TryGetAction(_step.ActionId, out _action);
        int generation = _generation;
        SequenceStep startedStep = _step;
        bool responded = false;
        _isWaitingForAction = true;
        try
        {
            if (!_action || !_action.isActiveAndEnabled || !_action.TryBegin(_step, _bindings, success =>
                {
                    if (responded || !IsRunning || generation != _generation || _step != startedStep) return;
                    responded = true;
                    _isWaitingForAction = false;
                    _action = null;
                    if (success) QueueStep(startedStep.NextId);
                    else Fail("Action failed: " + startedStep.Id);
                })) Fail("Cannot start action: " + id);
        }
        catch (Exception exception) { Fail("Action exception at " + id + ": " + exception.Message); }
    }

    public void Cancel()
    {
        if (!IsRunning) return;
        Cleanup();
        Cancelled?.Invoke();
    }
    private void Finish() { Cleanup(); Completed?.Invoke(); }
    private void Fail(string reason) { Cleanup(); Debug.LogError(reason, this); Failed?.Invoke(reason); }
    private void Cleanup()
    {
        IsRunning = false;
        _generation++;
        _hasPending = false;
        _isWaitingForAction = false;
        _step = null;
        if (_action)
        {
            try { _action.Cancel(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
        _action = null;
        try { if (_dialogue) _dialogue.SetSessionVisible(false); }
        catch (Exception exception) { Debug.LogException(exception, this); }
        if (_actorDirector) _actorDirector.RestoreActors();
        if (_cameraDirector) _cameraDirector.RestoreSnapshot();
        if (_inputGate) _inputGate.Release(_inputToken);
        if (_timeController) _timeController.ReleasePause(_pauseToken);
        _inputToken = 0;
        _pauseToken = 0;
        _index.Clear();
    }
}
