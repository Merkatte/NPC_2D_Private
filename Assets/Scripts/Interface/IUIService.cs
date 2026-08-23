public interface IUIService
{
    bool TryShow(PopupType popupType);
    bool TryHide(PopupType popupType);

    bool TryShow(HoverType hoverType, IHoverInfoSource source);
    bool TryHide(HoverType hoverType, IHoverInfoSource source);
}
