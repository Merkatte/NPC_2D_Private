using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConstructionPriorityHUD : MonoBehaviour
{
    private const int MaxVisible = 3;
    private const float ToastLifetime = 4f;
    private const float SlideDuration = 0.18f;

    [SerializeField] private BuildingPlotRegistry _registry;
    [SerializeField] private GameObject _priorityRow;
    [SerializeField] private LocalizeText _priorityLabel;
    [SerializeField] private RectTransform _toastRoot;
    [SerializeField] private LocalizeText _toastTemplate;

    private sealed class Toast
    {
        public readonly LocalizeText Label;
        public readonly RectTransform Rect;
        public float ExpiresAt;

        public Toast(LocalizeText label)
        {
            Label = label;
            Rect = (RectTransform)label.transform;
        }
    }

    private readonly Queue<ConstructionProjectInfo> _pending = new Queue<ConstructionProjectInfo>();
    private readonly List<Toast> _visible = new List<Toast>(MaxVisible);
    private readonly Stack<Toast> _unused = new Stack<Toast>(MaxVisible);
    private RectTransform _templateRect;

    private void Awake()
    {
        _templateRect = _toastTemplate ? _toastTemplate.transform as RectTransform : null;
        if (!_registry || !_priorityRow || !_priorityLabel || !_toastRoot || !_templateRect || _templateRect.rect.height <= 0f)
        {
            Debug.LogError($"{nameof(ConstructionPriorityHUD)} '{name}' needs a plot registry, priority row and label, toast root and a positive-height toast template.", this);
            enabled = false;
            return;
        }
        _toastTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _registry.PrioritiesChanged += RefreshPriority;
        _registry.ProjectCompleted += EnqueueCompletion;
        RefreshPriority();
    }

    private void OnDisable()
    {
        if (_registry)
        {
            _registry.PrioritiesChanged -= RefreshPriority;
            _registry.ProjectCompleted -= EnqueueCompletion;
        }
        _pending.Clear();
        while (_visible.Count > 0) HideOldest();
    }

    private void RefreshPriority()
    {
        if (_registry.Priorities.Count == 0)
        {
            _priorityRow.SetActive(false);
            return;
        }
        ConstructionProjectInfo first = _registry.Priorities[0];
        _priorityLabel.SetFormattedKey(LocalizeKey.ConstructionPriorityRank, 1,
            first.Definition.DisplayName, KindKey(first));
        _priorityRow.SetActive(true);
    }

    private void EnqueueCompletion(ConstructionProjectInfo project) => _pending.Enqueue(project);

    private void Update()
    {
        float now = Time.unscaledTime;
        while (_visible.Count > 0 && _visible[_visible.Count - 1].ExpiresAt <= now)
            HideOldest();
        if (_pending.Count == 0) return;
        if (_visible.Count == MaxVisible) HideOldest();
        if (!SlideRowsDown()) return;

        ConstructionProjectInfo project = _pending.Dequeue();
        Toast row = _unused.Count > 0 ? _unused.Pop() : new Toast(Instantiate(_toastTemplate, _toastRoot));
        row.Rect.anchoredPosition = _templateRect.anchoredPosition;
        row.Label.SetFormattedKey(LocalizeKey.ConstructionCompletion, project.Definition.DisplayName, KindKey(project));
        row.Label.gameObject.SetActive(true);
        row.ExpiresAt = now + ToastLifetime;
        _visible.Insert(0, row);
    }

    private bool SlideRowsDown()
    {
        float spacing = _templateRect.rect.height;
        float step = spacing * Time.unscaledDeltaTime / SlideDuration;
        bool complete = true;
        for (int i = 0; i < _visible.Count; ++i)
        {
            RectTransform row = _visible[i].Rect;
            Vector2 target = _templateRect.anchoredPosition + Vector2.down * (spacing * (i + 1));
            row.anchoredPosition = Vector2.MoveTowards(row.anchoredPosition, target, step);
            complete &= row.anchoredPosition == target;
        }
        return complete;
    }

    private void HideOldest()
    {
        int last = _visible.Count - 1;
        Toast row = _visible[last];
        _visible.RemoveAt(last);
        row.Label.gameObject.SetActive(false);
        _unused.Push(row);
    }

    private static LocalizeKey KindKey(ConstructionProjectInfo project) =>
        project.IsUpgrade ? LocalizeKey.ConstructionUpgrade : LocalizeKey.ConstructionNew;
}
