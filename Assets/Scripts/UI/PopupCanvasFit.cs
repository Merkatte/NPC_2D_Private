using UnityEngine;

[DisallowMultipleComponent]
public sealed class PopupCanvasFit : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _margin = 24f;
    private RectTransform _rect;
    private RectTransform _parent;
    private RectTransform _canvas;
    private Vector3 _authoredScale;

    private void Awake()
    {
        _rect = transform as RectTransform;
        _parent = transform.parent as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        _canvas = canvas ? canvas.rootCanvas.transform as RectTransform : null;
        _authoredScale = transform.localScale;
    }

    private void OnEnable() => Fit();
    private void LateUpdate() => Fit();

    private void Fit()
    {
        if (!_rect || !_parent || !_canvas) return;
        Vector3 size = _parent.InverseTransformVector(_canvas.TransformVector(_canvas.rect.size));
        float width = _rect.rect.width * Mathf.Abs(_authoredScale.x);
        float height = _rect.rect.height * Mathf.Abs(_authoredScale.y);
        if (width <= 0f || height <= 0f) return;
        float scale = Mathf.Clamp01(Mathf.Min((Mathf.Abs(size.x) - _margin * 2f) / width,
            (Mathf.Abs(size.y) - _margin * 2f) / height));
        // Position belongs to PopBase; the house drawer only changes its child rectangles.
        _rect.localScale = new Vector3(_authoredScale.x * scale, _authoredScale.y * scale, _authoredScale.z);
    }

    private void OnValidate()
        => _margin = float.IsNaN(_margin) || float.IsInfinity(_margin) ? 24f : Mathf.Max(0f, _margin);
}
