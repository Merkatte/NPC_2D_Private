using UnityEngine;

// Runtime navigation obstacle. Collider enabled state is deliberately not its topology state.
public sealed class NavigationObstacle2D : MonoBehaviour
{
    [SerializeField] private TilemapNavigation _navigation;
    [SerializeField] private BoxCollider2D _bounds;
    [SerializeField] private bool _blocksFriendly = true;
    [SerializeField] private bool _blocksEnemy = true;
    private bool _isBlocked = true;
    public bool IsBlocked => _isBlocked && isActiveAndEnabled;
    public BoxCollider2D Bounds => _bounds;
    public bool Blocks(NavigationAccess access) => IsBlocked &&
        (access == NavigationAccess.Enemy ? _blocksEnemy : _blocksFriendly);
    private void OnEnable() { if (_navigation) _navigation.RegisterObstacle(this); }
    private void OnDisable() { if (_navigation) _navigation.UnregisterObstacle(this); }
    public void SetBlocked(bool blocked)
    {
        if (_isBlocked == blocked) return;
        _isBlocked = blocked;
        if (_navigation) _navigation.InvalidateObstacles();
    }
}
