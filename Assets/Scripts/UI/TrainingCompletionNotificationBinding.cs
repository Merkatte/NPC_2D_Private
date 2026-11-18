using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrainingCompletionNotificationBinding : MonoBehaviour
{
    [SerializeField] private DefenseBattlefield _battlefield;
    [SerializeField] private NotificationToastHUD _notifications;

    private void OnEnable()
    {
        if (!_battlefield || !_notifications)
        {
            Debug.LogError($"{nameof(TrainingCompletionNotificationBinding)} '{name}' needs a battlefield and notification HUD.", this);
            enabled = false;
            return;
        }
        _battlefield.TrainingCompleted += OnTrainingCompleted;
    }

    private void OnDisable()
    {
        if (_battlefield)
            _battlefield.TrainingCompleted -= OnTrainingCompleted;
    }

    private void OnTrainingCompleted(string soldierLabel)
    {
        _notifications.Enqueue(LocalizeKey.TrainingCompletion, soldierLabel);
    }
}
