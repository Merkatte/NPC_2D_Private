using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LocalizeText : MonoBehaviour
{
    [SerializeField] private LocalizeKey _key = LocalizeKey.UI_Confirm;
    [SerializeField] private Text _text;
    [SerializeField] private TMP_Text _tmpText;
    private bool _hasStarted;
    private bool _hasReportedConfiguration;
    private bool _hasReportedManager;
    private bool _hasReportedFormat;
    private object[] _formatArguments;

    private void Start()
    {
        _hasStarted = true;
        Refresh();
    }

    private void OnEnable()
    {
        // Initial OnEnable can run before the manager's Awake.
        if (_hasStarted)
            Refresh();
    }

    public void SetKey(LocalizeKey key)
    {
        _key = key;
        if (_hasStarted)
            Refresh();
    }

    public void SetFormatArguments(params object[] arguments)
    {
        _formatArguments = arguments;
        if (_hasStarted)
            Refresh();
    }

    public void SetFormattedKey(LocalizeKey key, params object[] arguments)
    {
        _key = key;
        SetFormatArguments(arguments);
    }

    public void Refresh()
    {
        if (!_hasStarted)
            return;
        if ((bool)_text == (bool)_tmpText)
        {
            if (!_hasReportedConfiguration)
                Debug.LogError("LocalizeText '" + name + "': assign exactly one of _text or _tmpText.", this);
            _hasReportedConfiguration = true;
            return;
        }
        LocalizeManager manager = LocalizeManager.Instance;
        string value;
        if (!manager)
        {
            if (!_hasReportedManager)
                Debug.LogError("LocalizeText '" + name + "': LocalizeManager.Instance is missing.", this);
            _hasReportedManager = true;
            value = "[Missing:" + _key + "]";
        }
        else
            value = manager.GetText(_key);
        if (_formatArguments != null && _formatArguments.Length > 0)
        {
            try
            {
                value = string.Format(CultureInfo.InvariantCulture, value, GetFormatArguments(manager));
            }
            catch (FormatException)
            {
                if (!_hasReportedFormat)
                    Debug.LogError($"LocalizeText '{name}': invalid format or arguments for key {_key}.", this);
                _hasReportedFormat = true;
            }
        }
        if (_text)
            _text.text = value;
        else
            _tmpText.text = value;
    }

    private object[] GetFormatArguments(LocalizeManager manager)
    {
        object[] resolved = _formatArguments;
        for (int i = 0; i < _formatArguments.Length; ++i)
        {
            if (!(_formatArguments[i] is LocalizeKey key)) continue;
            // Retain the original keys so a later refresh resolves them again.
            if (ReferenceEquals(resolved, _formatArguments)) resolved = (object[])_formatArguments.Clone();
            resolved[i] = manager ? manager.GetText(key) : "[Missing:" + key + "]";
        }
        return resolved;
    }
}
