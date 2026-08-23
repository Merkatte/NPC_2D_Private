using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour, IUIService
{
    [Header("Registries")]
    [SerializeField] private PopBase[] _popups;
    [SerializeField] private HoverBase[] _hovers;

    private readonly Dictionary<PopupType, PopBase> _popupRegistry
        = new Dictionary<PopupType, PopBase>();
    private readonly Dictionary<HoverType, HoverBase> _hoverRegistry
        = new Dictionary<HoverType, HoverBase>();
    private readonly List<PopBase> _popupStack = new List<PopBase>();

    private HoverBase _activeHover;

    public bool HasOpenPopup => FindTopPopup() != null;
    public bool HasVisibleHover => _activeHover && _activeHover.IsVisible;

    private void Awake()
    {
        BuildPopupRegistry();
        BuildHoverRegistry();
    }

    public bool TryShow(PopupType popupType)
    {
        if (!_popupRegistry.TryGetValue(popupType, out PopBase popup) || !popup)
            return false;

        RemoveFromPopupStack(popup);
        popup.Open();
        _popupStack.Add(popup);
        return true;
    }

    public bool TryHide(PopupType popupType)
    {
        if (!_popupRegistry.TryGetValue(popupType, out PopBase popup) || !popup)
            return false;

        popup.Close();
        RemoveFromPopupStack(popup);
        return true;
    }

    public bool TryHideTopPopup()
    {
        PopBase popup = FindTopPopup();
        if (!popup)
            return false;

        popup.Close();
        RemoveFromPopupStack(popup);
        return true;
    }

    public bool TryShow(HoverType hoverType, IHoverInfoSource source)
    {
        if (!_hoverRegistry.TryGetValue(hoverType, out HoverBase hover) || !hover)
            return false;

        if (_activeHover && (_activeHover != hover || !_activeHover.IsOwnedBy(source)))
        {
            _activeHover.HideCurrent();
            _activeHover = null;
        }

        if (!hover.TryShow(source))
            return false;

        _activeHover = hover;
        return true;
    }

    public bool TryHide(HoverType hoverType, IHoverInfoSource source)
    {
        if (!_hoverRegistry.TryGetValue(hoverType, out HoverBase hover) || !hover)
            return false;

        if (_activeHover != hover || !hover.IsOwnedBy(source))
            return false;

        hover.HideCurrent();
        _activeHover = null;
        return true;
    }

    public void HideAll()
    {
        foreach (PopBase popup in _popupRegistry.Values)
        {
            if (popup)
                popup.Close();
        }

        _popupStack.Clear();

        if (_activeHover)
            _activeHover.HideCurrent();

        _activeHover = null;
    }

    private void BuildPopupRegistry()
    {
        _popupRegistry.Clear();
        _popupStack.Clear();

        if (_popups == null)
            return;

        foreach (PopBase popup in _popups)
        {
            if (!popup)
            {
                Debug.LogError("UIManager: null or destroyed entry in _popups.");
                continue;
            }

            if (popup.PopupType == PopupType.None)
            {
                Debug.LogError($"UIManager: popup '{popup.name}' has PopupType.None.", popup);
                continue;
            }

            if (_popupRegistry.ContainsKey(popup.PopupType))
            {
                Debug.LogError($"UIManager: duplicate popup registration for {popup.PopupType}; keeping the first entry.", popup);
                continue;
            }

            _popupRegistry.Add(popup.PopupType, popup);

            if (popup.IsOpen)
                _popupStack.Add(popup);
        }
    }

    private void BuildHoverRegistry()
    {
        _hoverRegistry.Clear();
        _activeHover = null;

        if (_hovers == null)
            return;

        foreach (HoverBase hover in _hovers)
        {
            if (!hover)
            {
                Debug.LogError("UIManager: null or destroyed entry in _hovers.");
                continue;
            }

            if (hover.HoverType == HoverType.None)
            {
                Debug.LogError($"UIManager: hover '{hover.name}' has HoverType.None.", hover);
                continue;
            }

            if (_hoverRegistry.ContainsKey(hover.HoverType))
            {
                Debug.LogError($"UIManager: duplicate hover registration for {hover.HoverType}; keeping the first entry.", hover);
                continue;
            }

            _hoverRegistry.Add(hover.HoverType, hover);
            hover.HideCurrent();
        }
    }

    private PopBase FindTopPopup()
    {
        for (int i = _popupStack.Count - 1; i >= 0; --i)
        {
            PopBase popup = _popupStack[i];
            if (popup && popup.IsOpen)
                return popup;

            _popupStack.RemoveAt(i);
        }

        return null;
    }

    private void RemoveFromPopupStack(PopBase popup)
    {
        for (int i = _popupStack.Count - 1; i >= 0; --i)
        {
            if (!_popupStack[i] || _popupStack[i] == popup)
                _popupStack.RemoveAt(i);
        }
    }
}
