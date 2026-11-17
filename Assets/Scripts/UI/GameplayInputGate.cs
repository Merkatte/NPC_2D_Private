using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Scene-scoped input ownership. Release stays blocked for the rest of the input frame.
[DefaultExecutionOrder(-9000)]
public sealed class GameplayInputGate : MonoBehaviour
{
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private CanvasGroup[] _gameplayGroups;
    private readonly HashSet<int> _leases = new HashSet<int>();
    private bool[] _interactable;
    private bool[] _raycasts;
    private float[] _alphas;
    private int _nextToken;
    private int _releaseFrame = -1;
    private bool _hasSnapshot;

    public bool IsBlocked => _leases.Count > 0 || Time.frameCount <= _releaseFrame;

    public int Acquire()
    {
        if (!isActiveAndEnabled || _nextToken == int.MaxValue) return 0;
        if (!_hasSnapshot)
        {
            int count = _gameplayGroups == null ? 0 : _gameplayGroups.Length;
            _interactable = new bool[count];
            _raycasts = new bool[count];
            _alphas = new float[count];
            for (int i = 0; i < count; i++)
            {
                CanvasGroup group = _gameplayGroups[i];
                if (!group) continue;
                _interactable[i] = group.interactable;
                _raycasts[i] = group.blocksRaycasts;
                _alphas[i] = group.alpha;
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            _hasSnapshot = true;
        }
        int token = ++_nextToken;
        _leases.Add(token);
        if (_uiManager) _uiManager.HideAllImmediately();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        return token;
    }

    public void Release(int token)
    {
        if (!_leases.Remove(token) || _leases.Count > 0) return;
        _releaseFrame = Time.frameCount;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (_hasSnapshot && !IsBlocked) Restore();
    }

    private void Restore()
    {
        if (!_hasSnapshot) return;
        for (int i = 0; i < _interactable.Length; i++)
        {
            if (!_gameplayGroups[i]) continue;
            _gameplayGroups[i].interactable = _interactable[i];
            _gameplayGroups[i].blocksRaycasts = _raycasts[i];
            _gameplayGroups[i].alpha = _alphas[i];
        }
        _hasSnapshot = false;
    }

    private void OnDisable()
    {
        _leases.Clear();
        Restore();
    }
}
