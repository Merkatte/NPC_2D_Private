using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public sealed class GoldHUD : MonoBehaviour
{
    [SerializeField] private ResourceManager _resourceManager;
    [SerializeField] private Text _amountText;
    [SerializeField] private Text _changeText;
    [SerializeField, Min(0f)] private float _changeDisplayDuration = 1.5f;

    private ResourceManager _subscribedResources;
    private Coroutine _hideChangeRoutine;
    private int _displayedGold;
    private bool _hasReportedConfigurationError;

    private void OnEnable()
    {
        ClearPresentation();
        if (!_resourceManager || !_amountText || !_changeText)
        {
            if (!_hasReportedConfigurationError)
            {
                Debug.LogError($"GoldHUD '{name}': ResourceManager, amount Text and change Text references are required.", this);
                _hasReportedConfigurationError = true;
            }
            enabled = false;
            return;
        }

        _amountText.raycastTarget = false;
        _changeText.raycastTarget = false;
        _subscribedResources = _resourceManager;
        _subscribedResources.ResourcesChanged += HandleResourcesChanged;
        _displayedGold = _subscribedResources.GetQuantity(ResourceManager.GoldItemId);
        _amountText.text = _displayedGold.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void OnDisable()
    {
        if (_subscribedResources)
            _subscribedResources.ResourcesChanged -= HandleResourcesChanged;
        _subscribedResources = null;
        ClearPresentation();
    }

    private void OnValidate()
    {
        _changeDisplayDuration = GetChangeDisplayDuration();
    }

    private void HandleResourcesChanged()
    {
        if (!_subscribedResources || !_amountText || !_changeText)
        {
            enabled = false;
            return;
        }

        int gold = _subscribedResources.GetQuantity(ResourceManager.GoldItemId);
        if (gold == _displayedGold)
            return;

        long change = (long)gold - _displayedGold;
        _displayedGold = gold;
        _amountText.text = gold.ToString("N0", CultureInfo.InvariantCulture);
        ClearChange();
        float duration = GetChangeDisplayDuration();
        if (duration <= 0f)
            return;

        _changeText.text = change.ToString("+#,0;-#,0;0", CultureInfo.InvariantCulture);
        _hideChangeRoutine = StartCoroutine(HideChangeAfterDelay(duration));
    }

    private IEnumerator HideChangeAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        _hideChangeRoutine = null;
        if (_changeText)
            _changeText.text = string.Empty;
    }

    private float GetChangeDisplayDuration()
    {
        return float.IsNaN(_changeDisplayDuration) || float.IsInfinity(_changeDisplayDuration)
            ? 0f
            : Mathf.Max(0f, _changeDisplayDuration);
    }

    private void ClearPresentation()
    {
        ClearChange();
        _displayedGold = 0;
        if (_amountText)
            _amountText.text = string.Empty;
    }

    private void ClearChange()
    {
        if (_hideChangeRoutine != null)
        {
            StopCoroutine(_hideChangeRoutine);
            _hideChangeRoutine = null;
        }
        if (_changeText)
            _changeText.text = string.Empty;
    }
}
