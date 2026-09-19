using UnityEngine;

public sealed class TemporaryGameOverReporter : MonoBehaviour
{
    [SerializeField] private GuardPost _guardPost;
    private bool _hasReported;

    private void OnEnable()
    {
        if (!_guardPost)
        {
            Debug.LogError($"TemporaryGameOverReporter '{name}': missing _guardPost.", this);
            enabled = false;
            return;
        }
        _guardPost.Died += Report;
    }

    private void Start()
    {
        if (_guardPost && _guardPost.CurrentHealth <= 0f)
            Report();
    }

    private void OnDisable()
    {
        if (_guardPost)
            _guardPost.Died -= Report;
    }

    private void Report()
    {
        if (_hasReported)
            return;
        _hasReported = true;
        Debug.Log("GameOver");
    }
}
