using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class MerchantProductRowView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _stockText;
    [SerializeField] private Text _priceText;
    [SerializeField] private InputField _quantityInput;
    [SerializeField] private Button _decreaseButton;
    [SerializeField] private Button _increaseButton;
    [SerializeField] private Color _normalColor = new Color(0.25f, 0.18f, 0.12f);
    [SerializeField] private Color _shortageColor = new Color(0.7f, 0.15f, 0.1f);

    private int _itemId;
    private bool _hasPendingInput;
    public bool IsConfigured => _icon && _nameText && _stockText && _priceText &&
        _quantityInput && _decreaseButton && _increaseButton;
    public event Action<int, string> QuantitySubmitted;
    public event Action<int, int> StepRequested;

    private void OnEnable()
    {
        if (!IsConfigured) return;
        _quantityInput.onValueChanged.AddListener(HandleInputChanged);
        _quantityInput.onEndEdit.AddListener(HandleEndEdit);
        _decreaseButton.onClick.AddListener(Decrease);
        _increaseButton.onClick.AddListener(Increase);
    }

    private void OnDisable()
    {
        if (!IsConfigured) return;
        _quantityInput.onValueChanged.RemoveListener(HandleInputChanged);
        _quantityInput.onEndEdit.RemoveListener(HandleEndEdit);
        _decreaseButton.onClick.RemoveListener(Decrease);
        _increaseButton.onClick.RemoveListener(Increase);
        _hasPendingInput = false;
    }

    public void Bind(int itemId, string displayName, Sprite icon, int stock, int quantity, string price)
    {
        if (!IsConfigured) return;
        if (_itemId != itemId) _hasPendingInput = false;
        _itemId = itemId;
        _nameText.text = displayName;
        _icon.sprite = icon;
        _icon.enabled = icon;
        _stockText.text = quantity > stock ? $"보유 {stock} · 재고 부족" : $"보유 {stock}";
        _stockText.color = quantity > stock ? _shortageColor : _normalColor;
        _priceText.text = price;
        if (!_hasPendingInput) _quantityInput.SetTextWithoutNotify(quantity.ToString());
        _decreaseButton.interactable = quantity > 0 || _hasPendingInput;
        _increaseButton.interactable = quantity < stock || _hasPendingInput;
    }

    public void CommitPendingInput()
    {
        if (!_hasPendingInput || !IsConfigured) return;
        _hasPendingInput = false;
        QuantitySubmitted?.Invoke(_itemId, _quantityInput.text);
    }

    public void DiscardPendingInput() => _hasPendingInput = false;
    private void HandleInputChanged(string value)
    {
        _hasPendingInput = true;
        // A step click must first submit the edited value, even if the previous value was a boundary.
        _decreaseButton.interactable = true;
        _increaseButton.interactable = true;
    }
    private void HandleEndEdit(string value) => CommitPendingInput();
    private void Decrease() { CommitPendingInput(); StepRequested?.Invoke(_itemId, -1); }
    private void Increase() { CommitPendingInput(); StepRequested?.Invoke(_itemId, 1); }
}
