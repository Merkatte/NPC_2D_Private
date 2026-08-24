using UnityEngine;

public interface IHoverInfoSource
{
    Object Owner { get; }
    HoverType HoverType { get; }
    bool TryGetHoverInfo(out HoverInfo info);
}
