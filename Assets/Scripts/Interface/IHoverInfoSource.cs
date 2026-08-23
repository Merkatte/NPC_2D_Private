using UnityEngine;

public interface IHoverInfoSource
{
    Object Owner { get; }
    bool TryGetHoverInfo(out HoverInfo info);
}
