using System.Collections;
using UnityEngine;

public sealed class CropVisualAnimator : MonoBehaviour
{
    private const int BaseLayerIndex = 0;

    private static readonly int AppearStateHash = Animator.StringToHash("Base Layer.Appear");
    private static readonly int DisappearStateHash = Animator.StringToHash("Base Layer.Disappear");
    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Leaf Sway");

    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    public bool TryConfigure(RuntimeAnimatorController controller)
    {
        if (!_animator)
            return ReportConfigurationFailure("missing Animator reference");

        if (!_spriteRenderer)
            return ReportConfigurationFailure("missing SpriteRenderer reference");

        if (!controller)
            return ReportConfigurationFailure("crop definition has no visual controller");

        if (_animator.runtimeAnimatorController != controller)
        {
            _animator.runtimeAnimatorController = controller;
            _isConfigured = false;
        }

        if (!_animator.HasState(BaseLayerIndex, AppearStateHash) ||
            !_animator.HasState(BaseLayerIndex, DisappearStateHash) ||
            !_animator.HasState(BaseLayerIndex, IdleStateHash))
        {
            return ReportConfigurationFailure("Animator Controller requires Appear, Disappear, and Leaf Sway states");
        }

        _isConfigured = true;
        return true;
    }

    public void ShowImmediate(Sprite sprite)
    {
        if (!_isConfigured || !sprite)
        {
            ReportConfigurationFailure("cannot show an unconfigured or missing crop sprite");
            HideImmediate();
            return;
        }

        ResetAnimatorPose();
        _spriteRenderer.sprite = sprite;
        _spriteRenderer.enabled = true;
        _animator.Play(IdleStateHash, BaseLayerIndex, 0f);
        _animator.Update(0f);
    }

    public void HideImmediate()
    {
        ResetAnimatorPose();

        if (_spriteRenderer)
            _spriteRenderer.enabled = false;
    }

    public IEnumerator PlayStageTransition(Sprite nextSprite)
    {
        if (!_isConfigured || !nextSprite)
        {
            ReportConfigurationFailure("cannot transition to an unconfigured or missing crop sprite");
            yield break;
        }

        _spriteRenderer.enabled = true;
        yield return PlayStateToCompletion(DisappearStateHash);

        _spriteRenderer.sprite = nextSprite;
        yield return PlayStateToCompletion(AppearStateHash);
    }

    public IEnumerator PlayHarvestDisappear()
    {
        if (!_isConfigured)
        {
            HideImmediate();
            yield break;
        }

        _spriteRenderer.enabled = true;
        yield return PlayStateToCompletion(DisappearStateHash);
        _spriteRenderer.enabled = false;
    }

    private IEnumerator PlayStateToCompletion(int stateHash)
    {
        if (!_animator || !_animator.isActiveAndEnabled)
            yield break;

        _animator.Play(stateHash, BaseLayerIndex, 0f);
        _animator.Update(0f);

        while (_animator && _animator.isActiveAndEnabled)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);
            if (state.fullPathHash != stateHash || (!state.loop && state.normalizedTime >= 1f))
                yield break;

            yield return null;
        }
    }

    private void ResetAnimatorPose()
    {
        if (!_animator || !_animator.runtimeAnimatorController)
            return;

        _animator.Rebind();
        _animator.Update(0f);
    }

    private bool ReportConfigurationFailure(string reason)
    {
        _isConfigured = false;

        if (!_hasLoggedConfigurationFailure)
        {
            Debug.LogError($"CropVisualAnimator '{name}': {reason}.", this);
            _hasLoggedConfigurationFailure = true;
        }

        return false;
    }
}
