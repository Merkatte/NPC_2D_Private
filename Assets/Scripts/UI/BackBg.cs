using DG.Tweening;
using UnityEngine;

/// <summary>
/// Opaque backdrop shown behind popups. Owns only its own fade in/out — UIManager decides when
/// any popup is open and calls Show()/Hide() accordingly.
/// </summary>
public sealed class BackBg : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.2f;

    private Tween _fadeTween;

    public void Show()
    {
        if (!_canvasGroup)
            return;

        _fadeTween?.Kill();
        gameObject.SetActive(true);
        _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration);
    }

    public void Hide()
    {
        if (!_canvasGroup)
        {
            gameObject.SetActive(false);
            return;
        }

        _fadeTween?.Kill();
        _fadeTween = _canvasGroup.DOFade(0f, _fadeOutDuration)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
