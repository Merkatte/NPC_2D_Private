using UnityEngine;

public class NPCComponent : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    
    private NPCStat _stat;

    public Vector3 Position => MoveRoot.position;

    private Transform MoveRoot => _transform ? _transform : transform;

    public void Init(NPCStat stat)
    {
        _stat = stat;
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
