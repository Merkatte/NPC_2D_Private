using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConstructionPriorityHUD : MonoBehaviour
{
    [SerializeField] private BuildingPlotRegistry _registry;
    [SerializeField] private GameObject _priorityRow;
    [SerializeField] private LocalizeText _priorityLabel;
    [SerializeField] private NotificationToastHUD _notifications;

    private void Awake()
    {
        if (!_registry || !_priorityRow || !_priorityLabel || !_notifications)
        {
            Debug.LogError($"{nameof(ConstructionPriorityHUD)} '{name}' needs a plot registry, priority row and label, and notification HUD.", this);
            enabled = false;
        }
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

    private void EnqueueCompletion(ConstructionProjectInfo project)
    {
        _notifications.Enqueue(LocalizeKey.ConstructionCompletion, project.Definition.DisplayName, KindKey(project));
    }

    private static LocalizeKey KindKey(ConstructionProjectInfo project) =>
        project.IsUpgrade ? LocalizeKey.ConstructionUpgrade : LocalizeKey.ConstructionNew;
}
