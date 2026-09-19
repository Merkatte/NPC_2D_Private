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
        if (_text)
            _text.text = value;
        else
            _tmpText.text = value;
    }
}
