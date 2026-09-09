using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY placeholder camera control. Arrow keys only, no mouse.
// A real input/camera control system will replace this later — keep it simple, do not extend it.
public class TestCameraArrowMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _smoothTime = 0.2f;
    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;

    private Vector2 _currentVelocity;

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
