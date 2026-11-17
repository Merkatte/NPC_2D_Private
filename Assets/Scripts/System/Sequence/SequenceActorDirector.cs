using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class SequenceActorDirector : SequenceAction
{
    private sealed class Snapshot
    {
        internal Transform Root;
        internal Vector3 Position;
        internal bool Active;
        internal Animator Animator;
        internal AnimatorUpdateMode Mode;
        internal float Speed;
        internal SpriteRenderer Renderer;
        internal bool FlipX;
    }
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private readonly List<Snapshot> _snapshots = new List<Snapshot>();
    private Tween _tween;
    private Animator _movingAnimator;
    private Transform _movingRoot;
    private Action<bool> _completed;

    public override bool TryBegin(SequenceStep step, SequenceBindings bindings, Action<bool> completed)
    {
        if (!isActiveAndEnabled || _tween != null || !bindings ||
            !bindings.TryGetActor(step.ActorId, out Transform root, out Animator animator, out _)) return false;
        if (!bindings.TryGetActorRenderer(step.ActorId, out SpriteRenderer renderer)) return false;
        Vector3 position = default;
        if (step.ActorCommand == SequenceActorCommand.Walk && !bindings.TryGetTarget(step.TargetId, out position)) return false;
        Snapshot snapshot = _snapshots.Find(value => value.Root == root);
        if (snapshot == null)
        {
            snapshot = new Snapshot { Root = root, Position = root.position, Active = root.gameObject.activeSelf,
                Animator = animator, Mode = animator.updateMode, Speed = animator.GetFloat(SpeedHash), Renderer = renderer, FlipX = renderer.flipX };
            _snapshots.Add(snapshot);
        }
        renderer.flipX = step.FaceLeft;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (step.ActorCommand != SequenceActorCommand.Walk)
        {
            root.gameObject.SetActive(step.ActorCommand == SequenceActorCommand.Show);
            completed(true);
            return true;
        }
        root.gameObject.SetActive(true);
        animator.SetFloat(SpeedHash, 1f);
        _movingAnimator = animator;
        _movingRoot = root;
        _completed = completed;
        _tween = root.DOMove(position, step.Duration).SetEase(Ease.Linear).SetUpdate(true)
            .OnComplete(() => { _tween = null; StopWalking(); completed(true); });
        return true;
    }

    private void StopWalking()
    {
        if (_movingAnimator) _movingAnimator.SetFloat(SpeedHash, 0f);
        _movingAnimator = null;
        _movingRoot = null;
        _completed = null;
    }
    private void Update()
    {
        if (_completed == null || (_movingRoot && _movingRoot.gameObject.activeInHierarchy && _movingAnimator && _movingAnimator.isActiveAndEnabled)) return;
        Action<bool> completed = _completed;
        Cancel();
        completed(false);
    }
    public override void Cancel() { _tween?.Kill(); _tween = null; StopWalking(); }
    public void RestoreActors()
    {
        Cancel();
        foreach (Snapshot snapshot in _snapshots)
        {
            if (snapshot.Animator)
            { snapshot.Animator.SetFloat(SpeedHash, snapshot.Speed); snapshot.Animator.updateMode = snapshot.Mode; }
            if (snapshot.Renderer) snapshot.Renderer.flipX = snapshot.FlipX;
            if (!snapshot.Root) continue;
            snapshot.Root.position = snapshot.Position;
            snapshot.Root.gameObject.SetActive(snapshot.Active);
        }
        _snapshots.Clear();
    }
    private void OnDisable() => RestoreActors();
}
