using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NPCMessageSource : MonoBehaviour, IHoverInfoSource
{
    [SerializeField] private WorkerNPC _worker;
    [SerializeField] private NPCThoughtCatalog _catalog;
    [SerializeField] private Transform _messageAnchor;
    [SerializeField] private MonoBehaviour _randomSource;

    private readonly NPCMessageState _state = new NPCMessageState();
    private readonly List<LocalizeKey> _candidates = new List<LocalizeKey>();
    private IRandomSource _random;
    private bool _hasWorker;
    private bool _hasResolvedReferences;

    public Object Owner => this;
    public HoverType HoverType => HoverType.NPCMessage;

    private void Awake() => EnsureReferences();

    private void EnsureReferences()
    {
        if (_hasResolvedReferences)
            return;
        _hasResolvedReferences = true;
        if (!_worker)
            TryGetComponent(out _worker);
        _hasWorker = _worker;
        _random = _randomSource as IRandomSource;
        if (_hasWorker && (!_catalog || _random == null || !_randomSource
            || _randomSource.gameObject != gameObject))
        {
            Debug.LogError($"NPCMessageSource '{name}': assign _catalog and a dedicated local _randomSource implementing IRandomSource.", this);
            _random = null;
        }
        if (!_messageAnchor)
            Debug.LogError($"NPCMessageSource '{name}': missing _messageAnchor; hover display is unavailable.", this);
    }

    public bool TryGetMessage(out LocalizeKey key)
    {
        key = default;
        if (!TrySynchronize(out IStatView stat, out ActionType? actionType))
            return false;

        _candidates.Clear();
        if (stat != null && _catalog && _randomSource && _random != null)
        {
            NPCThoughtSelector.CollectCandidates(stat, actionType, _catalog.ActionThoughts,
                _catalog.NeedThoughts, _catalog.FallbackKey, _candidates, _catalog.StrikeThoughts);
        }
        float duration = _catalog ? _catalog.HoldDuration : NPCThoughtCatalog.DefaultHoldDuration;
        return _state.TryGetMessage(_candidates, duration, Time.unscaledTimeAsDouble, _random, out key);
    }

    public bool TrySetFixedMessage(LocalizeKey key, out uint token)
    {
        token = 0;
        return TrySynchronize(out _, out _) && _state.TrySetFixedMessage(key, out token);
    }

    public bool TryClearFixedMessage(uint token)
        => TrySynchronize(out _, out _) && _state.TryClearFixedMessage(token);

    public bool TryGetHoverInfo(out HoverInfo info)
    {
        info = default;
        if (!_messageAnchor || !TryGetMessage(out LocalizeKey key))
            return false;
        info = new HoverInfo(null, null, _messageAnchor.position,
            messageKey: key, trackingAnchor: _messageAnchor);
        return true;
    }

    private bool TrySynchronize(out IStatView stat, out ActionType? actionType)
    {
        stat = null;
        actionType = null;
        if (!isActiveAndEnabled)
            return false;
        // A caller's Awake can precede this component's Awake.
        EnsureReferences();
        if (_hasWorker && (!_worker || !_worker.TryGetCurrentState(out stat, out actionType)))
        {
            _state.Synchronize(null);
            return false;
        }
        _state.Synchronize(stat);
        return true;
    }

    private void OnDisable()
    {
        _state.Reset();
        _candidates.Clear();
    }
}
