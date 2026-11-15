using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Backdrop shown behind popups. Owns its fade and reports backdrop clicks; UIManager owns
/// popup selection and the close lifecycle.
/// </summary>
public sealed class BackBg : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.2f;

    private Tween _fadeTween;

    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || eventData == null || eventData.dragging
            || eventData.button != PointerEventData.InputButton.Left)
            return;

        Clicked?.Invoke();
    }

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
