using UnityEngine;

public abstract class HoverBase : MonoBehaviour
{
    private const float MinimumRefreshInterval = 0.02f;

    [SerializeField] private HoverType _hoverType;
    [SerializeField, Min(MinimumRefreshInterval)] private float _refreshInterval = 0.1f;

    private IHoverInfoSource _source;
    private float _nextRefreshTime;

    public HoverType HoverType => _hoverType;
    public bool IsVisible => gameObject.activeSelf && HasUsableSource(_source);

    internal bool TryShow(IHoverInfoSource source)
    {
        if (!TryReadInfo(source, out HoverInfo info))
            return false;

        _source = source;
        _nextRefreshTime = Time.unscaledTime + _refreshInterval;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        transform.SetAsLastSibling();
        ApplyInfo(info);
        OnShown();
        return true;
    }

    internal bool IsOwnedBy(IHoverInfoSource source)
        => ReferenceEquals(_source, source);

    internal void HideCurrent()
    {
        if (_source == null && !gameObject.activeSelf)
            return;

        _source = null;
        OnBeforeHide();
        gameObject.SetActive(false);
        OnHidden();
    }

    protected abstract void ApplyInfo(HoverInfo info);

    protected virtual void OnShown()
    {
    }

    protected virtual void OnBeforeHide()
    {
    }

    protected virtual void OnHidden()
    {
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + Mathf.Max(MinimumRefreshInterval, _refreshInterval);

        if (!TryReadInfo(_source, out HoverInfo info))
        {
            HideCurrent();
            return;
        }

        ApplyInfo(info);
    }

    private static bool TryReadInfo(IHoverInfoSource source, out HoverInfo info)
    {
        info = default;
        return HasUsableSource(source) && source.TryGetHoverInfo(out info);
    }

    private static bool HasUsableSource(IHoverInfoSource source)
        => source != null && source.Owner;

    private void OnValidate()
    {
        _refreshInterval = Mathf.Max(MinimumRefreshInterval, _refreshInterval);
    }
}
