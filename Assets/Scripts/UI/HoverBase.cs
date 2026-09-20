using UnityEngine;

public abstract class HoverBase : MonoBehaviour
{
    private const float MinimumRefreshInterval = 0.02f;

    [SerializeField] private HoverType _hoverType;
    [SerializeField, Min(MinimumRefreshInterval)] private float _refreshInterval = 0.1f;

    private IHoverInfoSource _source;
    private float _nextRefreshTime;

    public HoverType HoverType => _hoverType;
    public bool IsVisible => isActiveAndEnabled && HasUsableSource(_source);

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
        if (!IsVisible)
            return false;
        OnShown();
        return true;
    }

    internal bool IsOwnedBy(IHoverInfoSource source)
        => ReferenceEquals(_source, source);

    internal void HideCurrent()
    {
        if (_source == null)
            return;

        _source = null;
        OnBeforeHide();
        BeginHide();
    }

    internal void HideImmediately()
    {
        _source = null;
        OnBeforeHide();
        CompleteHide();
    }

    protected virtual void BeginHide()
    {
        CompleteHide();
    }

    protected void CompleteHide()
    {
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
        if (_source == null || Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + Mathf.Max(MinimumRefreshInterval, _refreshInterval);

        if (!TryReadInfo(_source, out HoverInfo info))
        {
            HideCurrent();
            return;
        }

        ApplyInfo(info);
    }

    protected virtual void OnDisable()
    {
        _source = null;
    }

    private static bool TryReadInfo(IHoverInfoSource source, out HoverInfo info)
    {
        info = default;
        return HasUsableSource(source) && source.TryGetHoverInfo(out info);
    }

    private static bool HasUsableSource(IHoverInfoSource source)
    {
        if (source == null || !source.Owner)
            return false;
        if (source.Owner is Behaviour behaviour)
            return behaviour.isActiveAndEnabled;
        if (source.Owner is Component component)
            return component.gameObject.activeInHierarchy;
        return true;
    }

    private void OnValidate()
    {
        _refreshInterval = Mathf.Max(MinimumRefreshInterval, _refreshInterval);
    }
}
