using UnityEngine;
using UnityEngine.UI;

public class FarmGaugeHover : HoverBase
{
    [SerializeField] private Image _progressFill;
    [SerializeField] private Camera _worldCamera;

    private bool _isConfigured;

    private void Awake()
    {
        _isConfigured = _progressFill && _worldCamera;

        if (!_isConfigured)
            Debug.LogError($"FarmGaugeHover '{name}': missing _progressFill or _worldCamera.", this);
    }

    protected override void ApplyInfo(HoverInfo info)
    {
        if (!_isConfigured)
            return;

        _progressFill.gameObject.SetActive(info.HasProgress);
        if (info.HasProgress)
            _progressFill.fillAmount = info.NormalizedProgress;

        Vector3 screenPosition = _worldCamera.WorldToScreenPoint(info.AnchorPosition);
        ((RectTransform)transform).position = screenPosition;
    }
}
