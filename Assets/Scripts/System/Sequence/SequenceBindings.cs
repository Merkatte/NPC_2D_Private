using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SequenceBindings : MonoBehaviour
{
    [Serializable]
    private sealed class ActorBinding
    {
        [SerializeField] private string _id;
        internal string Id => _id;
        [SerializeField] private Transform _root;
        internal Transform Root => _root;
        [SerializeField] private Animator _animator;
        internal Animator Animator => _animator;
        [SerializeField] private Transform _messageAnchor;
        internal Transform MessageAnchor => _messageAnchor;
        [SerializeField] private SpriteRenderer _renderer;
        internal SpriteRenderer Renderer => _renderer;
    }
    [Serializable]
    private sealed class TargetBinding
    {
        [SerializeField] private string _id;
        internal string Id => _id;
        [SerializeField] private Transform _transform;
        internal Transform Transform => _transform;
        [SerializeField] private bool _usePosition;
        internal bool UsePosition => _usePosition;
        [SerializeField] private Vector3 _position;
        internal Vector3 Position => _position;
    }
    [Serializable]
    private sealed class ActionBinding
    {
        [SerializeField] private string _id;
        internal string Id => _id;
        [SerializeField] private SequenceAction _action;
        internal SequenceAction Action => _action;
    }
    [SerializeField] private ActorBinding[] _actors;
    [SerializeField] private TargetBinding[] _targets;
    [SerializeField] private ActionBinding[] _actions;

    public bool TryValidate()
    {
        var ids = new HashSet<string>();
        if (_actors != null) foreach (ActorBinding row in _actors)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id) || !ids.Add(row.Id) ||
                !row.Root || !row.Animator || !row.MessageAnchor || !row.Renderer) return false;
        }
        ids.Clear();
        if (_targets != null) foreach (TargetBinding row in _targets)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id) || !ids.Add(row.Id) ||
                (!row.UsePosition && !row.Transform)) return false;
            Vector3 position = row.UsePosition ? row.Position : row.Transform.position;
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z)) return false;
        }
        ids.Clear();
        if (_actions != null) foreach (ActionBinding row in _actions)
            if (row == null || string.IsNullOrWhiteSpace(row.Id) || !ids.Add(row.Id) || !row.Action) return false;
        return true;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public bool TryGetActor(string id, out Transform root, out Animator animator, out Transform anchor)
    {
        root = null; animator = null; anchor = null;
        if (_actors == null || string.IsNullOrEmpty(id)) return false;
        foreach (ActorBinding row in _actors)
            if (row != null && row.Id == id)
            { root = row.Root; animator = row.Animator; anchor = row.MessageAnchor; return root && animator && anchor; }
        return false;
    }


    public bool TryGetActorRenderer(string id, out SpriteRenderer renderer)
    {
        renderer = null;
        if (_actors == null) return false;
        foreach (ActorBinding row in _actors)
            if (row != null && row.Id == id) { renderer = row.Renderer; return renderer; }
        return false;
    }

    public bool TryGetTarget(string id, out Vector3 position)
    {
        position = default;
        if (_targets == null || string.IsNullOrEmpty(id)) return false;
        foreach (TargetBinding row in _targets)
            if (row != null && row.Id == id)
            { if (!row.UsePosition && !row.Transform) return false;
                position = row.UsePosition ? row.Position : row.Transform.position; return true; }
        return false;
    }

    public bool TryGetAction(string id, out SequenceAction action)
    {
        action = null;
        if (_actions == null || string.IsNullOrEmpty(id)) return false;
        foreach (ActionBinding row in _actions)
            if (row != null && row.Id == id) { action = row.Action; return action; }
        return false;
    }
}
