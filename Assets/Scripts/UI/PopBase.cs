using UnityEngine;

public abstract class PopBase : MonoBehaviour
{
    [SerializeField] private PopupType _popupType;

    public PopupType PopupType => _popupType;
    public bool IsOpen => gameObject.activeSelf;

    internal void Open()
    {
        if (IsOpen)
        {
            transform.SetAsLastSibling();
            return;
        }

        OnBeforeOpen();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        OnOpened();
    }

    internal void Close()
    {
        if (!IsOpen)
            return;

        OnBeforeClose();
        gameObject.SetActive(false);
        OnClosed();
    }

    protected virtual void OnBeforeOpen()
    {
    }

    protected virtual void OnOpened()
    {
    }

    protected virtual void OnBeforeClose()
    {
    }

    protected virtual void OnClosed()
    {
    }
}
