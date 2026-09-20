using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Input adapter: only source/service contracts and category priority are known here.
// Collider and IHoverInfoSource components must share a GameObject.
public class PointerHoverRouter : MonoBehaviour
{
    private const float MinimumPollInterval = 0.02f;
    private const float SourceRetryInterval = 0.5f;

    [SerializeField] private Camera _worldCamera;
    [SerializeField] private MonoBehaviour _uiServiceSource;
    [SerializeField] private LayerMask _hoverableMask;
    [SerializeField] private HoverType _priorityHoverType = HoverType.NPCMessage;
    [SerializeField, Min(MinimumPollInterval)] private float _pollInterval = 0.05f;

    private sealed class SourceCache
    {
        public IHoverInfoSource[] Sources;
        public float NextResolutionTime;
    }

    private readonly List<Collider2D> _hits = new List<Collider2D>();
    private readonly Dictionary<Collider2D, SourceCache> _sources = new Dictionary<Collider2D, SourceCache>();
    private readonly List<Collider2D> _expiredColliders = new List<Collider2D>();
    private ContactFilter2D _filter;
    private IUIService _uiService;
    private float _nextPollTime;
    private IHoverInfoSource _activeSource;
    private HoverType _activeHoverType;

    private void Awake()
    {
        _uiService = _uiServiceSource as IUIService;
        if (!_worldCamera || _uiService == null)
        {
            Debug.LogError($"PointerHoverRouter '{name}': assign _worldCamera and _uiServiceSource implementing IUIService.", this);
            enabled = false;
            return;
        }
        // Preserve each scene's existing farm mask while including Friendly NPCs.
        _filter.SetLayerMask(_hoverableMask.value | LayerMask.GetMask("Friendly"));
        _filter.useTriggers = Physics2D.queriesHitTriggers;
        if (_filter.layerMask.value == 0)
            Debug.LogError($"PointerHoverRouter '{name}': no hover layers are configured.", this);
    }

    private void OnDisable()
    {
        ClearActiveHover();
        _sources.Clear();
        _hits.Clear();
        _expiredColliders.Clear();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextPollTime)
            return;
        _nextPollTime = Time.unscaledTime + Mathf.Max(MinimumPollInterval, _pollInterval);

        Mouse mouse = Mouse.current;
        if (mouse == null || !_worldCamera || !_uiServiceSource)
        {
            ClearActiveHover();
            return;
        }

        Vector2 worldPosition = _worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        Physics2D.OverlapPoint(worldPosition, _filter, _hits);
        RefreshSources();

        // Try the configured category first, then all remaining usable sources.
        // An invalid/disabled foreground collider cannot mask a usable farm.
        IHoverInfoSource candidate = FindUsableSource(true) ?? FindUsableSource(false);
        if (candidate == null)
        {
            ClearActiveHover();
            return;
        }
        if (!ReferenceEquals(candidate, _activeSource))
            ClearActiveHover();

        // Views can close themselves while their collider stays under the pointer.
        // UIManager treats an already-visible source as an idempotent success.
        if (_uiService.TryShow(candidate.HoverType, candidate))
        {
            _activeSource = candidate;
            _activeHoverType = candidate.HoverType;
        }
        else
            ClearActiveHover();
    }

    private void RefreshSources()
    {
        _expiredColliders.Clear();
        foreach (KeyValuePair<Collider2D, SourceCache> pair in _sources)
            if (!pair.Key || !_hits.Contains(pair.Key))
                _expiredColliders.Add(pair.Key);
        for (int i = 0; i < _expiredColliders.Count; ++i)
            _sources.Remove(_expiredColliders[i]);

        for (int i = 0; i < _hits.Count; ++i)
        {
            Collider2D hit = _hits[i];
            if (!hit)
                continue;
            if (!_sources.TryGetValue(hit, out SourceCache cache))
            {
                cache = new SourceCache();
                _sources.Add(hit, cache);
            }
            if (cache.Sources != null && (!NeedsResolution(cache.Sources) || Time.unscaledTime < cache.NextResolutionTime))
                continue;
            cache.Sources = hit.GetComponents<IHoverInfoSource>();
            cache.NextResolutionTime = Time.unscaledTime + SourceRetryInterval;
        }
    }

    private IHoverInfoSource FindUsableSource(bool priorityOnly)
    {
        for (int i = 0; i < _hits.Count; ++i)
        {
            Collider2D hit = _hits[i];
            if (!hit || !hit.isActiveAndEnabled || !_sources.TryGetValue(hit, out SourceCache cache))
                continue;
            foreach (IHoverInfoSource source in cache.Sources)
            {
                if (source == null || !source.Owner || source.HoverType == HoverType.None
                    || (source.HoverType == _priorityHoverType) != priorityOnly)
                    continue;
                if (source.Owner is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                    continue;
                if (source.TryGetHoverInfo(out _))
                    return source;
            }
        }
        return null;
    }

    private static bool NeedsResolution(IHoverInfoSource[] sources)
    {
        if (sources.Length == 0)
            return true;
        for (int i = 0; i < sources.Length; ++i)
            if (sources[i] == null || !sources[i].Owner)
                return true;
        return false;
    }

    private void ClearActiveHover()
    {
        if (_activeSource == null)
            return;
        if (_uiServiceSource)
            _uiService.TryHide(_activeHoverType, _activeSource);
        _activeSource = null;
    }

    private void OnValidate()
    {
        _pollInterval = Mathf.Max(MinimumPollInterval, _pollInterval);
    }
}
