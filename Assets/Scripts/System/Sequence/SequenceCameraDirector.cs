using System;
using DG.Tweening;
using UnityEngine;

public sealed class SequenceCameraDirector : SequenceAction
{
    [SerializeField] private Camera _camera;
    private Vector3 _position;
    private Quaternion _rotation;
    private float _size;
    private bool _hasSnapshot;
    private Tween _tween;
    private Action<bool> _completed;

    public bool TryCapture()
    {
        if (!_camera || !_camera.orthographic || _hasSnapshot) return false;
        _position = _camera.transform.position;
        _rotation = _camera.transform.rotation;
        _size = _camera.orthographicSize;
        _hasSnapshot = true;
        return true;
    }

    public override bool TryBegin(SequenceStep step, SequenceBindings bindings, Action<bool> completed)
    {
        if (!_hasSnapshot || !_camera || _tween != null || !bindings) return false;
        Vector3 position = _position;
        if (!step.RestoreCamera && !bindings.TryGetTarget(step.TargetId, out position)) return false;
        if (!step.RestoreCamera) position.z = _camera.transform.position.z;
        float size = step.RestoreCamera ? _size : step.OrthographicSize;
        Quaternion rotation = step.RestoreCamera ? _rotation : _camera.transform.rotation;
        return TryMoveTo(position, size, step.Duration, step.Ease, rotation, completed);
    }

    public bool TryMoveTo(Vector3 position, float size, float duration, Ease ease, Quaternion rotation, Action<bool> completed)
    {
        if (!isActiveAndEnabled || !_camera || !_camera.orthographic || _tween != null || completed == null || duration < 0f || size <= 0f ||
            float.IsNaN(duration) || float.IsInfinity(duration) || float.IsNaN(size) || float.IsInfinity(size)) return false;
        if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z) ||
            !IsFinite(rotation.x) || !IsFinite(rotation.y) || !IsFinite(rotation.z) || !IsFinite(rotation.w)) return false;
        _completed = completed;
        _tween = DOTween.Sequence()
            .Join(_camera.transform.DOMove(position, duration).SetEase(Ease.Linear))
            .Join(_camera.transform.DORotateQuaternion(rotation, duration).SetEase(Ease.Linear))
            .Join(_camera.DOOrthoSize(size, duration).SetEase(Ease.Linear))
            .SetEase(ease).SetUpdate(true)
            .OnComplete(() => { _tween = null; _completed = null; completed(true); });
        return true;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private void Update()
    {
        if (_completed == null || _camera) return;
        Action<bool> completed = _completed;
        Cancel();
        completed(false);
    }
    public override void Cancel() { _tween?.Kill(); _tween = null; _completed = null; }
    public void RestoreSnapshot()
    {
        Cancel();
        if (_hasSnapshot && _camera)
        {
            _camera.transform.SetPositionAndRotation(_position, _rotation);
            _camera.orthographicSize = _size;
        }
        _hasSnapshot = false;
    }
    private void OnDisable() => RestoreSnapshot();
}
