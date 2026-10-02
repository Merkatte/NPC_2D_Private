using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// TEMPORARY camera controls: arrow movement and bounded orthographic mouse-wheel zoom.
// Keep these test-scene controls local until a real camera system replaces them.
public class TestCameraArrowMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _smoothTime = 0.2f;
    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;

    private const float MinimumOrthographicSize = 0.01f;
    private const float WindowsScrollUnitsPerNotch = 120f;

    [SerializeField, Tooltip("Required orthographic camera controlled by the mouse wheel.")]
    private Camera _worldCamera;
    [SerializeField, Min(MinimumOrthographicSize), Tooltip("Closest zoom limit. Smaller orthographic size shows a closer view.")]
    private float _minOrthographicSize = 2f;
    [SerializeField, Min(MinimumOrthographicSize), Tooltip("Farthest zoom limit. Larger orthographic size shows a wider view; must be at least the minimum.")]
    private float _maxOrthographicSize = 20f;
    [SerializeField, Min(0f), Tooltip("Orthographic size units per wheel notch. Wheel up reduces size for a closer view; zero disables zoom.")]
    private float _zoomSpeed = 1f;

    private Vector2 _currentVelocity;
    private bool _hasReportedCameraError;
    private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
    private EventSystem _pointerEventSystem;
    private PointerEventData _pointer;

    private void Awake()
    {
        ValidateZoomSettings();
        if (HasZoomCamera())
            _worldCamera.orthographicSize = Mathf.Clamp(_worldCamera.orthographicSize, _minOrthographicSize, _maxOrthographicSize);
    }

    private void OnValidate()
    {
        ValidateZoomSettings();
    }

    private void ValidateZoomSettings()
    {
        _minOrthographicSize = Mathf.Max(MinimumOrthographicSize, _minOrthographicSize);
        _maxOrthographicSize = Mathf.Max(_minOrthographicSize, _maxOrthographicSize);
        _zoomSpeed = Mathf.Max(0f, _zoomSpeed);
    }

    private bool HasZoomCamera()
    {
        if (_worldCamera && _worldCamera.orthographic)
            return true;

        if (!_hasReportedCameraError)
        {
            Debug.LogError($"TestCameraArrowMove '{name}': _worldCamera must reference an orthographic Camera. Wheel zoom is unavailable; arrow movement remains active.", this);
            _hasReportedCameraError = true;
        }
        return false;
    }

    private void Update()
    {
        Vector2 input = ReadArrowInput();
        Vector3 position = transform.position;

        if (position.x <= _minBounds.x && input.x < 0f) input.x = 0f;
        if (position.x >= _maxBounds.x && input.x > 0f) input.x = 0f;
        if (position.y <= _minBounds.y && input.y < 0f) input.y = 0f;
        if (position.y >= _maxBounds.y && input.y > 0f) input.y = 0f;

        Vector2 targetPosition = (Vector2)position + input * _moveSpeed;
        Vector2 smoothed = Vector2.SmoothDamp(position, targetPosition, ref _currentVelocity, _smoothTime);

        smoothed.x = Mathf.Clamp(smoothed.x, _minBounds.x, _maxBounds.x);
        smoothed.y = Mathf.Clamp(smoothed.y, _minBounds.y, _maxBounds.y);

        transform.position = new Vector3(smoothed.x, smoothed.y, position.z);
        UpdateZoom();
    }

    private void UpdateZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !HasZoomCamera())
            return;

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll == 0f || IsOverUi(mouse.position.ReadValue()))
            return;

        // Input System defaults to one unit per notch. Only legacy Windows input uses 120.
        // Preserve accumulated notches and fractional deltas; wheel input is not a velocity.
        bool isWindows = Application.platform == RuntimePlatform.WindowsEditor
            || Application.platform == RuntimePlatform.WindowsPlayer;
        if (isWindows && InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange)
            scroll /= WindowsScrollUnitsPerNotch;

        _worldCamera.orthographicSize = Mathf.Clamp(
            _worldCamera.orthographicSize - scroll * _zoomSpeed, _minOrthographicSize, _maxOrthographicSize);
    }

    private bool IsOverUi(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (!eventSystem)
            return false;

        if (_pointer == null || _pointerEventSystem != eventSystem)
        {
            _pointerEventSystem = eventSystem;
            _pointer = new PointerEventData(eventSystem);
        }

        // Query the current wheel position directly, independent of EventSystem update order.
        _pointer.Reset();
        _pointer.position = screenPosition;
        _uiHits.Clear();
        eventSystem.RaycastAll(_pointer, _uiHits);
        foreach (RaycastResult hit in _uiHits)
        {
            if (hit.module is GraphicRaycaster)
                return true;
        }
        return false;
    }

    private static Vector2 ReadArrowInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.upArrowKey.isPressed) y += 1f;

        Vector2 input = new Vector2(x, y);
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }
}
