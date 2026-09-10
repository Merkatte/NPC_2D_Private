using System.Collections.Generic;
using UnityEngine;

public class WorkerNPC : MonoBehaviour
{
    [SerializeField] private NPCComponent _component;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D[] _gameplayColliders;

    private IAction _currentAction;
    private NPCStat _stat;
    private NPCType _npcType;
    private BaseNPCActionSelector _selector;
    private Queue<IAction> _actionQueue;
    private bool _isInitialized;

    private bool _isSpawnPresentationActive;
    private bool _wasRigidbodySimulated;
    private bool[] _wereCollidersEnabled;

    private void Awake()
    {
        _wereCollidersEnabled = new bool[_gameplayColliders != null ? _gameplayColliders.Length : 0];
    }

    /// <summary>
    /// Called by NPCManager.TryReserveWorker right after renting from the pool, before this worker
    /// is Init'd. WorkerPool.OnGetWorker already SetActive(true)'d the GameObject, so its
    /// Collider2D would otherwise participate in gameplay (e.g. CombatPerception's ProximitySensor2D
    /// on the Sensor child) while it's still just falling through the air with no stat/selector.
    /// SpriteRenderer/Animator are left alone so the drop presentation stays visible.
    /// </summary>
    public void BeginSpawnPresentation()
    {
        if (_isSpawnPresentationActive)
        {
            return;
        }

        _isSpawnPresentationActive = true;

        if (_rigidbody)
        {
            _wasRigidbodySimulated = _rigidbody.simulated;
            _rigidbody.simulated = false;
        }

        for (int i = 0; i < _gameplayColliders.Length; ++i)
        {
            Collider2D collider = _gameplayColliders[i];
            if (!collider)
            {
                continue;
            }

            _wereCollidersEnabled[i] = collider.enabled;
            collider.enabled = false;
        }
    }

    /// <summary>
    /// Restores whatever physics/collider state BeginSpawnPresentation captured, rather than
    /// unconditionally turning everything back on — some other path may have already had a
    /// collider disabled. Called by NPCManager.CommitReservation right before Init and by
    /// CancelReservation right before returning the worker to the pool. Idempotent so a defensive
    /// call from OnDisable never double-restores.
    /// </summary>
    public void CompleteSpawnPresentation()
    {
        if (!_isSpawnPresentationActive)
        {
            return;
        }

        if (_rigidbody)
        {
            _rigidbody.simulated = _wasRigidbodySimulated;
        }

        for (int i = 0; i < _gameplayColliders.Length; ++i)
        {
            Collider2D collider = _gameplayColliders[i];
            if (!collider)
            {
                continue;
            }

            collider.enabled = _wereCollidersEnabled[i];
        }

        _isSpawnPresentationActive = false;
    }

    public void Init(NPCType npcType, NPCStat stat, BaseNPCActionSelector selector)
    {
        if (_isInitialized)
        {
            CancelAndReturnQueue();
        }

        if (_component)
        {
            _component.ResetRuntimeState();
        }

        _npcType = npcType;
        _stat = stat;
        _component.Init(_stat);
        _component.SetToolVisible(npcType == NPCType.Farmer);
        _selector = selector;

        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_currentAction == null)
        {
            AdvanceQueue();
            return;
        }

        _currentAction.Tick();

        switch (_currentAction.Result)
        {
            case ActionResult.Running:
                break;

            case ActionResult.Completed:
                _selector.ReturnAction(_currentAction);
                _currentAction = null;
                AdvanceQueue();
                break;

            case ActionResult.ReplanRequested:
            case ActionResult.Failed:
                CancelAndReturnQueue();
                AdvanceQueue();
                break;
        }
    }

    void OnDisable()
    {
        // Defensive: a reserved-but-not-yet-committed worker (not _isInitialized) can still be
        // disabled, e.g. pool cleanup. CompleteSpawnPresentation is idempotent, so this never
        // conflicts with NPCManager.CommitReservation/CancelReservation calling it themselves.
        CompleteSpawnPresentation();

        if (!_isInitialized)
        {
            return;
        }

        CancelAndReturnQueue();
        _selector = null;
        _stat = null;

        if (_component)
        {
            _component.ResetRuntimeState();
        }

        _isInitialized = false;
    }

    private void AdvanceQueue()
    {
        if (_actionQueue == null || _actionQueue.Count == 0)
        {
            _actionQueue = _selector.RequestNewActionQueue(_stat, _npcType, _component);

            if (_actionQueue == null || _actionQueue.Count == 0)
            {
                _actionQueue = null;
                return;
            }
        }

        _currentAction = _actionQueue.Dequeue();
        _currentAction.Start();
    }

    private void CancelAndReturnQueue()
    {
        if (_currentAction != null)
        {
            IAction action = _currentAction;
            _currentAction = null;
            action.Stop();
            _selector.ReturnAction(action);
        }

        if (_actionQueue == null)
        {
            return;
        }

        while (_actionQueue.Count > 0)
        {
            IAction action = _actionQueue.Dequeue();
            action.Stop();
            _selector.ReturnAction(action);
        }

        _actionQueue = null;
    }
}
