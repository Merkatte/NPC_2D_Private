using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single reusable label that follows the cursor during a slot drag. MerchantPopup owns exactly
/// one instance and toggles it — a fresh Instantiate/Destroy per drag would be wasted work for
/// something that only ever shows one line of text. blocksRaycasts must stay false: if the ghost
/// itself blocked raycasts it would intercept the pointer and IDropHandler.OnDrop would never fire
/// on whatever is underneath it.
/// </summary>
public sealed class DragGhostView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Text _labelText;
    [SerializeField] private RectTransform _rectTransform;

    public void Show(string label, Vector2 screenPosition)
    {
        if (_labelText)
            _labelText.text = label;

        MoveTo(screenPosition);
        gameObject.SetActive(true);
    }

    public void MoveTo(Vector2 screenPosition)
    {
        if (_rectTransform)
            _rectTransform.position = screenPosition;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (_canvasGroup)
            _canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }
}
