using UnityEngine;
using UnityEngine.Serialization;

public class NPCComponent : MonoBehaviour
{
    private static readonly int SpeedParameterHash = Animator.StringToHash("Speed");
    private static readonly int IsInsideBuildingParameterHash = Animator.StringToHash("IsInsideBuilding");
    private static readonly int IsWorkingParameterHash = Animator.StringToHash("IsWorking");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");

    [SerializeField] private Transform _transform;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _gameObject;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] [FormerlySerializedAs("_guardPerception")] private CombatPerception _combatPerception;
    [SerializeField] private SpriteRenderer _toolRenderer;
    [SerializeField] private Transform _toolAnchor;

    // Farmer/Guard require a fully-parameterized Animator (Speed/IsInsideBuilding/IsWorking) and
    // CacheAnimatorParameters logs loudly if any is missing. Roles without art yet (e.g. Enemy)
    // leave _animator unassigned on purpose and set this false so spawning them doesn't spam errors.
    [SerializeField] private bool _requiresAnimator = true;

    private NPCStat _stat;
    private readonly CombatRuntimeState _combatRuntimeState = new CombatRuntimeState();
    private bool _hasSpeedParameter;
    private bool _hasInsideBuildingParameter;
    private bool _hasIsWorkingParameter;
    private bool _movedThisFrame;

    public Vector3 Position => MoveRoot.position;

    // Null for roles that don't carry a combat sensor child. Callers must guard for null.
    public CombatPerception CombatPerception => _combatPerception;
    public CombatRuntimeState CombatRuntimeState => _combatRuntimeState;

    private Transform MoveRoot => _transform ? _transform : transform;

    void Awake()
    {
        CacheAnimatorParameters();
    }

    void LateUpdate()
    {
        if (_animator && _hasSpeedParameter)
        {
            float speed = _movedThisFrame && _stat != null
                ? Mathf.Max(0f, _stat.GetMoveSpeed)
                : 0f;
            _animator.SetFloat(SpeedParameterHash, speed);
        }

        _movedThisFrame = false;
    }

    public void Init(NPCStat stat)
    {
        _stat = stat;
        ResetAnimationState();
    }

    /// <summary>
    /// Clears per-NPC runtime state before pool reuse or re-Init.
    /// </summary>
    public void ResetRuntimeState()
    {
        _combatRuntimeState.ClearTarget();
        _movedThisFrame = false;
        ResetAnimationState();
    }

    public void Move(Vector3 direction)
    {
        if (_stat == null)
        {
            return;
        }

        Vector3 displacement = direction * (_stat.GetMoveSpeed * Time.deltaTime);
        MoveRoot.position += displacement;
        _movedThisFrame |= displacement.sqrMagnitude > 0f;
    }

    public void Flip(bool isRight)
    {
        _spriteRenderer.flipX = isRight;

        if (_toolAnchor)
        {
            Vector3 anchorScale = _toolAnchor.localScale;
            anchorScale.x = isRight ? -Mathf.Abs(anchorScale.x) : Mathf.Abs(anchorScale.x);
            _toolAnchor.localScale = anchorScale;
        }
    }

    public void SetInsideBuilding(bool isInsideBuilding)
    {
        if (_animator && _hasInsideBuildingParameter)
        {
            _animator.SetBool(IsInsideBuildingParameterHash, isInsideBuilding);
        }
    }

    public void SetWorking(bool isWorking)
    {
        if (_animator && _hasIsWorkingParameter)
        {
            _animator.SetBool(IsWorkingParameterHash, isWorking);
        }
    }

    public void SetToolVisible(bool isVisible)
    {
        if (_toolRenderer)
        {
            _toolRenderer.gameObject.SetActive(isVisible);
        }
    }

    public void EnableObject(bool isEnable)
    {
        _gameObject.SetActive(isEnable);
    }

    private void CacheAnimatorParameters()
    {
        if (!_animator)
        {
            if (_requiresAnimator)
            {
                Debug.LogError("NPCComponent is missing its Animator reference.", this);
            }
            return;
        }

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.nameHash == SpeedParameterHash && parameter.type == AnimatorControllerParameterType.Float)
            {
                _hasSpeedParameter = true;
            }
            else if (parameter.nameHash == IsInsideBuildingParameterHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                _hasInsideBuildingParameter = true;
            }
            else if (parameter.nameHash == IsWorkingParameterHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                _hasIsWorkingParameter = true;
            }
        }

        if (!_requiresAnimator)
        {
            return;
        }

        if (!_hasSpeedParameter)
        {
            Debug.LogError("NPCComponent Animator is missing the Speed float parameter.", this);
        }

        if (!_hasInsideBuildingParameter)
        {
            Debug.LogError("NPCComponent Animator is missing the IsInsideBuilding bool parameter.", this);
        }

        if (!_hasIsWorkingParameter)
        {
            Debug.LogError("NPCComponent Animator is missing the IsWorking bool parameter.", this);
        }
    }

    private void ResetAnimationState()
    {
        if (!_animator)
        {
            return;
        }

        if (_hasSpeedParameter)
        {
            _animator.SetFloat(SpeedParameterHash, 0f);
        }

        if (_hasInsideBuildingParameter)
        {
            _animator.SetBool(IsInsideBuildingParameterHash, false);
        }

        if (_hasIsWorkingParameter)
        {
            _animator.SetBool(IsWorkingParameterHash, false);
        }

        if (_animator.isActiveAndEnabled)
        {
            _animator.Play(IdleStateHash, 0, 0f);
        }
    }
}
