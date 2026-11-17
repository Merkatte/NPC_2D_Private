using UnityEngine;

public static class MessageBubblePosition
{
    public static bool Apply(RectTransform rect, RectTransform canvasRect, Canvas canvas, Camera camera,
        Vector3 anchor, float padding, Vector3[] corners, Vector2 offset = default)
    {
        Vector3 screen = camera.WorldToScreenPoint(anchor);
        if (screen.z <= 0f) return false;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screen, uiCamera, out Vector3 world)) return false;
        rect.position = world + canvasRect.TransformVector(offset);
        rect.GetWorldCorners(corners);
        Vector2 minimum = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 maximum = minimum;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 corner = canvasRect.InverseTransformPoint(corners[i]);
            minimum = Vector2.Min(minimum, corner);
            maximum = Vector2.Max(maximum, corner);
        }
        Rect bounds = canvasRect.rect;
        Vector2 shift = new Vector2(ClampShift(minimum.x, maximum.x, bounds.xMin + padding, bounds.xMax - padding),
            ClampShift(minimum.y, maximum.y, bounds.yMin + padding, bounds.yMax - padding));
        rect.position += canvasRect.TransformVector(shift);
        return true;
    }
    private static float ClampShift(float minimum, float maximum, float lower, float upper)
    {
        if (maximum - minimum > upper - lower) return (lower + upper - minimum - maximum) * 0.5f;
        if (minimum < lower) return lower - minimum;
        if (maximum > upper) return upper - maximum;
        return 0f;
    }
}
