using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sorts one explicitly configured visual group by its stable ground position. Child renderers
/// retain their authored relative orders; animated visual height never becomes ground depth.
/// </summary>
[DisallowMultipleComponent]
public sealed class GroundDepthSort : MonoBehaviour
{
    private const int GroundBaseOrder = 10000;
    private const int MinimumGroundOrder = 1000;
    private const int MaximumGroundOrder = 19000;
    private const int AirborneOrder = 20000;
    private const float OrdersPerWorldUnit = 100f;

    [SerializeField] private SortingGroup _sortingGroup;
    [SerializeField] private Transform _groundAnchor;
    [SerializeField] private float _groundYOffset;
    [SerializeField, Range(0, 99)] private int _airborneOrderOffset;

    private Transform _rootTransform;
    private bool _isAirborne;
    private bool _hasLoggedConfigurationFailure;
    private int _lastSortingOrder = int.MinValue;

    private void Awake()
    {
        _rootTransform = transform;
    }

    private void OnEnable()
    {
        _isAirborne = false;
        _lastSortingOrder = int.MinValue;

        if (!_sortingGroup)
        {
            if (!_hasLoggedConfigurationFailure)
            {
                Debug.LogError($"GroundDepthSort '{name}': missing required _sortingGroup reference.", this);
                _hasLoggedConfigurationFailure = true;
            }

            enabled = false;
            return;
        }

        UpdateSortingOrder();
    }

    private void OnDisable()
    {
        _isAirborne = false;
        _lastSortingOrder = int.MinValue;
        UpdateSortingOrder();
        _lastSortingOrder = int.MinValue;
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();
    }

    public void SetAirborne(bool isAirborne)
    {
        _isAirborne = isAirborne;
        if (isActiveAndEnabled)
        {
            UpdateSortingOrder();
        }
    }

    private void UpdateSortingOrder()
    {
        if (!_sortingGroup || !_rootTransform)
        {
            return;
        }

        Transform anchor = _groundAnchor ? _groundAnchor : _rootTransform;
        float groundY = anchor.position.y + _groundYOffset;
        // Clamp before integer conversion so even distant anchors cannot overflow the order.
        float groundDepth = Mathf.Clamp(groundY * OrdersPerWorldUnit,
            GroundBaseOrder - MaximumGroundOrder, GroundBaseOrder - MinimumGroundOrder);
        int sortingOrder = _isAirborne
            ? AirborneOrder + Mathf.Clamp(_airborneOrderOffset, 0, 99)
            : GroundBaseOrder - Mathf.RoundToInt(groundDepth);
        if (_lastSortingOrder == sortingOrder)
        {
            return;
        }

        _sortingGroup.sortingOrder = sortingOrder;
        _lastSortingOrder = sortingOrder;
    }

    private void OnValidate()
    {
        _airborneOrderOffset = Mathf.Clamp(_airborneOrderOffset, 0, 99);
        if (float.IsNaN(_groundYOffset) || float.IsInfinity(_groundYOffset))
        {
            _groundYOffset = 0f;
        }

        _lastSortingOrder = int.MinValue;
    }
}
