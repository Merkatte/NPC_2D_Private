using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NotificationToastHUD : MonoBehaviour
{
    private const int MaxVisible = 3;
    private const float ToastLifetime = 4f;
    private const float SlideDuration = 0.18f;
    private const float GlideDistance = 32f;

    [SerializeField] private RectTransform _toastRoot;
    [SerializeField] private LocalizeText _toastTemplate;

    private sealed class Toast
    {
        public readonly LocalizeText Label;
        public readonly RectTransform Rect;
        public readonly CanvasGroup Group;
        public float ExpiresAt;
        public bool IsExiting;
        private Tween _motion;

        public bool IsMoving => _motion.IsActive();

        public Toast(LocalizeText label)
        {
            Label = label;
            Rect = (RectTransform)label.transform;
            if (!label.TryGetComponent(out CanvasGroup group))
                group = label.gameObject.AddComponent<CanvasGroup>();
            Group = group;
        }

        public void Play(Tween motion)
        {
            StopMotion();
            // The HUD polls this handle after completion, so it must never refer to a recycled tween.
            _motion = motion.SetUpdate(true).SetAutoKill(true).SetRecyclable(false);
        }

        public void StopMotion()
        {
            _motion?.Kill(false);
            _motion = null;
        }

        public void Glide(float targetX, float alpha, Ease ease)
        {
            Sequence glide = DOTween.Sequence();
            glide.Append(Rect.DOAnchorPosX(targetX, SlideDuration).SetEase(ease));
            glide.Join(Group.DOFade(alpha, SlideDuration).SetEase(Ease.InOutSine));
            Play(glide);
        }
    }

    private readonly Queue<(LocalizeKey Key, object[] Args)> _pending = new Queue<(LocalizeKey, object[])>();
    private readonly List<Toast> _visible = new List<Toast>(MaxVisible);
    private readonly Stack<Toast> _unused = new Stack<Toast>(MaxVisible);
    private RectTransform _templateRect;

    private void Awake()
    {
        _templateRect = _toastTemplate ? _toastTemplate.transform as RectTransform : null;
        if (!_toastRoot || !_templateRect || _templateRect.rect.height <= 0f)
        {
            Debug.LogError($"{nameof(NotificationToastHUD)} '{name}' needs a toast root and a positive-height toast template.", this);
            enabled = false;
            return;
        }
        _toastTemplate.gameObject.SetActive(false);
    }

    public void Enqueue(LocalizeKey key, params object[] args)
    {
        if (isActiveAndEnabled)
            _pending.Enqueue((key, args == null ? null : (object[])args.Clone()));
    }

    private void OnDisable()
    {
        _pending.Clear();
        while (_visible.Count > 0) ReturnToPool(_visible.Count - 1);
    }

    private void OnDestroy()
    {
        while (_unused.Count > 0)
        {
            Toast row = _unused.Pop();
            if (row.Label) Destroy(row.Label.gameObject);
        }
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        bool moving = false;
        for (int i = _visible.Count - 1; i >= 0; --i)
        {
            Toast row = _visible[i];
            if (row.IsExiting && !row.IsMoving)
            {
                ReturnToPool(i);
                continue;
            }
            if (!row.IsExiting && now >= row.ExpiresAt) StartExit(row);
            moving |= row.IsMoving;
        }

        if (_pending.Count == 0 || moving) return;
        if (_visible.Count == MaxVisible)
        {
            StartExit(_visible[_visible.Count - 1]);
            return;
        }
        if (SlideRowsDown()) RevealNext(now);
    }

    private void RevealNext(float now)
    {
        var notification = _pending.Dequeue();
        Toast row = _unused.Count > 0 ? _unused.Pop() : new Toast(Instantiate(_toastTemplate, _toastRoot));
        Vector2 rest = _templateRect.anchoredPosition;
        row.Rect.anchoredPosition = rest + Vector2.left * GlideDistance;
        row.Group.alpha = 0f;
        row.Label.SetFormattedKey(notification.Key, notification.Args);
        row.Label.gameObject.SetActive(true);
        row.ExpiresAt = now + ToastLifetime;
        _visible.Insert(0, row);
        row.Glide(rest.x, 1f, Ease.OutSine);
    }

    private bool SlideRowsDown()
    {
        float spacing = _templateRect.rect.height;
        bool complete = true;
        for (int i = 0; i < _visible.Count; ++i)
        {
            Toast row = _visible[i];
            float targetY = _templateRect.anchoredPosition.y - spacing * (i + 1);
            if (Mathf.Approximately(row.Rect.anchoredPosition.y, targetY)) continue;
            row.Play(row.Rect.DOAnchorPosY(targetY, SlideDuration).SetEase(Ease.InOutSine));
            complete = false;
        }
        return complete;
    }

    private void StartExit(Toast row)
    {
        row.IsExiting = true;
        row.Glide(_templateRect.anchoredPosition.x + GlideDistance, 0f, Ease.InSine);
    }

    private void ReturnToPool(int index)
    {
        Toast row = _visible[index];
        _visible.RemoveAt(index);
        row.StopMotion();
        row.IsExiting = false;
        row.ExpiresAt = 0f;
        if (row.Label) row.Label.gameObject.SetActive(false);
        _unused.Push(row);
    }
}
