using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal "how many?" sub-panel nested inside MerchantPopup rather than registered as its own
/// PopBase/PopupType: IUIService.TryShow(PopupType) has no payload channel to hand it an itemId
/// and a max quantity, UIManager has no modality concept beyond stack order, and this panel being
/// a child means it is force-closed for free whenever the parent popup is (satisfying "cart is
/// discarded whenever the popup closes" with no extra wiring). A second consumer elsewhere would
/// be the trigger to promote this into a shared popup; there isn't one yet.
/// </summary>
public sealed class QuantityPromptPanel : MonoBehaviour
{
    private const int InvalidItemId = -1;

    [SerializeField] private MerchantPopup _popup;
    [SerializeField] private GameObject _blockerRoot;
    [SerializeField] private Text _titleText;
    [SerializeField] private Slider _quantitySlider;
    [SerializeField] private InputField _quantityInputField;

    private int _itemId = InvalidItemId;
    private int _maxQuantity;
    private bool _isSyncing;

    public bool IsOpen => gameObject.activeSelf;

    public void Open(int itemId, string displayName, int maxQuantity)
    {
        _itemId = itemId;
        _maxQuantity = Mathf.Max(0, maxQuantity);

        if (_titleText)
            _titleText.text = displayName;

        if (_quantitySlider)
        {
            _quantitySlider.wholeNumbers = true;
            _quantitySlider.minValue = 1;
            _quantitySlider.maxValue = Mathf.Max(1, _maxQuantity);
            SetSliderValueSilently(_maxQuantity > 0 ? 1 : 0);
        }

        SyncInputFieldFromSlider();

        if (_blockerRoot)
            _blockerRoot.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Close()
    {
        _itemId = InvalidItemId;
        _maxQuantity = 0;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Copies the still-valid context into locals before Close() resets _itemId/_maxQuantity —
    /// calling _popup.OnQuantityConfirmed with fields read after Close() would hand it a stale
    /// InvalidItemId instead of the item the user actually picked.
    /// </summary>
    public void Confirm()
    {
        if (_maxQuantity <= 0 || _itemId <= InvalidItemId)
        {
            Close();
            return;
        }

        int itemId = _itemId;
        int quantity = ParseAndClamp(_quantityInputField ? _quantityInputField.text : string.Empty);
        Close();

        if (_popup)
            _popup.OnQuantityConfirmed(itemId, quantity);
    }

    public void Cancel()
    {
        int itemId = _itemId;
        Close();

        if (_popup && itemId >= 0)
            _popup.OnQuantityCanceled(itemId);
    }

    public void SelectAll()
    {
        if (_quantitySlider)
            _quantitySlider.value = _quantitySlider.maxValue;
    }

    public void Decrease()
    {
        if (_quantitySlider)
            _quantitySlider.value -= 1;
    }

    public void Increase()
    {
        if (_quantitySlider)
            _quantitySlider.value += 1;
    }

    /// <summary>
    /// Display-sync only, guarded by _isSyncing to stop Slider<->InputField updates from recursing
    /// into each other. Neither this nor OnInputFieldEndEdit is Confirm's safety net — Confirm()
    /// re-parses the InputField text itself, since a button click can arrive before onEndEdit does.
    /// </summary>
    public void OnSliderChanged(float value)
    {
        if (_isSyncing)
            return;

        _isSyncing = true;
        if (_quantityInputField)
            _quantityInputField.text = Mathf.RoundToInt(value).ToString();
        _isSyncing = false;
    }

    public void OnInputFieldEndEdit(string text)
    {
        if (_isSyncing)
            return;

        int clamped = ParseAndClamp(text);
        _isSyncing = true;
        if (_quantitySlider)
            _quantitySlider.value = clamped;
        if (_quantityInputField)
            _quantityInputField.text = clamped.ToString();
        _isSyncing = false;
    }

    private void SetSliderValueSilently(int value)
    {
        _isSyncing = true;
        _quantitySlider.value = value;
        _isSyncing = false;
    }

    private void SyncInputFieldFromSlider()
    {
        if (!_quantityInputField)
            return;

        _isSyncing = true;
        _quantityInputField.text = (_quantitySlider ? Mathf.RoundToInt(_quantitySlider.value) : 0).ToString();
        _isSyncing = false;
    }

    private int ParseAndClamp(string text)
    {
        if (_maxQuantity <= 0)
            return 0;

        if (!int.TryParse(text, out int value))
            value = _maxQuantity;

        return Mathf.Clamp(value, 1, _maxQuantity);
    }

    private void Awake()
    {
        if (_blockerRoot)
            _blockerRoot.SetActive(false);

        gameObject.SetActive(false);
    }
}
