/// <summary>
/// A world-space object that can open a popup when clicked. Returning false means "not
/// clickable right now" — the reason (e.g. an animation still playing) stays inside the
/// implementation; PointerClickRouter never learns why and never needs to.
/// </summary>
public interface IClickPopupSource
{
    bool TryGetClickPopup(out PopupType popupType);
}
