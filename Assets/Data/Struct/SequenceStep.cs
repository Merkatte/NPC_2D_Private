using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum SequenceStepKind { Dialogue = 0, Choice = 1, Camera = 2, Actor = 3, ExternalAction = 4, End = 5 }
public enum DialoguePresentation { Bubble = 0, BottomPanel = 1 }
public enum SequenceActorCommand { Walk = 0, Show = 1, Hide = 2 }

[Serializable]
public sealed class SequenceChoice
{
    [SerializeField] private string _id;
    [SerializeField] private LocalizeKey _textKey;
    [SerializeField] private string _nextId;
    public string Id => _id;
    public LocalizeKey TextKey => _textKey;
    public string NextId => _nextId;
}

[Serializable]
public sealed class SequenceStep
{
    [SerializeField] private string _id;
    [SerializeField] private SequenceStepKind _kind;
    [SerializeField] private string _nextId;
    [SerializeField] private LocalizeKey _textKey;
    [SerializeField] private LocalizeKey _speakerKey;
    [SerializeField, Min(0f)] private float _autoAdvanceDelay = 3f;
    [SerializeField] private Ease _ease = Ease.InOutCubic;
    [SerializeField] private bool _faceLeft;
    [SerializeField] private DialoguePresentation _presentation;
    [SerializeField] private string _actorId;
    [SerializeField] private string _targetId;
    [SerializeField, Min(0f)] private float _duration = 0.8f;
    [SerializeField, Min(0.01f)] private float _orthographicSize = 5f;
    [SerializeField] private bool _restoreCamera;
    [SerializeField] private SequenceActorCommand _actorCommand;
    [SerializeField] private string _actionId;
    [SerializeField] private SequenceChoice[] _choices;
    public string Id => _id;
    public SequenceStepKind Kind => _kind;
    public string NextId => _nextId;
    public LocalizeKey TextKey => _textKey;
    public DialoguePresentation Presentation => _presentation;
    public LocalizeKey SpeakerKey => _speakerKey;
    public float AutoAdvanceDelay => _autoAdvanceDelay;
    public Ease Ease => _ease;
    public bool FaceLeft => _faceLeft;
    public string ActorId => _actorId;
    public string TargetId => _targetId;
    public float Duration => _duration;
    public float OrthographicSize => _orthographicSize;
    public bool RestoreCamera => _restoreCamera;
    public SequenceActorCommand ActorCommand => _actorCommand;
    public string ActionId => _actionId;
    public IReadOnlyList<SequenceChoice> Choices => _choices;
    public bool IsValid => Enum.IsDefined(typeof(SequenceStepKind), _kind) &&
        Enum.IsDefined(typeof(DialoguePresentation), _presentation) &&
        Enum.IsDefined(typeof(SequenceActorCommand), _actorCommand) &&
        !float.IsNaN(_autoAdvanceDelay) && !float.IsInfinity(_autoAdvanceDelay) && _autoAdvanceDelay >= 0f &&
        !float.IsNaN(_duration) && !float.IsInfinity(_duration) && _duration >= 0f &&
        !float.IsNaN(_orthographicSize) && !float.IsInfinity(_orthographicSize) && _orthographicSize > 0f &&
        ((_kind != SequenceStepKind.Dialogue && _kind != SequenceStepKind.Choice) || (Enum.IsDefined(typeof(LocalizeKey), _textKey) && Enum.IsDefined(typeof(LocalizeKey), _speakerKey))) &&
        (_kind != SequenceStepKind.Choice || (_choices != null && _choices.Length > 0));
}
