using JetBrains.Annotations;
using UnityEngine;

public readonly struct HoverInfo
{
    [CanBeNull] public string Title { get; }
    [CanBeNull] public string Description { get; }
    public Vector3 AnchorPosition { get; }
    public bool HasProgress { get; }
    public float NormalizedProgress { get; }

    public HoverInfo(
        [CanBeNull] string title,
        [CanBeNull] string description,
        Vector3 anchorPosition,
        bool hasProgress = false,
        float normalizedProgress = 0f)
    {
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        AnchorPosition = anchorPosition;
        HasProgress = hasProgress;
        NormalizedProgress = Mathf.Clamp01(normalizedProgress);
    }
}
