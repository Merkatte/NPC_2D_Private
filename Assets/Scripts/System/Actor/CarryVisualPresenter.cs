using UnityEngine;

/// <summary>
/// Plays the carry-basket appear/disappear motion on its own Animator, separate from the main
/// NPC Animator so the one-shot cargo motion never shares a bound property with locomotion/tool
/// clips. NPCComponent.SetCarryVisible is the sole caller; nothing else should touch this Animator.
/// </summary>
public sealed class CarryVisualPresenter : MonoBehaviour
{
    private const int BaseLayerIndex = 0;

    private static readonly int HasCargoHash = Animator.StringToHash("HasCargo");
    private static readonly int HiddenStateHash = Animator.StringToHash("Hidden");

    [SerializeField] private Animator _animator;

    private bool _isVisible;
    private bool _hasLoggedConfigurationFailure;

    void Awake()
    {
        if (!_animator)
        {
            ReportConfigurationFailure("missing Animator reference");
            return;
        }

        if (!_animator.HasState(BaseLayerIndex, HiddenStateHash))
        {
            ReportConfigurationFailure("Animator Controller requires a Hidden state");
        }
    }

    /// <summary>
    /// WorkerInventory calls this on every accepted TryAdd, even while already visible.
    /// The equality guard is what keeps a second harvest from replaying the Show motion.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_isVisible == visible)
        {
            return;
        }

        _isVisible = visible;

        if (_animator)
        {
            _animator.SetBool(HasCargoHash, visible);
        }
    }

    /// <summary>
    /// Snaps straight to Hidden with no animation. Used on pool reuse so a leftover Hide motion
    /// never survives into the next NPC that borrows this instance.
    /// </summary>
    public void ResetImmediate()
    {
        _isVisible = false;

        if (!_animator)
        {
            return;
        }

        _animator.SetBool(HasCargoHash, false);
        _animator.Play(HiddenStateHash, BaseLayerIndex, 0f);
        _animator.Update(0f);
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
        {
            return;
        }

        Debug.LogError($"CarryVisualPresenter '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }
}
