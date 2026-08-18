using UnityEngine;

public class NPCComponent : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private GuardPerception _guardPerception;

    private NPCStat _stat;
    private readonly GuardRuntimeState _guardRuntimeState = new GuardRuntimeState();

    public Vector3 Position => MoveRoot.position;

    // Null for roles (e.g. Farmer) that don't carry a combat sensor child. Callers must guard for null.
    public GuardPerception GuardPerception => _guardPerception;
    public GuardRuntimeState GuardRuntimeState => _guardRuntimeState;

    private Transform MoveRoot => _transform ? _transform : transform;

    public void Init(NPCStat stat)
    {
        _stat = stat;
    }

    /// <summary>
    /// Clears per-NPC runtime state before pool reuse or re-Init.
    /// </summary>
    public void ResetRuntimeState()
    {
        _guardRuntimeState.ClearTarget();
    }

    public void Move(Vector3 direction)
    {
        if (_stat == null)
        {
            return;
        }

        MoveRoot.position += direction * (_stat.GetMoveSpeed * Time.deltaTime);
    }

    public void Flip(bool isRight)
    {
        _spriteRenderer.flipX = isRight;
    }

    public void PlayAnim()
    {
        
    }

    public void PlayDOTweenAnim()
    {
        
    }

    public void EnableObject(bool isEnable)
    {
        _gameObject.SetActive(isEnable);
    }
}
